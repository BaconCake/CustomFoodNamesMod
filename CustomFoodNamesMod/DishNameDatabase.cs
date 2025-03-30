using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using CustomFoodNamesMod.Database;
using CustomFoodNamesMod.Utils;

namespace CustomFoodNamesMod
{
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
        private static readonly List<string> FallbackPatterns = new List<string>
        {
            "{0} dish",
            "Simple {0} meal",
            "{0} preparation",
            "Basic {0} plate",
            "Plain {0} serving"
        };

        // Track missing ingredients we've seen for logging purposes
        private static HashSet<string> reportedMissingIngredients = new HashSet<string>();

        // Flag to enable/disable logging of missing ingredients
        public static bool LogMissingIngredients = false;

        /// <summary>
        /// Initialize the database
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
                        result = new DishInfo(
                            result.Name + " with " + StringUtils.GetCapitalizedLabel(ingredients[1].label),
                            result.Description + " " + StringUtils.GetCapitalizedLabel(ingredients[1].label) + " adds a complementary flavor.");
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
            }
            else
            {
                // For more complex combinations, return null
                result = null;
            }

            Rand.PopState(); // Restore previous random state
            return result;
        }

        /// <summary>
        /// Gets a dish name for a list of ingredients, using the database for small combos
        /// and falling back to procedural generation for complex combinations
        /// </summary>
        public static string GetDishNameForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            DishInfo info = GetDishInfoForIngredients(ingredients, mealQuality);

            if (info != null)
                return info.Name;

            // Fall back to a simple name if no dish info found
            if (ingredients.Count > 0)
                return StringUtils.GetCapitalizedLabel(ingredients[0].label) + " dish";

            return "Mystery meal";
        }

        /// <summary>
        /// Gets dish info for a single ingredient, with fallback to generated names
        /// </summary>
        public static DishInfo GetRandomDishInfo(string ingredientDefName, string mealQuality = null)
        {
            // Try quality-specific lookup first if quality is provided
            if (!string.IsNullOrEmpty(mealQuality) &&
                IngredientToQualityDishEntries.TryGetValue(ingredientDefName, out var qualityDict) &&
                qualityDict.TryGetValue(mealQuality, out List<DishEntry> qualityDishEntries) &&
                qualityDishEntries.Count > 0)
            {
                var entry = qualityDishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // Try direct lookup
            if (IngredientToDishEntries.TryGetValue(ingredientDefName, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
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