﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static CustomFoodNamesMod.IngredientCategorizer;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Generates procedural dish names based on ingredients
    /// </summary>
    public static class ProceduralDishNameGenerator
    {
        #region Name Templates

        // Template formats for meat-based dishes
        private static readonly List<string> MeatDishTemplates = new List<string>
        {
            "{0} Stew",
            "Braised {0}",
            "{0} Roast",
            "{0} with {1}",
            "Seared {0}",
            "{0} {1} Pot",
            "{0} in {1} Sauce",
            "Slow-cooked {0}",
            "{0} Stir-fry",
            "Spiced {0}",
            "{0} Casserole"
        };

        // Template formats for vegetable-based dishes
        private static readonly List<string> VegetableDishTemplates = new List<string>
        {
            "{0} Medley",
            "Seasoned {0}",
            "{0} and {1} Mix",
            "{0} Stir-fry",
            "Roasted {0}",
            "{0} Soup",
            "Steamed {0}",
            "{0} with {1} Garnish",
            "Garden {0} Plate",
            "{0} Salad"
        };

        // Template formats for grain-based dishes
        private static readonly List<string> GrainDishTemplates = new List<string>
        {
            "{0} Pilaf",
            "{0} with {1}",
            "{0} Porridge",
            "Seasoned {0}",
            "{0} Bowl",
            "{0} and {1} Mix",
            "{0} Risotto"
        };

        // Template formats for mixed dishes
        private static readonly List<string> MixedDishTemplates = new List<string>
        {
            "{0} and {1} Plate",
            "{0} with {1} Side",
            "Colony {0} Special",
            "{0} {1} Medley",
            "Frontier {0} with {1}",
            "Settler's {0} and {1}",
            "Homestead {0} Dish",
            "Rimworld {0} Platter"
        };

        // Templates for exotic ingredient dishes
        private static readonly List<string> ExoticDishTemplates = new List<string>
        {
            "Exotic {0} Delicacy",
            "Rare {0} Preparation",
            "Gourmet {0} with {1}",
            "Special {0} Dish",
            "Unusual {0} Recipe",
            "{0} Chef's Creation"
        };

        // Templates for fine meal qualifiers
        private static readonly List<string> FineMealPrefixes = new List<string>
        {
            "Delicious",
            "Fine",
            "Quality",
            "Refined",
            "Fancy",
            "Gourmet",
            "Artisanal",
            "Select"
        };

        // Templates for lavish meal qualifiers
        private static readonly List<string> LavishMealPrefixes = new List<string>
        {
            "Exquisite",
            "Lavish",
            "Luxurious",
            "Gourmet",
            "Sumptuous",
            "Decadent",
            "Opulent",
            "Extravagant",
            "Magnificent"
        };

        // Cooking methods for variety
        private static readonly List<string> CookingMethods = new List<string>
        {
            "Roasted",
            "Seared",
            "Grilled",
            "Sautéed",
            "Braised",
            "Pan-fried",
            "Steamed",
            "Stewed",
            "Baked",
            "Fire-roasted"
        };

        // Sauce types for meat dishes
        private static readonly List<string> SauceTypes = new List<string>
        {
            "Rich",
            "Herb",
            "Savory",
            "Spiced",
            "Creamy",
            "Tangy",
            "Sweet",
            "Peppery",
            "Red Wine",
            "Umami"
        };

        #endregion

        /// <summary>
        /// Generate a dish name based on ingredients and meal quality
        /// </summary>
        public static string GenerateDishName(List<ThingDef> ingredients, ThingDef mealDef)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Dish";

            // First try the database for simple or specific combinations
            if (ingredients.Count <= 2)
            {
                string databaseName = DishNameDatabase.GetDishNameForIngredients(ingredients);
                if (!string.IsNullOrEmpty(databaseName))
                    return databaseName;
            }

            // Determine meal quality based on its def name
            MealQuality mealQuality = DetermineMealQuality(mealDef);

            // Generate a procedural name
            return GenerateProceduralName(ingredients, mealQuality);
        }

        /// <summary>
        /// Determine the quality level of a meal
        /// </summary>
        private static MealQuality DetermineMealQuality(ThingDef mealDef)
        {
            if (mealDef == null)
                return MealQuality.Simple;

            string defName = mealDef.defName;

            if (defName.Contains("Lavish"))
                return MealQuality.Lavish;
            else if (defName.Contains("Fine"))
                return MealQuality.Fine;
            else
                return MealQuality.Simple;
        }

        /// <summary>
        /// Core method to generate a procedural dish name
        /// </summary>
        private static string GenerateProceduralName(List<ThingDef> ingredients, MealQuality mealQuality)
        {
            // Get the primary category and dominant ingredients
            IngredientCategory primaryCategory = GetPrimaryMealCategory(ingredients);

            // Get top one or two ingredients to feature in the name
            List<ThingDef> dominantIngredients = GetDominantIngredients(ingredients, 2);

            // Get clean labels for the dominant ingredients
            List<string> ingredientLabels = dominantIngredients
                .Select(i => CleanIngredientLabel(i.label))
                .ToList();

            // Check if this is vegetarian or carnivore
            bool isVegetarian = IsMealVegetarian(ingredients);
            bool isCarnivore = IsMealCarnivore(ingredients);

            // Generate the name based on ingredients and meal type
            string dishName = GenerateNameByCategory(
                primaryCategory,
                ingredientLabels,
                mealQuality,
                isVegetarian,
                isCarnivore);

            // Add quality-specific prefix for fine/lavish meals
            if (mealQuality == MealQuality.Fine)
            {
                dishName = $"{FineMealPrefixes.RandomElement()} {dishName}";
            }
            else if (mealQuality == MealQuality.Lavish)
            {
                dishName = $"{LavishMealPrefixes.RandomElement()} {dishName}";
            }

            return dishName;
        }

        /// <summary>
        /// Generate a name based on the primary category of ingredients
        /// </summary>
        private static string GenerateNameByCategory(
            IngredientCategory category,
            List<string> ingredientLabels,
            MealQuality quality,
            bool isVegetarian,
            bool isCarnivore)
        {
            // Ensure we have at least one ingredient label
            if (ingredientLabels.Count == 0)
                return "Mystery Dish";

            string primaryIngredient = ingredientLabels[0];

            // Get secondary ingredient if available
            string secondaryIngredient = ingredientLabels.Count > 1
                ? ingredientLabels[1]
                : GetFillerIngredient(category);

            // Special case: exotic ingredients always use exotic templates
            if (category == IngredientCategory.Exotic)
            {
                string exoticTemplate = ExoticDishTemplates.RandomElement();
                return string.Format(exoticTemplate, primaryIngredient, secondaryIngredient);
            }

            // Pick templates based on category
            List<string> templates;
            switch (category)
            {
                case IngredientCategory.Meat:
                    templates = MeatDishTemplates;

                    // For meat dishes, sometimes use cooking method
                    if (Rand.Value < 0.4f)
                    {
                        primaryIngredient = $"{CookingMethods.RandomElement()} {primaryIngredient}";
                    }

                    // For meat dishes, sometimes use sauce
                    if (Rand.Value < 0.3f && secondaryIngredient == "Sauce")
                    {
                        secondaryIngredient = $"{SauceTypes.RandomElement()} {secondaryIngredient}";
                    }
                    break;
                    
                case IngredientCategory.Vegetable:
                case IngredientCategory.Fruit:
                    templates = VegetableDishTemplates;
                    break;

                case IngredientCategory.Grain:
                    templates = GrainDishTemplates;
                    break;

                default:
                    templates = MixedDishTemplates;
                    break;
            }

            // Select a template and fill it
            string template = templates.RandomElement();
            return string.Format(template, primaryIngredient, secondaryIngredient);
        }

        /// <summary>
        /// Get the most significant ingredients from the list
        /// </summary>
        private static List<ThingDef> GetDominantIngredients(List<ThingDef> ingredients, int count)
        {
            var result = new List<ThingDef>();

            // First look for special case ingredients
            foreach (var ingredient in ingredients)
            {
                if (specialCaseIngredients.Contains(ingredient.defName))
                {
                    result.Add(ingredient);
                    if (result.Count >= count)
                        return result;
                }
            }

            // Group by category
            var categoryGroups = ingredients
                .GroupBy(i => GetIngredientCategory(i))
                .OrderByDescending(g => g.Count())
                .ToList();

            // Add dominant ingredients from each category
            foreach (var group in categoryGroups)
            {
                // Skip Other category
                if (group.Key == IngredientCategory.Other)
                    continue;

                // Add an ingredient from this category
                if (group.Any())
                {
                    result.Add(group.RandomElement());
                    if (result.Count >= count)
                        return result;
                }
            }

            // If we still don't have enough, add random ingredients
            while (result.Count < count && result.Count < ingredients.Count)
            {
                var remaining = ingredients.Except(result).ToList();
                if (remaining.Count == 0)
                    break;

                result.Add(remaining.RandomElement());
            }

            return result;
        }

        /// <summary>
        /// Get a generic filler ingredient appropriate for the category
        /// </summary>
        private static string GetFillerIngredient(IngredientCategory category)
        {
            switch (category)
            {
                case IngredientCategory.Meat:
                    return Rand.Element("Herbs", "Spices", "Sauce", "Vegetables");

                case IngredientCategory.Vegetable:
                    return Rand.Element("Herbs", "Spices", "Garnish", "Seasoning");

                case IngredientCategory.Grain:
                    return Rand.Element("Vegetables", "Herbs", "Broth");

                case IngredientCategory.Fruit:
                    return Rand.Element("Cream", "Honey", "Syrup");

                default:
                    return Rand.Element("Seasoning", "Sides", "Garnish", "Spices");
            }
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

        /// <summary>
        /// Quality levels for meals
        /// </summary>
        public enum MealQuality
        {
            Simple,
            Fine,
            Lavish
        }
    }
}