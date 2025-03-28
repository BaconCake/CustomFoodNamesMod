using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using static CustomFoodNamesMod.IngredientCategorizer;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Generates procedural descriptions for meals based on their ingredients
    /// </summary>
    public static class MealDescriptionGenerator
    {
        #region Description Templates

        // Templates for meal descriptions
        private static readonly List<string> MealDescriptionTemplates = new List<string>
        {
            "A {0} dish made with {1}.",
            "This {0} meal contains {1}.",
            "A {0} preparation featuring {1}.",
            "A {0} dish combining {1}.",
            "{1} come together in this {0} meal.",
            "A {0} blend of {1}."
        };

        // Templates for vegetarian meal descriptors
        private static readonly List<string> VegetarianDescriptors = new List<string>
        {
            "vegetarian",
            "plant-based",
            "meatless",
            "garden-fresh"
        };

        // Templates for carnivore meal descriptors
        private static readonly List<string> CarnivoreDescriptors = new List<string>
        {
            "meaty",
            "protein-rich",
            "carnivore",
            "hearty"
        };

        // Templates for mixed meal descriptors
        private static readonly List<string> MixedDescriptors = new List<string>
        {
            "balanced",
            "complete",
            "nourishing",
            "satisfying"
        };

        // Templates for simple meal qualifiers
        private static readonly List<string> SimpleDescriptors = new List<string>
        {
            "simple",
            "basic",
            "straightforward",
            "rustic"
        };

        // Templates for fine meal qualifiers
        private static readonly List<string> FineDescriptors = new List<string>
        {
            "fine",
            "quality",
            "well-crafted",
            "skillfully prepared"
        };

        // Templates for lavish meal qualifiers
        private static readonly List<string> LavishDescriptors = new List<string>
        {
            "lavish",
            "luxurious",
            "exquisite",
            "gourmet"
        };

        // Templates for cooking methods
        private static readonly List<string> CookingMethods = new List<string>
        {
            "cooked",
            "prepared",
            "mixed",
            "assembled",
            "crafted"
        };

        // Templates for exotic ingredient mentions
        private static readonly List<string> ExoticMentions = new List<string>
        {
            "The {0} adds a unique flavor.",
            "The rare {0} is a highlight of this dish.",
            "The unusual addition of {0} makes this meal special.",
            "The exotic {0} provides a distinctive taste."
        };

        #endregion

        /// <summary>
        /// Generate a meal description based on its ingredients
        /// </summary>
        public static string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "A mysterious meal with unknown ingredients.";

            // Determine meal quality
            ProceduralDishNameGenerator.MealQuality mealQuality = DetermineMealQuality(mealDef);

            // Check dietary type
            bool isVegetarian = IngredientCategorizer.IsMealVegetarian(ingredients);
            bool isCarnivore = IngredientCategorizer.IsMealCarnivore(ingredients);

            // Get ingredient lists by category
            var meatIngredients = GetIngredientsOfCategory(ingredients, IngredientCategory.Meat);
            var vegIngredients = GetIngredientsOfCategory(ingredients, IngredientCategory.Vegetable)
                .Concat(GetIngredientsOfCategory(ingredients, IngredientCategory.Fruit))
                .ToList();
            var grainIngredients = GetIngredientsOfCategory(ingredients, IngredientCategory.Grain);

            // Build the description
            StringBuilder description = new StringBuilder();

            // Start with a template
            string typeDescriptor = GetTypeDescriptor(mealQuality, isVegetarian, isCarnivore);
            string ingredientList = FormatIngredientList(ingredients);

            string baseTemplate = MealDescriptionTemplates.RandomElement();
            description.AppendFormat(baseTemplate, typeDescriptor, ingredientList);

            // Add cooking method if appropriate
            if (Rand.Value < 0.5f)
            {
                description.Append(" ");
                description.AppendFormat("It was {0} with care.", CookingMethods.RandomElement());
            }

            // Add nutritional comment
            description.Append(" ");
            if (isVegetarian)
            {
                description.Append("A healthy plant-based option.");
            }
            else if (isCarnivore)
            {
                description.Append("High in protein and very filling.");
            }
            else
            {
                description.Append("A balanced meal with good nutritional value.");
            }

            return description.ToString();
        }

        /// <summary>
        /// Get a descriptor for the meal based on quality and type
        /// </summary>
        private static string GetTypeDescriptor(
            ProceduralDishNameGenerator.MealQuality quality,
            bool isVegetarian,
            bool isCarnivore)
        {
            // Get quality descriptor
            string qualityDescriptor;
            switch (quality)
            {
                case ProceduralDishNameGenerator.MealQuality.Lavish:
                    qualityDescriptor = LavishDescriptors.RandomElement();
                    break;
                case ProceduralDishNameGenerator.MealQuality.Fine:
                    qualityDescriptor = FineDescriptors.RandomElement();
                    break;
                default:
                    qualityDescriptor = SimpleDescriptors.RandomElement();
                    break;
            }

            // Get diet-type descriptor
            string dietDescriptor;
            if (isVegetarian)
            {
                dietDescriptor = VegetarianDescriptors.RandomElement();
            }
            else if (isCarnivore)
            {
                dietDescriptor = CarnivoreDescriptors.RandomElement();
            }
            else
            {
                dietDescriptor = MixedDescriptors.RandomElement();
            }

            // Combine the descriptors
            return $"{qualityDescriptor}, {dietDescriptor}";
        }

        /// <summary>
        /// Format a list of ingredients for display in the description
        /// </summary>
        private static string FormatIngredientList(List<ThingDef> ingredients)
        {
            if (ingredients.Count == 0)
                return "unknown ingredients";

            if (ingredients.Count == 1)
                return CleanIngredientLabel(ingredients[0].label);

            // Group ingredients by category
            var groupedIngredients = new Dictionary<IngredientCategory, List<string>>();

            foreach (var ingredient in ingredients)
            {
                var category = GetIngredientCategory(ingredient);

                if (!groupedIngredients.ContainsKey(category))
                    groupedIngredients[category] = new List<string>();

                groupedIngredients[category].Add(CleanIngredientLabel(ingredient.label));
            }

            // Build a readable list with categories
            var result = new List<string>();

            foreach (var group in groupedIngredients)
            {
                // Avoid duplicate names
                var uniqueNames = group.Value.Distinct().ToList();

                // If there's only one item, just add it directly
                if (uniqueNames.Count == 1)
                {
                    result.Add(uniqueNames[0]);
                }
                // If there are too many items, summarize
                else if (uniqueNames.Count > 3)
                {
                    string categoryName = GetCategoryDisplayName(group.Key);
                    result.Add($"various {categoryName}");
                }
                // Otherwise, list them with their category
                else
                {
                    string categoryName = GetCategoryDisplayName(group.Key);
                    string items = string.Join(", ", uniqueNames);
                    result.Add($"{items} ({categoryName})");
                }
            }

            // Format the final list
            if (result.Count == 1)
                return result[0];

            if (result.Count == 2)
                return $"{result[0]} and {result[1]}";

            return string.Join(", ", result.Take(result.Count - 1)) + ", and " + result.Last();
        }

        /// <summary>
        /// Get a display name for an ingredient category
        /// </summary>
        private static string GetCategoryDisplayName(IngredientCategory category)
        {
            switch (category)
            {
                case IngredientCategory.Meat:
                    return "meats";
                case IngredientCategory.Vegetable:
                    return "vegetables";
                case IngredientCategory.Grain:
                    return "grains";
                case IngredientCategory.Egg:
                    return "eggs";
                case IngredientCategory.Dairy:
                    return "dairy";
                case IngredientCategory.Fruit:
                    return "fruits";
                case IngredientCategory.Fungus:
                    return "fungi";
                case IngredientCategory.Special:
                    return "special ingredients";
                default:
                    return "ingredients";
            }
        }

        /// <summary>
        /// Determine the quality level of a meal
        /// </summary>
        private static ProceduralDishNameGenerator.MealQuality DetermineMealQuality(ThingDef mealDef)
        {
            if (mealDef == null)
                return ProceduralDishNameGenerator.MealQuality.Simple;

            string defName = mealDef.defName;

            if (defName.Contains("Lavish"))
                return ProceduralDishNameGenerator.MealQuality.Lavish;
            else if (defName.Contains("Fine"))
                return ProceduralDishNameGenerator.MealQuality.Fine;
            else
                return ProceduralDishNameGenerator.MealQuality.Simple;
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

            // Ensure no meat suffix for readability
            cleaned = cleaned.Replace(" meat", "");

            cleaned = cleaned.Trim();

            // Ensure first letter is lowercase for display in lists
            if (cleaned.Length > 0)
            {
                cleaned = char.ToLower(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }
    }
}