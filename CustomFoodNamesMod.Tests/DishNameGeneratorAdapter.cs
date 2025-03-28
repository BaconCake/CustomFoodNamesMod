using System;
using System.Collections.Generic;

namespace CustomFoodNamesMod.Tests
{
    /// <summary>
    /// This adapter class allows testing dish name generation logic 
    /// without requiring the actual RimWorld environment
    /// </summary>
    public static class DishNameGeneratorAdapter
    {
        /// <summary>
        /// Generate a dish name based on mocked ingredients
        /// </summary>
        public static string GenerateDishName(List<DishNameGeneratorTests.MockThingDef> ingredients,
                                             DishNameGeneratorTests.MockThingDef mealDef)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Dish";

            // Determine if meal is simple, fine, or lavish
            string mealQuality = GetMealQuality(mealDef);

            // Get primary ingredient
            var primaryIngredient = ingredients[0];
            string primaryLabel = CleanIngredientName(primaryIngredient.label);

            // Templates based on meal quality
            List<string> templates = new List<string>();

            if (mealQuality == "Lavish")
            {
                templates.Add("Gourmet {0} Platter");
                templates.Add("Chef's Special {0}");
                templates.Add("Exquisite {0} Dish");
                templates.Add("Luxurious {0} Creation");
            }
            else if (mealQuality == "Fine")
            {
                templates.Add("Quality {0} Dish");
                templates.Add("Well-Prepared {0}");
                templates.Add("Fine {0} Plate");
                templates.Add("Seasoned {0} Meal");
            }
            else // Simple
            {
                templates.Add("Basic {0} Dish");
                templates.Add("Simple {0} Meal");
                templates.Add("{0} Plate");
                templates.Add("Plain {0}");
            }

            // Handle multiple ingredients
            if (ingredients.Count > 1)
            {
                var secondaryIngredient = ingredients[1];
                string secondaryLabel = CleanIngredientName(secondaryIngredient.label);

                List<string> comboTemplates = new List<string>
                {
                    "{0} with {1}",
                    "{0} and {1} Plate",
                    "Mixed {0} and {1}",
                    "{0} {1} Dish"
                };

                Random random = new Random();
                string template = comboTemplates[random.Next(comboTemplates.Count)];
                return string.Format(template, primaryLabel, secondaryLabel);
            }

            // Single ingredient
            Random rand = new Random();
            string singleTemplate = templates[rand.Next(templates.Count)];
            return string.Format(singleTemplate, primaryLabel);
        }

        /// <summary>
        /// Generate a nutrient paste meal name based on mocked ingredients
        /// </summary>
        public static string GenerateNutrientPasteName(List<DishNameGeneratorTests.MockThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Nutrient Paste";

            var primaryIngredient = ingredients[0];
            string ingredientLabel = CleanIngredientName(primaryIngredient.label);

            string[] pasteTerms = {
                "Paste", "Sludge", "Slop", "Goo", "Muck",
                "Glop", "Mash", "Pulp", "Gruel", "Slurry"
            };

            Random rand = new Random();
            string pasteTerm = pasteTerms[rand.Next(pasteTerms.Length)];

            return $"{ingredientLabel} {pasteTerm}";
        }

        /// <summary>
        /// Extract the meal quality from the meal def
        /// </summary>
        private static string GetMealQuality(DishNameGeneratorTests.MockThingDef mealDef)
        {
            if (mealDef.defName.Contains("Lavish"))
                return "Lavish";
            else if (mealDef.defName.Contains("Fine"))
                return "Fine";
            else
                return "Simple";
        }

        /// <summary>
        /// Clean up an ingredient label for better display
        /// </summary>
        private static string CleanIngredientName(string label)
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
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Replace(" meat", "");

            // Capitalize first letter
            if (cleaned.Length > 0)
            {
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned.Trim();
        }
    }
}