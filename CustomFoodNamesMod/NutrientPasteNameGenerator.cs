using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static CustomFoodNamesMod.IngredientCategorizer;

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

        #endregion

        /// <summary>
        /// Generate a disgusting but funny name for a nutrient paste meal
        /// </summary>
        public static string GenerateNutrientPasteName(List<ThingDef> ingredients)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Nutrient Paste";

            // Get most frequent ingredient type
            var dominantIngredient = GetDominantIngredient(ingredients);

            // Get the processed ingredient name
            string ingredientName = CleanIngredientLabel(dominantIngredient.label);

            // Choose a random paste term
            string pasteTerm = PasteTerms.RandomElement();

            // Combine for base name
            string baseName = $"{ingredientName} {pasteTerm}";

            return baseName;
        }

        /// <summary>
        /// Get the most dominant ingredient (the one that appears most frequently)
        /// </summary>
        private static ThingDef GetDominantIngredient(List<ThingDef> ingredients)
        {
            // Group by defName and count occurrences
            var groupedIngredients = ingredients
                .GroupBy(i => i.defName)
                .Select(g => new { DefName = g.Key, Count = g.Count(), Ingredient = g.First() })
                .OrderByDescending(g => g.Count)
                .ToList();

            // Return the most frequent ingredient
            return groupedIngredients.First().Ingredient;
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