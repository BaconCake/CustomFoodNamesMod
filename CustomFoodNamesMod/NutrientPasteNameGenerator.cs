using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static CustomFoodNamesMod.IngredientCategoryResolver;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Generates funny, slightly disgusting names for nutrient paste meals based on ingredients
    /// </summary>
    public static class NutrientPasteNameGenerator
    {
        #region Name Components

        // Derogatory terms for paste
        private static readonly List<string> PasteTerms = new List<string>
        {
            "Paste",
            "Sludge",
            "Slop",
            "Goo",
            "Muck",
            "Glop",
            "Mash",
            "Pulp",
            "Muckpile",
            "Gruel",
            "Slurry",
            "Mush",
            "Ooze",
            "Glue",
            "Slog",
            "Mulch"
        };

        // Category-specific descriptors for nutrient paste
        private static readonly Dictionary<IngredientCategory, List<string>> CategoryDescriptors =
            new Dictionary<IngredientCategory, List<string>>
            {
                {
                    IngredientCategory.Protein,
                    new List<string> { "Protein", "Meat", "Flesh" }
                },
                {
                    IngredientCategory.Carb,
                    new List<string> { "Starch", "Carb", "Grain" }
                },
                {
                    IngredientCategory.Vegetable,
                    new List<string> { "Vegetable", "Plant", "Greens" }
                },
                {
                    IngredientCategory.Dairy,
                    new List<string> { "Dairy", "Milk", "Cream" }
                },
                {
                    IngredientCategory.Fat,
                    new List<string> { "Fat", "Grease", "Oil" }
                },
                {
                    IngredientCategory.Sweetener,
                    new List<string> { "Sweet", "Sugar", "Syrup" }
                },
                {
                    IngredientCategory.Flavoring,
                    new List<string> { "Spice", "Herb", "Flavor" }
                },
                {
                    IngredientCategory.Exotic,
                    new List<string> { "Mystery", "Exotic", "Strange" }
                },
                {
                    IngredientCategory.Other,
                    new List<string> { "Unidentified", "Unknown", "Mysterious" }
                }
            };

        #endregion

        /// <summary>
        /// Generate a disgusting but funny name for a nutrient paste meal
        /// </summary>
        public static string GenerateNutrientPasteName(List<ThingDef> ingredients)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Nutrient Paste";

            // Get most frequent ingredient category
            IngredientCategory dominantCategory = GetDominantCategory(ingredients);

            // Get a representative ingredient from the dominant category
            ThingDef dominantIngredient = GetRepresentativeIngredient(ingredients, dominantCategory);

            // Get the processed ingredient name
            string ingredientName;

            if (dominantIngredient != null)
            {
                // Use the actual ingredient name
                ingredientName = CleanIngredientLabel(dominantIngredient.label);
            }
            else
            {
                // Use a generic category name
                ingredientName = CategoryDescriptors[dominantCategory].RandomElement();
            }

            // Choose a random paste term
            string pasteTerm = PasteTerms.RandomElement();

            // Combine for base name
            string baseName = $"{ingredientName} {pasteTerm}";

            return baseName;
        }

        /// <summary>
        /// Get a representative ingredient from a specific category
        /// </summary>
        private static ThingDef GetRepresentativeIngredient(List<ThingDef> ingredients, IngredientCategory category)
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
        /// Clean up an ingredient label for better name generation
        /// </summary>
        private static string CleanIngredientLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "Mystery";

            // Remove "raw" prefix
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                label,
                @"^raw\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Some specific replacements
            cleaned = cleaned.Replace("meat", "");
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Trim();

            // Ensure first letter is capitalized
            if (cleaned.Length > 0)
            {
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }
    }
}