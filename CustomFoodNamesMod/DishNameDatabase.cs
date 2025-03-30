namespace CustomFoodNamesMod
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CustomFoodNamesMod.Database;
    using CustomFoodNamesMod.Utils;
    using Verse;

    /// <summary>
    /// Database of dish names based on ingredients
    /// </summary>
    public static class DishNameDatabase
    {
        /// <summary>
        /// Ingredient to dish entries for simple lookups
        /// </summary>
        public static Dictionary<string, List<DishEntry>> IngredientToDishEntries =
            new Dictionary<string, List<DishEntry>>();

        /// <summary>
        /// Quality-specific ingredient entries
        /// </summary>
        public static Dictionary<string, Dictionary<string, List<DishEntry>>> IngredientToQualityDishEntries =
            new Dictionary<string, Dictionary<string, List<DishEntry>>>();

        /// <summary>
        /// Two-ingredient combination entries
        /// </summary>
        public static Dictionary<string, Dictionary<string, List<DishEntry>>> IngredientComboDishEntries =
            new Dictionary<string, Dictionary<string, List<DishEntry>>>();

        /// <summary>
        /// Three-ingredient combination entries
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>> ThreeIngredientComboDishEntries =
            new Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>>();

        // Fallback patterns for ingredients not in database

        /// <summary>
        /// Defines the FallbackPatterns
        /// </summary>
        private static readonly List<string> FallbackPatterns = new List<string>
        {
            "{0} dish",
            "Simple {0} meal",
            "{0} preparation",
            "Basic {0} plate",
            "Plain {0} serving"
        };

        // Track missing ingredients we've seen for logging purposes

        /// <summary>
        /// Defines the reportedMissingIngredients
        /// </summary>
        private static HashSet<string> reportedMissingIngredients = new HashSet<string>();

        // Flag to enable/disable logging of missing ingredients

        /// <summary>
        /// Defines the LogMissingIngredients
        /// </summary>
        public static bool LogMissingIngredients = false;

        /// <summary>
        /// Initializes static members of the <see cref="DishNameDatabase"/> class.
        /// </summary>
        static DishNameDatabase()
        {
            LoadDatabase();
        }

        /// <summary>
        /// Load the dish name database
        /// </summary>
        public static void LoadDatabase()
        {
            // Use the new DatabaseLoader
            DatabaseLoader.LoadDatabase(
                IngredientToDishEntries,
                IngredientComboDishEntries,
                IngredientToQualityDishEntries,
                ThreeIngredientComboDishEntries);
        }

        /// <summary>
        /// Gets dish info (name and description) for a list of ingredients
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetDishInfoForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            if (ingredients == null || ingredients.Count == 0)
                return new DishInfo("Mystery meal", "A meal of mysterious origin and content.");

            // Create a deterministic seed based on the ingredients
            // This ensures the same meal will always get the same dish
            // But different meals with the same ingredients will get different dishes
            int seed = 0;
            foreach (var ing in ingredients)
            {
                seed += ing.defName.GetHashCode();
            }

            // Add a unique identifier for this specific meal to the seed
            // For example, you could use the meal's ID or creation timestamp
            // This ensures different meals with the same ingredients get different dishes
            if (Rand.Value < 0.8f) // 80% chance to add variation
            {
                seed += GenTicks.TicksGame;  // Use current game tick for randomness
            }

            if (!string.IsNullOrEmpty(mealQuality))
            {
                seed += mealQuality.GetHashCode();
            }

            Rand.PushState(seed);

            DishInfo result;
            if (ingredients.Count == 1)
            {
                // Single ingredient
                result = GetRandomDishInfo(ingredients[0].defName, mealQuality);
            }
            else if (ingredients.Count == 2)
            {
                // Two ingredients
                result = GetRandomComboDishInfo(ingredients[0].defName, ingredients[1].defName);

                // Fall back to single ingredient if no combo found
                if (result == null)
                {
                    result = GetRandomDishInfo(ingredients[0].defName, mealQuality);
                    if (result != null)
                    {
                        string secondaryLabel = StringUtils.GetCapitalizedLabel(ingredients[1].label);

                        // Apply quality-specific formatting based on meal quality
                        if (mealQuality == "Lavish")
                        {
                            string[] lavishFormats = new string[] {
                        "{0} accompanied by exquisite {1}",
                        "{0} with delicate {1} accent",
                        "{0} served atop artisanal {1}",
                        "Gourmet {0} with {1} infusion",
                        "Luxurious {0} with {1} accompaniment",
                        "Chef's special {0} with premium {1}",
                        "Decadent {0} with fine {1} garnish",
                        "Sumptuous {0} presented with elegant {1}",
                        "Extravagant {0} paired with {1} reduction"
                    };
                            string format = lavishFormats[Rand.Range(0, lavishFormats.Length)];

                            // Generate a lavish name and description
                            string lavishName = string.Format(format, result.Name, secondaryLabel);
                            // Remove any contradictory terms that might exist in the database name
                            lavishName = lavishName.Replace("Simple ", "").Replace("Basic ", "").Replace("Plain ", "");

                            string lavishDesc = result.Description + " The " + secondaryLabel.ToLower() +
                                " adds sophistication to this lavish creation, elevating it to a gourmet experience.";

                            result = new DishInfo(lavishName, lavishDesc);
                        }
                        else if (mealQuality == "Fine")
                        {
                            string[] fineFormats = new string[] {
                        "Quality {0} with {1}",
                        "Fine {0} with {1} accent",
                        "Well-crafted {0} with {1}",
                        "Refined {0} and {1} plate"
                    };
                            string format = fineFormats[Rand.Range(0, fineFormats.Length)];

                            // Generate a fine name and description
                            string fineName = string.Format(format, result.Name, secondaryLabel);
                            // Remove any contradictory terms
                            fineName = fineName.Replace("Simple ", "").Replace("Basic ", "").Replace("Plain ", "");

                            string fineDesc = result.Description + " The " + secondaryLabel.ToLower() +
                                " adds a refined touch to this well-prepared meal.";

                            result = new DishInfo(fineName, fineDesc);
                        }
                        else
                        {
                            // Original behavior for simple meals
                            result = new DishInfo(
                                result.Name + " with " + secondaryLabel,
                                result.Description + " " + secondaryLabel + " adds a complementary flavor.");
                        }
                    }
                    else
                    {
                        // If no dish info found even for the first ingredient, create a quality-appropriate fallback
                        string primaryLabel = StringUtils.GetCapitalizedLabel(ingredients[0].label);
                        string secondaryLabel = StringUtils.GetCapitalizedLabel(ingredients[1].label);

                        string name;
                        string description;

                        if (mealQuality == "Lavish")
                        {
                            string[] lavishTemplates = new string[] {
                        "Gourmet {0} with {1}",
                        "Luxurious {0} and {1} creation",
                        "Chef's special {0} with {1}",
                        "Exquisite {0} and {1} platter"
                    };
                            name = string.Format(lavishTemplates[Rand.Range(0, lavishTemplates.Length)], primaryLabel, secondaryLabel);
                            description = $"A lavishly prepared dish featuring premium {primaryLabel.ToLower()} and {secondaryLabel.ToLower()}, expertly combined for a gourmet dining experience.";
                        }
                        else if (mealQuality == "Fine")
                        {
                            string[] fineTemplates = new string[] {
                        "Quality {0} with {1}",
                        "Fine {0} and {1} dish",
                        "Well-prepared {0} with {1}"
                    };
                            name = string.Format(fineTemplates[Rand.Range(0, fineTemplates.Length)], primaryLabel, secondaryLabel);
                            description = $"A well-crafted meal combining {primaryLabel.ToLower()} and {secondaryLabel.ToLower()} for a balanced and satisfying dining experience.";
                        }
                        else
                        {
                            name = $"{primaryLabel} and {secondaryLabel} dish";
                            description = $"A simple meal combining {primaryLabel.ToLower()} and {secondaryLabel.ToLower()}.";
                        }

                        result = new DishInfo(name, description);
                    }
                }
            }
            else if (ingredients.Count == 3)
            {
                // Three ingredients
                result = GetRandomThreeComboDishInfo(
                    ingredients[0].defName,
                    ingredients[1].defName,
                    ingredients[2].defName);

                // Fallback for three ingredients if no specific combo found
                if (result == null && !string.IsNullOrEmpty(mealQuality))
                {
                    string primaryLabel = StringUtils.GetCapitalizedLabel(ingredients[0].label);

                    if (mealQuality == "Lavish")
                    {
                        string[] ingredientLabels = ingredients
                            .Select(i => StringUtils.GetCapitalizedLabel(i.label).ToLower())
                            .ToArray();

                        string[] lavishTrioTemplates = new string[] {
                    $"Gourmet Trio of {ingredientLabels[0]}, {ingredientLabels[1]}, and {ingredientLabels[2]}",
                    $"Chef's Masterpiece with {ingredientLabels[0]}, {ingredientLabels[1]}, and {ingredientLabels[2]}",
                    $"Luxurious {primaryLabel} Ensemble",
                    $"Extravagant {primaryLabel} Three-Way"
                };

                        string name = lavishTrioTemplates[Rand.Range(0, lavishTrioTemplates.Length)];
                        string description = $"A sumptuous meal featuring a lavish combination of {string.Join(", ", ingredientLabels)}, prepared with culinary expertise.";

                        result = new DishInfo(name, description);
                    }
                }
            }
            else
            {
                // For more complex combinations with 4+ ingredients
                if (!string.IsNullOrEmpty(mealQuality) && mealQuality == "Lavish")
                {
                    string primaryLabel = StringUtils.GetCapitalizedLabel(ingredients[0].label);

                    string[] lavishMultiTemplates = new string[] {
                $"Gourmet {primaryLabel} Feast",
                $"Chef's Grand Selection",
                $"Luxurious Culinary Medley",
                $"Extravagant Multi-Ingredient Creation",
                $"Decadent {primaryLabel} Showcase"
            };

                    string name = lavishMultiTemplates[Rand.Range(0, lavishMultiTemplates.Length)];
                    string description = "An exquisite meal featuring multiple premium ingredients, masterfully combined to create a lavish dining experience.";

                    result = new DishInfo(name, description);
                }
                else
                {
                    // Default for other meal qualities with many ingredients
                    result = null;
                }
            }

            Rand.PopState(); // Restore previous random state
            return result;
        }

        /// <summary>
        /// Gets a dish name for a list of ingredients, using the database for small combos
        /// and falling back to procedural generation for complex combinations
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetDishNameForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            DishInfo info = GetDishInfoForIngredients(ingredients, mealQuality);

            if (info != null)
            {
                Log.Message($"[CustomFoodNames] Using database name: {info.Name}");
                return info.Name;
            }

            // Fall back to a simple name if no dish info found
            if (ingredients.Count > 0)
            {
                Log.Message($"[CustomFoodNames] Using fallback name for: {ingredients[0].defName}");
                return StringUtils.GetCapitalizedLabel(ingredients[0].label) + " dish";
            }

            return "Mystery meal";
        }

        /// <summary>
        /// Gets dish info for a single ingredient, with fallback to generated names
        /// </summary>
        /// <param name="ingredientDefName">The ingredientDefName<see cref="string"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomDishInfo(string ingredientDefName, string mealQuality = null)
        {
            // Log attempts
            Log.Message($"[CustomFoodNames] Looking up: {ingredientDefName} with quality {mealQuality ?? "none"}");

            // Try quality-specific lookup first if quality is provided
            if (!string.IsNullOrEmpty(mealQuality) &&
                IngredientToQualityDishEntries.TryGetValue(ingredientDefName, out var qualityDict) &&
                qualityDict.TryGetValue(mealQuality, out List<DishEntry> qualityDishEntries) &&
                qualityDishEntries.Count > 0)
            {
                Log.Message($"[CustomFoodNames] Found quality-specific entry for {ingredientDefName}");
                var entry = qualityDishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // Try direct lookup
            if (IngredientToDishEntries.TryGetValue(ingredientDefName, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                Log.Message($"[CustomFoodNames] Found basic entry for {ingredientDefName}");
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // Try case-insensitive lookup
            var matchingKey = IngredientToDishEntries.Keys.FirstOrDefault(k =>
                string.Equals(k, ingredientDefName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null && IngredientToDishEntries[matchingKey].Count > 0)
            {
                var entry = IngredientToDishEntries[matchingKey].RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // No exact match found, try to get the ingredient label and generate a name
            var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
            if (ingredientDef != null)
            {
                // Log missing ingredient only once
                if (LogMissingIngredients && !reportedMissingIngredients.Contains(ingredientDefName))
                {
                    Log.Message($"[CustomFoodNames] Missing dish name for ingredient: {ingredientDefName} (label: {ingredientDef.label})");
                    reportedMissingIngredients.Add(ingredientDefName);
                }

                // Clean up the ingredient label
                string cleanLabel = StringUtils.GetCapitalizedLabel(ingredientDef.label);

                // Select a random pattern and apply it
                string pattern = FallbackPatterns.RandomElement();
                string name = string.Format(pattern, cleanLabel);
                string description = $"A simple dish made with {cleanLabel.ToLower()}.";

                return new DishInfo(name, description);
            }

            // Last resort fallback
            return new DishInfo("Mystery dish", "A meal of unknown origin and questionable content.");
        }

        /// <summary>
        /// Gets dish info for a combination of two ingredients
        /// </summary>
        /// <param name="ingredient1">The ingredient1<see cref="string"/></param>
        /// <param name="ingredient2">The ingredient2<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomComboDishInfo(string ingredient1, string ingredient2)
        {
            // Return null if we don't have both ingredients
            if (string.IsNullOrEmpty(ingredient1) || string.IsNullOrEmpty(ingredient2))
                return null;

            // Check if we have a combo for these ingredients
            if (IngredientComboDishEntries.TryGetValue(ingredient1, out var innerDict) &&
                innerDict.TryGetValue(ingredient2, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // We didn't find a match, return null
            return null;
        }

        /// <summary>
        /// Gets dish info for a combination of three ingredients
        /// </summary>
        /// <param name="ingredient1">The ingredient1<see cref="string"/></param>
        /// <param name="ingredient2">The ingredient2<see cref="string"/></param>
        /// <param name="ingredient3">The ingredient3<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomThreeComboDishInfo(string ingredient1, string ingredient2, string ingredient3)
        {
            // Return null if we don't have all three ingredients
            if (string.IsNullOrEmpty(ingredient1) ||
                string.IsNullOrEmpty(ingredient2) ||
                string.IsNullOrEmpty(ingredient3))
                return null;

            // Check if we have a combo for these ingredients
            if (ThreeIngredientComboDishEntries.TryGetValue(ingredient1, out var dict1) &&
                dict1.TryGetValue(ingredient2, out var dict2) &&
                dict2.TryGetValue(ingredient3, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // We didn't find a match, return null
            return null;
        }
    }
}
