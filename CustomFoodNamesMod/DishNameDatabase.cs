namespace CustomFoodNamesMod
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
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

        // Quality-appropriate dish descriptions

        /// <summary>
        /// Defines the QualityDescriptionFormats
        /// </summary>
        private static readonly Dictionary<string, List<string>> QualityDescriptionFormats = new Dictionary<string, List<string>>
        {
            {
                "Simple", new List<string>
                {
                    "A basic dish containing {0}. It offers good nutrition although the taste is simple.",
                    "A simple preparation with {0}. It provides adequate nutrition with basic flavors.",
                    "A no-frills meal made with {0}. Satisfying but nothing special.",
                    "A straightforward dish of {0}. It fills the stomach without much flair."
                }
            },
            {
                "Fine", new List<string>
                {
                    "A well-prepared dish containing {0}. It has been skillfully made to balance nutrition and taste.",
                    "A quality meal featuring {0}. The flavors are harmoniously balanced.",
                    "A refined dish made with {0}. The care in preparation is evident in every bite.",
                    "A nicely crafted meal of {0}. It satisfies both hunger and culinary expectations."
                }
            },
            {
                "Lavish", new List<string>
                {
                    "A lavishly prepared dish containing {0}. It has been expertly crafted to be both nutritious and delicious.",
                    "A gourmet creation showcasing {0}. The exquisite preparation highlights each flavor component.",
                    "A sumptuous meal featuring {0}. Every element has been carefully selected and prepared with mastery.",
                    "An exceptional culinary achievement with {0}. The complexity and richness of flavors are remarkable."
                }
            }
        };

        // Quality-specific adjectives for ingredients

        /// <summary>
        /// Defines the QualityIngredientAdjectives
        /// </summary>
        private static readonly Dictionary<string, List<string>> QualityIngredientAdjectives = new Dictionary<string, List<string>>
        {
            {
                "Simple", new List<string>
                {
                    "basic", "simple", "plain", "modest", "humble", "straightforward"
                }
            },
            {
                "Fine", new List<string>
                {
                    "quality", "fine", "select", "superior", "choice", "premium"
                }
            },
            {
                "Lavish", new List<string>
                {
                    "exquisite", "gourmet", "lavish", "exceptional", "superb", "prime", "luxurious"
                }
            }
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

            // Use a consistent quality string or default to Simple
            string qualityString = !string.IsNullOrEmpty(mealQuality) ? mealQuality : "Simple";

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

            // Check for twisted meat first
            bool hasTwistedMeat = HasTwistedMeat(ingredients);

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
                    ThingDef primaryIngredient = ingredients[0];
                    ThingDef secondaryIngredient = ingredients[1];

                    // Determine which one is twisted meat (if any)
                    bool primaryIsTwisted = IsTwistedMeat(primaryIngredient);
                    bool secondaryIsTwisted = IsTwistedMeat(secondaryIngredient);

                    // If we have twisted meat, make sure it's handled correctly
                    if (primaryIsTwisted || secondaryIsTwisted)
                    {
                        // Ensure the non-twisted ingredient is primary
                        if (primaryIsTwisted && !secondaryIsTwisted)
                        {
                            // Swap them so non-twisted is primary
                            var temp = primaryIngredient;
                            primaryIngredient = secondaryIngredient;
                            secondaryIngredient = temp;
                        }

                        result = GetRandomDishInfo(primaryIngredient.defName, mealQuality);
                        if (result != null)
                        {
                            // Always use "Twisted Meat" for the twisted meat ingredient
                            string primaryLabel = StringUtils.GetCapitalizedLabel(primaryIngredient.label);

                            // Apply quality-specific formatting based on meal quality
                            if (mealQuality == "Lavish")
                            {
                                string[] lavishFormats = new string[] {
                                    "{0} accompanied by unsettling Twisted Meat",
                                    "{0} with eerie Twisted Meat accent",
                                    "{0} served with uncanny Twisted Meat",
                                    "Gourmet {0} with Twisted Meat infusion",
                                    "Luxurious {0} with disturbing Twisted Meat",
                                    "Chef's special {0} with peculiar Twisted Meat",
                                };
                                string format = lavishFormats[Rand.Range(0, lavishFormats.Length)];
                                string lavishName = string.Format(format, result.Name);

                                string lavishDesc = GenerateQualityDescription(ingredients, "Lavish");
                                lavishDesc += " The Twisted Meat adds an otherworldly quality to this lavish creation, making it both fascinating and slightly disturbing.";

                                result = new DishInfo(lavishName, lavishDesc);
                            }
                            else if (mealQuality == "Fine")
                            {
                                string[] fineFormats = new string[] {
                                    "Quality {0} with Twisted Meat",
                                    "Fine {0} with Twisted Meat accent",
                                    "Well-crafted {0} with Twisted Meat",
                                    "Refined {0} and Twisted Meat plate"
                                };
                                string format = fineFormats[Rand.Range(0, fineFormats.Length)];
                                string fineName = string.Format(format, result.Name);

                                string fineDesc = GenerateQualityDescription(ingredients, "Fine");
                                fineDesc += " The Twisted Meat adds an unusual quality to this well-prepared meal.";

                                result = new DishInfo(fineName, fineDesc);
                            }
                            else
                            {
                                // Original behavior for simple meals
                                string simpleName = result.Name + " with Twisted Meat";
                                string simpleDesc = GenerateQualityDescription(ingredients, "Simple");
                                simpleDesc += " Twisted Meat adds a strange, otherworldly flavor.";

                                result = new DishInfo(simpleName, simpleDesc);
                            }
                        }
                        else
                        {
                            // If no dish info found for first ingredient, create something with twisted meat
                            string primaryLabel = StringUtils.GetCapitalizedLabel(primaryIngredient.label);

                            string name = $"{primaryLabel} with Twisted Meat";
                            string description = GenerateQualityDescription(ingredients, qualityString);
                            description += " The twisted meat gives it a strange, otherworldly quality.";

                            result = new DishInfo(name, description);
                        }
                    }
                    else
                    {
                        // Regular case (no twisted meat)
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

                                string lavishDesc = GenerateQualityDescription(ingredients, "Lavish");
                                lavishDesc += $" The {secondaryLabel.ToLower()} adds sophistication to this lavish creation, elevating it to a gourmet experience.";

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

                                string fineDesc = GenerateQualityDescription(ingredients, "Fine");
                                fineDesc += $" The {secondaryLabel.ToLower()} adds a refined touch to this well-prepared meal.";

                                result = new DishInfo(fineName, fineDesc);
                            }
                            else
                            {
                                // Original behavior for simple meals
                                string simpleName = result.Name + " with " + secondaryLabel;
                                string simpleDesc = GenerateQualityDescription(ingredients, "Simple");
                                simpleDesc += $" {secondaryLabel} adds a complementary flavor.";

                                result = new DishInfo(simpleName, simpleDesc);
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
                                description = GenerateQualityDescription(ingredients, "Lavish");
                            }
                            else if (mealQuality == "Fine")
                            {
                                string[] fineTemplates = new string[] {
                                    "Quality {0} with {1}",
                                    "Fine {0} and {1} dish",
                                    "Well-prepared {0} with {1}"
                                };
                                name = string.Format(fineTemplates[Rand.Range(0, fineTemplates.Length)], primaryLabel, secondaryLabel);
                                description = GenerateQualityDescription(ingredients, "Fine");
                            }
                            else
                            {
                                name = $"{primaryLabel} and {secondaryLabel} dish";
                                description = GenerateQualityDescription(ingredients, "Simple");
                            }

                            result = new DishInfo(name, description);
                        }
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
                    // Check for twisted meat
                    if (hasTwistedMeat)
                    {
                        // Create a special name for meals with twisted meat and other ingredients
                        string[] twistedTrioTemplates = new string[] {
                            "Eldritch Medley with Twisted Meat",
                            "Anomalous Trio featuring Twisted Meat",
                            "Unsettling Platter with Twisted Meat",
                            "Warped Combination with Twisted Meat"
                        };

                        string name = twistedTrioTemplates[Rand.Range(0, twistedTrioTemplates.Length)];
                        string description = GenerateQualityDescription(ingredients, qualityString);
                        description += " The twisted meat gives this dish a strange, otherworldly quality that's both fascinating and slightly disturbing.";

                        result = new DishInfo(name, description);
                    }
                    else
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
                            string description = GenerateQualityDescription(ingredients, "Lavish");

                            result = new DishInfo(name, description);
                        }
                        else if (mealQuality == "Fine")
                        {
                            string[] ingredientLabels = ingredients
                                .Select(i => StringUtils.GetCapitalizedLabel(i.label).ToLower())
                                .ToArray();

                            string[] fineTrioTemplates = new string[] {
                                $"Quality Blend of {ingredientLabels[0]}, {ingredientLabels[1]}, and {ingredientLabels[2]}",
                                $"Fine {primaryLabel} Medley",
                                $"Well-crafted {primaryLabel} Trio",
                                $"Superior {primaryLabel} Composition"
                            };

                            string name = fineTrioTemplates[Rand.Range(0, fineTrioTemplates.Length)];
                            string description = GenerateQualityDescription(ingredients, "Fine");

                            result = new DishInfo(name, description);
                        }
                        else
                        {
                            string[] ingredientLabels = ingredients
                                .Select(i => StringUtils.GetCapitalizedLabel(i.label).ToLower())
                                .ToArray();

                            string[] simpleTrioTemplates = new string[] {
                                $"Simple Mix of {ingredientLabels[0]}, {ingredientLabels[1]}, and {ingredientLabels[2]}",
                                $"Basic {primaryLabel} Combination",
                                $"{primaryLabel} with Extras",
                                $"Three-Ingredient {primaryLabel} Dish"
                            };

                            string name = simpleTrioTemplates[Rand.Range(0, simpleTrioTemplates.Length)];
                            string description = GenerateQualityDescription(ingredients, "Simple");

                            result = new DishInfo(name, description);
                        }
                    }
                }

                // If we got a result but it has no description, generate one
                if (result != null && string.IsNullOrEmpty(result.Description))
                {
                    string description = GenerateQualityDescription(ingredients, qualityString);

                    if (hasTwistedMeat)
                    {
                        description += " The twisted meat gives this dish a strange, otherworldly quality.";
                    }

                    result = new DishInfo(result.Name, description);
                }
            }
            else
            {
                // For more complex combinations with 4+ ingredients
                if (!string.IsNullOrEmpty(mealQuality))
                {
                    if (hasTwistedMeat)
                    {
                        // Special name for meals with twisted meat and many ingredients
                        string[] twistedMealTemplates;

                        if (mealQuality == "Lavish")
                        {
                            twistedMealTemplates = new string[] {
                                "Eldritch Feast with Twisted Meat",
                                "Anomalous Banquet featuring Twisted Meat",
                                "Otherworldly Gourmet Selection with Twisted Meat",
                                "Unsettling Culinary Creation"
                            };
                        }
                        else if (mealQuality == "Fine")
                        {
                            twistedMealTemplates = new string[] {
                                "Refined Dish with Twisted Meat",
                                "Quality Preparation featuring Twisted Meat",
                                "Well-crafted Twisted Meat Platter",
                                "Superior Meal with Otherworldly Meat"
                            };
                        }
                        else
                        {
                            twistedMealTemplates = new string[] {
                                "Simple Dish with Twisted Meat",
                                "Basic Twisted Meat Preparation",
                                "Plain Meal with Otherworldly Meat",
                                "Modest Dish with Twisted Meat"
                            };
                        }

                        string name = twistedMealTemplates[Rand.Range(0, twistedMealTemplates.Length)];
                        string description = GenerateQualityDescription(ingredients, qualityString);
                        description += " The twisted meat adds an unsettling yet intriguing element to the dish.";

                        result = new DishInfo(name, description);
                    }
                    else
                    {
                        string primaryLabel = StringUtils.GetCapitalizedLabel(ingredients[0].label);
                        string[] mealTemplates;

                        if (mealQuality == "Lavish")
                        {
                            mealTemplates = new string[] {
                                $"Gourmet {primaryLabel} Feast",
                                $"Chef's Grand Selection",
                                $"Luxurious Culinary Medley",
                                $"Extravagant Multi-Ingredient Creation",
                                $"Decadent {primaryLabel} Showcase"
                            };
                        }
                        else if (mealQuality == "Fine")
                        {
                            mealTemplates = new string[] {
                                $"Quality {primaryLabel} Mix",
                                $"Fine Multi-Ingredient Blend",
                                $"Well-prepared {primaryLabel} Medley",
                                $"Superior Culinary Combination",
                                $"Refined {primaryLabel} Selection"
                            };
                        }
                        else
                        {
                            mealTemplates = new string[] {
                                $"Simple {primaryLabel} Mix",
                                $"Basic Multi-Ingredient Dish",
                                $"{primaryLabel} Combination Meal",
                                $"Plain {primaryLabel} Medley",
                                $"Modest {primaryLabel} Creation"
                            };
                        }

                        string name = mealTemplates[Rand.Range(0, mealTemplates.Length)];
                        string description = GenerateQualityDescription(ingredients, qualityString);

                        result = new DishInfo(name, description);
                    }
                }
                else
                {
                    // Default for missing quality with many ingredients
                    result = null;
                }
            }

            Rand.PopState(); // Restore previous random state

            // Ensure the description is quality-appropriate
            if (result != null && !string.IsNullOrEmpty(result.Description))
            {
                // Check if the description contains contradictory quality terms
                if (mealQuality == "Lavish" &&
                    (result.Description.Contains("simple dish") ||
                     result.Description.Contains("basic dish") ||
                     result.Description.Contains("plain dish")))
                {
                    // Replace with quality-appropriate description
                    result = new DishInfo(result.Name, GenerateQualityDescription(ingredients, "Lavish"));
                }
                else if (mealQuality == "Fine" &&
                    (result.Description.Contains("simple dish") ||
                     result.Description.Contains("basic dish") ||
                     result.Description.Contains("plain dish")))
                {
                    // Replace with quality-appropriate description
                    result = new DishInfo(result.Name, GenerateQualityDescription(ingredients, "Fine"));
                }
            }

            return result;
        }

        /// <summary>
        /// Generate a quality-appropriate description for a dish
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="quality">The quality<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GenerateQualityDescription(List<ThingDef> ingredients, string quality)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "A mysterious meal with unknown ingredients.";

            // Get the quality format list or default to simple
            List<string> formats = QualityDescriptionFormats.ContainsKey(quality)
                ? QualityDescriptionFormats[quality]
                : QualityDescriptionFormats["Simple"];

            // Choose a random format
            string format = formats.RandomElement();

            // Format ingredient list with quality-appropriate adjectives
            string ingredientList = IngredientUtils.FormatIngredientsList(ingredients);

            // Add quality-appropriate adjectives occasionally
            if (Rand.Value < 0.3f && QualityIngredientAdjectives.ContainsKey(quality))
            {
                string adjective = QualityIngredientAdjectives[quality].RandomElement();

                // Avoid adding the adjective if it's already in the ingredientList
                if (!ingredientList.Contains(adjective))
                {
                    // Find the first ingredient
                    Match match = Regex.Match(ingredientList, @"^([a-z]+)");
                    if (match.Success)
                    {
                        string firstIngredient = match.Groups[1].Value;
                        ingredientList = ingredientList.Replace(firstIngredient, $"{adjective} {firstIngredient}");
                    }
                }
            }

            // Build the description
            return string.Format(format, ingredientList);
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

            // Special case for Twisted Meat
            if (ingredientDefName.Contains("Twisted") || ingredientDefName.Contains("twisted"))
            {
                // Create a special dish info for twisted meat
                string name;
                string description;

                if (mealQuality == "Lavish")
                {
                    name = "Eldritch Twisted Meat Delicacy";
                    description = "A lavishly prepared dish centered around twisted meat. It has an otherworldly appearance and an unsettling, yet somehow enticing aroma.";
                }
                else if (mealQuality == "Fine")
                {
                    name = "Refined Twisted Meat Dish";
                    description = "A well-prepared dish featuring twisted meat. The unusual meat has been skillfully cooked to minimize its disturbing qualities while preserving its unique flavor.";
                }
                else
                {
                    name = "Twisted Meat Preparation";
                    description = "A simple dish made with twisted meat. The meat's strange, otherworldly properties remain evident despite cooking.";
                }

                return new DishInfo(name, description);
            }

            // Try quality-specific lookup first if quality is provided
            if (!string.IsNullOrEmpty(mealQuality) &&
                IngredientToQualityDishEntries.TryGetValue(ingredientDefName, out var qualityDict) &&
                qualityDict.TryGetValue(mealQuality, out List<DishEntry> qualityDishEntries) &&
                qualityDishEntries.Count > 0)
            {
                Log.Message($"[CustomFoodNames] Found quality-specific entry for {ingredientDefName}");
                var entry = qualityDishEntries.RandomElement();

                // Generate a quality-appropriate description if needed
                string description = entry.Description;
                if (string.IsNullOrEmpty(description))
                {
                    var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
                    if (ingredientDef != null)
                    {
                        List<ThingDef> ingredients = new List<ThingDef> { ingredientDef };
                        description = GenerateQualityDescription(ingredients, mealQuality);
                    }
                }

                return new DishInfo(entry.Name, description);
            }

            // Try direct lookup
            if (IngredientToDishEntries.TryGetValue(ingredientDefName, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                Log.Message($"[CustomFoodNames] Found basic entry for {ingredientDefName}");
                var entry = dishEntries.RandomElement();

                // Generate a quality-appropriate description if needed
                string description = entry.Description;
                if (string.IsNullOrEmpty(description))
                {
                    var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
                    if (ingredientDef != null)
                    {
                        List<ThingDef> ingredients = new List<ThingDef> { ingredientDef };
                        description = GenerateQualityDescription(ingredients, mealQuality ?? "Simple");
                    }
                }
                else if (!string.IsNullOrEmpty(mealQuality) && mealQuality != "Simple")
                {
                    // If we have a quality specified and the dish entry is from the general database,
                    // we might need to adjust the description quality
                    var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
                    if (ingredientDef != null &&
                        (description.Contains("simple dish") ||
                         description.Contains("basic dish") ||
                         description.Contains("plain dish")))
                    {
                        List<ThingDef> ingredients = new List<ThingDef> { ingredientDef };
                        description = GenerateQualityDescription(ingredients, mealQuality);
                    }
                }

                return new DishInfo(entry.Name, description);
            }

            // Try case-insensitive lookup
            var matchingKey = IngredientToDishEntries.Keys.FirstOrDefault(k =>
                string.Equals(k, ingredientDefName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null && IngredientToDishEntries[matchingKey].Count > 0)
            {
                var entry = IngredientToDishEntries[matchingKey].RandomElement();

                // Generate a quality-appropriate description if needed
                string description = entry.Description;
                if (string.IsNullOrEmpty(description))
                {
                    var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
                    if (ingredientDef != null)
                    {
                        List<ThingDef> ingredients = new List<ThingDef> { ingredientDef };
                        description = GenerateQualityDescription(ingredients, mealQuality ?? "Simple");
                    }
                }
                else if (!string.IsNullOrEmpty(mealQuality) && mealQuality != "Simple")
                {
                    // If we have a quality specified and the dish entry is from the general database,
                    // we might need to adjust the description quality
                    var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
                    if (ingredientDef != null &&
                        (description.Contains("simple dish") ||
                         description.Contains("basic dish") ||
                         description.Contains("plain dish")))
                    {
                        List<ThingDef> ingredients = new List<ThingDef> { ingredientDef };
                        description = GenerateQualityDescription(ingredients, mealQuality);
                    }
                }

                return new DishInfo(entry.Name, description);
            }

            // No exact match found, try to get the ingredient label and generate a name
            var ingredientDef2 = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
            if (ingredientDef2 != null)
            {
                // Log missing ingredient only once
                if (LogMissingIngredients && !reportedMissingIngredients.Contains(ingredientDefName))
                {
                    Log.Message($"[CustomFoodNames] Missing dish name for ingredient: {ingredientDefName} (label: {ingredientDef2.label})");
                    reportedMissingIngredients.Add(ingredientDefName);
                }

                // Clean up the ingredient label
                string cleanLabel = StringUtils.GetCapitalizedLabel(ingredientDef2.label);

                // Select a name pattern appropriate for the meal quality
                string pattern;

                if (mealQuality == "Lavish")
                {
                    pattern = "Gourmet " + cleanLabel;
                }
                else if (mealQuality == "Fine")
                {
                    pattern = "Quality " + cleanLabel + " dish";
                }
                else
                {
                    // For simple meals, use the standard patterns
                    pattern = FallbackPatterns.RandomElement();
                    pattern = string.Format(pattern, cleanLabel);
                }

                // Generate a quality-appropriate description
                List<ThingDef> ingredients = new List<ThingDef> { ingredientDef2 };
                string description = GenerateQualityDescription(ingredients, mealQuality ?? "Simple");

                return new DishInfo(pattern, description);
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

        /// <summary>
        /// Check if ingredient is twisted meat
        /// </summary>
        /// <param name="ingredient">The ingredient<see cref="ThingDef"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private static bool IsTwistedMeat(ThingDef ingredient)
        {
            if (ingredient == null || ingredient.defName == null)
                return false;

            return ingredient.defName.Contains("TwistedMeat") ||
                   ingredient.defName.Contains("Meat_Twisted") ||
                   (ingredient.label != null && ingredient.label.ToLower().Contains("twisted meat"));
        }

        /// <summary>
        /// Check if any ingredient in the list is twisted meat
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private static bool HasTwistedMeat(List<ThingDef> ingredients)
        {
            return ingredients.Any(IsTwistedMeat);
        }
    }
}
