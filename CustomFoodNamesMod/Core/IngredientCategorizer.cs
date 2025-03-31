using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomFoodNamesMod.Core
{
    /// <summary>
    /// Unified system for categorizing ingredients
    /// </summary>
    public static class IngredientCategorizer
    {
        // Cache of ingredient categories to avoid recalculating
        private static Dictionary<string, IngredientCategory> ingredientCategoryCache =
            new Dictionary<string, IngredientCategory>();

        // Special case ingredients with their own dedicated naming logic
        public static HashSet<string> specialCaseIngredients = new HashSet<string>
        {
            // Removed RawPotatoes from special cases
            "RawRice",
            "Milk",
            "InsectJelly",
            "Chocolate"
        };

        // Initialize the categorizer
        static IngredientCategorizer()
        {
            // Pre-categorize some common ingredients for faster lookup
            PreCategorizeCommonIngredients();
        }

        /// <summary>
        /// Determine the category of an ingredient
        /// </summary>
        public static IngredientCategory GetIngredientCategory(ThingDef ingredient)
        {
            if (ingredient == null)
                return IngredientCategory.Other;

            // Check cache first
            if (ingredientCategoryCache.TryGetValue(ingredient.defName, out IngredientCategory cachedCategory))
                return cachedCategory;

            // Determine category based on various factors
            IngredientCategory category = DetermineCategory(ingredient);

            // Cache the result
            ingredientCategoryCache[ingredient.defName] = category;

            return category;
        }

        /// <summary>
        /// Logic to determine the category of an ingredient
        /// </summary>
        private static IngredientCategory DetermineCategory(ThingDef ingredient)
        {
            string defName = ingredient.defName;

            // Special case ingredients have priority
            if (specialCaseIngredients.Contains(defName))
                return IngredientCategory.Special;

            // Check for meat - treat all meat the same including human and thrumbo
            if (defName.StartsWith("Meat_"))
            {
                return IngredientCategory.Meat;
            }

            // Handle twisted meat as a special case of meat
            if (defName.Contains("TwistedMeat") || (ingredient.label != null && ingredient.label.Contains("twisted meat")))
            {
                return IngredientCategory.Meat;
            }

            // Check for eggs
            if (defName.StartsWith("Egg"))
                return IngredientCategory.Egg;

            // Check for Dairy
            if (defName == "Milk")
                return IngredientCategory.Dairy;

            // Check for grains
            if (defName == "RawRice" || defName == "RawCorn")
                return IngredientCategory.Grain;

            // Check for fruits
            if (defName == "RawBerries" || defName.Contains("Fruit") ||
                defName.Contains("Berry") || defName == "RawAgave")
                return IngredientCategory.Fruit;

            // Check for fungi
            if (defName == "RawFungus" || defName.Contains("Mushroom") ||
                defName.Contains("Fungus") || defName == "Glowstool")
                return IngredientCategory.Fungus;

            // Check for potatoes specifically
            if (defName == "RawPotatoes")
                return IngredientCategory.Vegetable;

            // Default assumption for Raw* is vegetable
            if (defName.StartsWith("Raw"))
                return IngredientCategory.Vegetable;

            // Default category if we can't determine
            return IngredientCategory.Other;
        }

        /// <summary>
        /// Find the dominant ingredient from a list of ingredients
        /// </summary>
        public static ThingDef GetDominantIngredient(List<ThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return null;

            // First check for special ingredients
            foreach (var ingredient in ingredients)
            {
                if (specialCaseIngredients.Contains(ingredient.defName))
                    return ingredient;
            }

            // Group by category and find the most common category
            var categoryGroups = ingredients
                .GroupBy(i => GetIngredientCategory(i))
                .OrderByDescending(g => g.Count());

            var dominantCategory = categoryGroups.First().Key;

            // Pick a random ingredient from the dominant category
            var dominantIngredients = ingredients
                .Where(i => GetIngredientCategory(i) == dominantCategory)
                .ToList();

            // Otherwise just pick any from the dominant category
            return dominantIngredients.RandomElement();
        }

        /// <summary>
        /// Get all ingredients that match a specific category
        /// </summary>
        public static List<ThingDef> GetIngredientsOfCategory(List<ThingDef> ingredients, IngredientCategory category)
        {
            return ingredients
                .Where(i => GetIngredientCategory(i) == category)
                .ToList();
        }

        /// <summary>
        /// Get the primary category of a meal based on its ingredients
        /// </summary>
        public static IngredientCategory GetPrimaryMealCategory(List<ThingDef> ingredients)
        {
            // If no ingredients, return Other
            if (ingredients == null || ingredients.Count == 0)
                return IngredientCategory.Other;

            // Group by category and count
            var categoryCounts = new Dictionary<IngredientCategory, int>();

            foreach (var ingredient in ingredients)
            {
                var category = GetIngredientCategory(ingredient);

                if (!categoryCounts.ContainsKey(category))
                    categoryCounts[category] = 0;

                categoryCounts[category]++;
            }

            // Get the most common category (excluding Other)
            return categoryCounts
                .Where(kvp => kvp.Key != IngredientCategory.Other)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get all distinct categories present in a list of ingredients
        /// </summary>
        public static List<IngredientCategory> GetAllCategories(List<ThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return new List<IngredientCategory>();

            return ingredients
                .Select(i => GetIngredientCategory(i))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Get a representative ingredient from a specific category
        /// </summary>
        public static ThingDef GetRepresentativeIngredient(List<ThingDef> ingredients, IngredientCategory category)
        {
            // Get all ingredients of this category
            var categoryIngredients = GetIngredientsOfCategory(ingredients, category);

            if (categoryIngredients.Count == 0)
                return null;

            // Find the most common ingredient in this category
            return categoryIngredients.GroupBy(i => i.defName)
                .OrderByDescending(g => g.Count())
                .First()
                .First();
        }

        /// <summary>
        /// Precategorize common ingredients for faster lookups
        /// </summary>
        private static void PreCategorizeCommonIngredients()
        {
            // Meats - now all treated the same
            ingredientCategoryCache["Meat_Cow"] = IngredientCategory.Meat;
            ingredientCategoryCache["Meat_Chicken"] = IngredientCategory.Meat;
            ingredientCategoryCache["Meat_Pig"] = IngredientCategory.Meat;
            ingredientCategoryCache["Meat_Human"] = IngredientCategory.Meat; // Now regular meat
            ingredientCategoryCache["Meat_Thrumbo"] = IngredientCategory.Meat; // Now regular meat

            // Handle twisted meat variants
            ingredientCategoryCache["TwistedMeat"] = IngredientCategory.Meat;
            ingredientCategoryCache["Meat_Twisted"] = IngredientCategory.Meat;

            // Vegetables
            ingredientCategoryCache["RawPotatoes"] = IngredientCategory.Vegetable; // Changed from Special to Vegetable

            // Grains
            ingredientCategoryCache["RawRice"] = IngredientCategory.Grain;
            ingredientCategoryCache["RawCorn"] = IngredientCategory.Grain;

            // Fruits
            ingredientCategoryCache["RawBerries"] = IngredientCategory.Fruit;

            // Fungi
            ingredientCategoryCache["RawFungus"] = IngredientCategory.Fungus;

            // Dairy
            ingredientCategoryCache["Milk"] = IngredientCategory.Dairy;

            // Eggs
            ingredientCategoryCache["EggChickenUnfertilized"] = IngredientCategory.Egg;
            ingredientCategoryCache["EggChickenFertilized"] = IngredientCategory.Egg;
        }
    }
}