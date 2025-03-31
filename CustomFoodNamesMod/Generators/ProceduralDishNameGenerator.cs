using System.Collections.Generic;
using System.Linq;
using Verse;
using CustomFoodNamesMod.Core;
using CustomFoodNamesMod.Utils;

namespace CustomFoodNamesMod.Generators
{
    /// <summary>
    /// Generates procedural dish names based on ingredients
    /// </summary>
    public class ProceduralDishNameGenerator : NameGeneratorBase
    {
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

        // Template formats specifically for twisted meat
        private static readonly List<string> TwistedMeatTemplates = new List<string>
        {
            "Eldritch {0} Stew",
            "Anomalous {0} Dish",
            "Warped {0} Roast",
            "Strange {0} Medley",
            "Unsettling {0} Delicacy",
            "Peculiar {0} Creation",
            "Distorted {0} Recipe",
            "Uncanny {0} Platter"
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

        /// <summary>
        /// Generate a dish name based on ingredients and meal quality
        /// </summary>
        public override string GenerateName(List<ThingDef> ingredients, ThingDef mealDef)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Dish";

            // Check for twisted meat first
            bool containsTwistedMeat = HasTwistedMeat(ingredients);

            // Determine meal quality based on its def name
            MealQuality mealQuality = DetermineMealQuality(mealDef);
            string qualityString = mealQuality.ToString();

            // First try the database for simple or specific combinations with quality
            if (ingredients.Count <= 2)
            {
                string databaseName = DishNameDatabase.GetDishNameForIngredients(ingredients, qualityString);
                if (!string.IsNullOrEmpty(databaseName))
                    return databaseName;
            }

            // Generate a procedural name with special handling for twisted meat
            return GenerateProceduralName(ingredients, mealQuality, containsTwistedMeat);
        }

        /// <summary>
        /// Generate a description for the meal
        /// </summary>
        public override string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "A mysterious meal with unknown ingredients.";

            // Check for twisted meat
            bool containsTwistedMeat = HasTwistedMeat(ingredients);

            // Determine meal quality
            MealQuality mealQuality = DetermineMealQuality(mealDef);

            // Get ingredient list for description
            string ingredientList = IngredientUtils.FormatIngredientsList(ingredients);

            // Build appropriate description based on meal quality and ingredients
            string baseDescription;
            switch (mealQuality)
            {
                case MealQuality.Lavish:
                    baseDescription = $"A lavishly prepared dish containing {ingredientList}. It has been expertly crafted to be both nutritious and delicious.";
                    break;
                case MealQuality.Fine:
                    baseDescription = $"A well-prepared dish containing {ingredientList}. It has been skillfully made to balance nutrition and taste.";
                    break;
                default: // Simple
                    baseDescription = $"A basic dish containing {ingredientList}. It offers good nutrition although the taste is simple.";
                    break;
            }

            // Add special description for twisted meat
            if (containsTwistedMeat)
            {
                baseDescription += " The twisted meat gives this dish a strange, otherworldly quality that's both fascinating and slightly disturbing.";
            }

            return baseDescription;
        }

        /// <summary>
        /// Check if the ingredients contain twisted meat
        /// </summary>
        private bool HasTwistedMeat(List<ThingDef> ingredients)
        {
            return ingredients.Any(i =>
                i.defName.Contains("TwistedMeat") ||
                i.defName.Contains("Meat_Twisted") ||
                (i.label != null && i.label.ToLower().Contains("twisted meat")));
        }

        /// <summary>
        /// Core method to generate a procedural dish name
        /// </summary>
        private static string GenerateProceduralName(List<ThingDef> ingredients, MealQuality mealQuality, bool hasTwistedMeat)
        {
            // Get the primary category and dominant ingredients
            IngredientCategory primaryCategory = IngredientCategorizer.GetPrimaryMealCategory(ingredients);

            // Get top one or two ingredients to feature in the name
            List<ThingDef> dominantIngredients = GetDominantIngredients(ingredients, 2);

            // Get clean labels for the dominant ingredients
            List<string> ingredientLabels = dominantIngredients
                .Select(i => StringUtils.GetCapitalizedLabel(i.label))
                .ToList();

            // Check if this is vegetarian or carnivore
            bool isVegetarian = IngredientUtils.IsMealVegetarian(ingredients);
            bool isCarnivore = IngredientUtils.IsMealCarnivore(ingredients);

            // Generate the name based on ingredients and meal type
            string dishName = GenerateNameByCategory(
                primaryCategory,
                ingredientLabels,
                mealQuality,
                isVegetarian,
                isCarnivore,
                hasTwistedMeat);

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
            bool isCarnivore,
            bool hasTwistedMeat)
        {
            // Ensure we have at least one ingredient label
            if (ingredientLabels.Count == 0)
                return "Mystery Dish";

            string primaryIngredient = ingredientLabels[0];

            // Special handling for twisted meat
            if (hasTwistedMeat || primaryIngredient.Contains("Twisted"))
            {
                // Ensure consistent capitalization
                primaryIngredient = "Twisted Meat";

                // Use special twisted meat templates
                string twistedTemplate = TwistedMeatTemplates.RandomElement();
                return string.Format(twistedTemplate, primaryIngredient);
            }

            // Get secondary ingredient if available
            string secondaryIngredient = ingredientLabels.Count > 1
                ? ingredientLabels[1]
                : GetFillerIngredient(category);

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

            // Select a template and fill it - renamed variable to avoid conflict
            string selectedTemplate = templates.RandomElement();
            string dishName = string.Format(selectedTemplate, primaryIngredient, secondaryIngredient);

            // For fine and lavish meals, sometimes add a quality prefix
            if (quality == MealQuality.Fine && Rand.Value < 0.5f)
            {
                dishName = $"{FineMealPrefixes.RandomElement()} {dishName}";
            }
            else if (quality == MealQuality.Lavish)
            {
                dishName = $"{LavishMealPrefixes.RandomElement()} {dishName}";
            }

            return dishName;
        }

        /// <summary>
        /// Get the most significant ingredients from the list
        /// </summary>
        private static List<ThingDef> GetDominantIngredients(List<ThingDef> ingredients, int count)
        {
            var result = new List<ThingDef>();

            // First, prioritize twisted meat if present
            var twistedMeat = ingredients.FirstOrDefault(i =>
                i.defName.Contains("TwistedMeat") ||
                i.defName.Contains("Meat_Twisted") ||
                (i.label != null && i.label.ToLower().Contains("twisted meat")));

            if (twistedMeat != null)
            {
                result.Add(twistedMeat);
                if (result.Count >= count)
                    return result;
            }

            // Then look for special case ingredients
            foreach (var ingredient in ingredients)
            {
                if (IngredientCategorizer.specialCaseIngredients.Contains(ingredient.defName))
                {
                    result.Add(ingredient);
                    if (result.Count >= count)
                        return result;
                }
            }

            // Group by category
            var categoryGroups = ingredients
                .GroupBy(i => IngredientCategorizer.GetIngredientCategory(i))
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
    }
}