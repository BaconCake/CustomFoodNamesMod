using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Utility functions for generating custom meal descriptions
    /// </summary>
    public static class MealDescriptionUtility
    {
        /// <summary>
        /// Generate a description for any meal type
        /// </summary>
        public static string GenerateMealDescription(string dishName, List<ThingDef> ingredients, ThingDef mealDef)
        {
            if (mealDef.defName.Contains("NutrientPaste"))
            {
                return GenerateNutrientPasteDescription(dishName, ingredients);
            }
            else if (mealDef.defName.Contains("Lavish"))
            {
                return GenerateLavishMealDescription(dishName, ingredients);
            }
            else if (mealDef.defName.Contains("Fine"))
            {
                return GenerateFineMealDescription(dishName, ingredients);
            }
            else
            {
                return GenerateSimpleMealDescription(dishName, ingredients);
            }
        }

        /// <summary>
        /// Generate a description for a nutrient paste meal
        /// </summary>
        private static string GenerateNutrientPasteDescription(string dishName, List<ThingDef> ingredients)
        {
            string ingredientsList = FormatIngredientsList(ingredients);

            return $"This is a {dishName} made from processed {ingredientsList}. " +
                   "The nutritional value is adequate, but the taste leaves much to be desired.";
        }

        /// <summary>
        /// Generate a description for a lavish meal
        /// </summary>
        private static string GenerateLavishMealDescription(string dishName, List<ThingDef> ingredients)
        {
            string ingredientsList = FormatIngredientsList(ingredients);

            return $"This is a {dishName}, a lavishly prepared dish containing {ingredientsList}. " +
                   "It has been expertly crafted to be both nutritious and delicious.";
        }

        /// <summary>
        /// Generate a description for a fine meal
        /// </summary>
        private static string GenerateFineMealDescription(string dishName, List<ThingDef> ingredients)
        {
            string ingredientsList = FormatIngredientsList(ingredients);

            return $"This is a {dishName}, a well-prepared dish containing {ingredientsList}. " +
                   "It has been skillfully made to balance nutrition and taste.";
        }

        /// <summary>
        /// Generate a description for a simple meal
        /// </summary>
        private static string GenerateSimpleMealDescription(string dishName, List<ThingDef> ingredients)
        {
            string ingredientsList = FormatIngredientsList(ingredients);

            return $"This is a {dishName}, a basic dish containing {ingredientsList}. " +
                   "It offers good nutrition although the taste is simple.";
        }

        /// <summary>
        /// Format the ingredients list for display in the description
        /// </summary>
        public static string FormatIngredientsList(List<ThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "unknown ingredients";

            // Group ingredients by name to avoid repetition
            var groupedIngredients = ingredients
                .GroupBy(i => i.defName)
                .Select(g => new {
                    Label = CleanIngredientLabel(g.First().label),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Build a readable list
            var result = new List<string>();
            foreach (var ingredient in groupedIngredients)
            {
                // Only mention count if there's more than one
                if (ingredient.Count > 1)
                    result.Add($"{ingredient.Label} (x{ingredient.Count})");
                else
                    result.Add(ingredient.Label);
            }

            // Format the final list
            if (result.Count == 1)
                return result[0];

            if (result.Count == 2)
                return $"{result[0]} and {result[1]}";

            return string.Join(", ", result.Take(result.Count - 1)) + ", and " + result.Last();
        }

        /// <summary>
        /// Clean up ingredient names for better display
        /// </summary>
        private static string CleanIngredientLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "unknown ingredient";

            // Remove "raw" prefix
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                label,
                @"^raw\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Some specific replacements
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Trim();

            // Ensure first letter is lowercase for listing in text
            if (cleaned.Length > 0)
            {
                cleaned = char.ToLower(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }
    }
}