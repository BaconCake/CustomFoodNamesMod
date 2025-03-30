using System.Collections.Generic;
using System.Linq;
using Verse;
using CustomFoodNamesMod.Core;
using CustomFoodNamesMod.Utils;

namespace CustomFoodNamesMod.Generators
{
    /// <summary>
    /// Generates funny, slightly disgusting names for nutrient paste meals based on ingredients
    /// </summary>
    public class NutrientPasteNameGenerator : NameGeneratorBase
    {
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
                    IngredientCategory.Meat,
                    new List<string> { "Protein", "Meat", "Flesh" }
                },
                {
                    IngredientCategory.Vegetable,
                    new List<string> { "Vegetable", "Plant", "Greens" }
                },
                {
                    IngredientCategory.Grain,
                    new List<string> { "Starch", "Carb", "Grain" }
                },
                {
                    IngredientCategory.Dairy,
                    new List<string> { "Dairy", "Milk", "Cream" }
                },
                {
                    IngredientCategory.Egg,
                    new List<string> { "Egg", "Yolk", "Albumin" }
                },
                {
                    IngredientCategory.Fruit,
                    new List<string> { "Fruit", "Berry", "Sweet" }
                },
                {
                    IngredientCategory.Fungus,
                    new List<string> { "Fungal", "Mushroom", "Spore" }
                },
                {
                    IngredientCategory.Special,
                    new List<string> { "Mystery", "Exotic", "Strange" }
                },
                {
                    IngredientCategory.Other,
                    new List<string> { "Unidentified", "Unknown", "Mysterious" }
                }
            };

        /// <summary>
        /// Generate a dish name based on ingredients and meal definition
        /// </summary>
        public override string GenerateName(List<ThingDef> ingredients, ThingDef mealDef)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Nutrient Paste";

            // Get most frequent ingredient category
            IngredientCategory dominantCategory = IngredientCategorizer.GetPrimaryMealCategory(ingredients);

            // Get a representative ingredient from the dominant category
            ThingDef dominantIngredient = IngredientCategorizer.GetRepresentativeIngredient(ingredients, dominantCategory);

            // Get the processed ingredient name
            string ingredientName;

            if (dominantIngredient != null)
            {
                // Use the actual ingredient name
                ingredientName = StringUtils.GetCapitalizedLabel(dominantIngredient.label);
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
        /// Generate a description for the meal
        /// </summary>
        public override string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef)
        {
            string ingredientsList = IngredientUtils.FormatIngredientsList(ingredients);

            return $"This is a nutrient paste meal made from processed {ingredientsList}. " +
                   "The nutritional value is adequate, but the taste leaves much to be desired.";
        }
    }
}