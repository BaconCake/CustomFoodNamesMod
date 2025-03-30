using System.Collections.Generic;
using System.Linq;
using CustomFoodNamesMod.Core;
using RimWorld;
using Verse;

namespace CustomFoodNamesMod.Utils
{
    /// <summary>
    /// Utility methods for ingredient handling
    /// </summary>
    public static class IngredientUtils
    {
        /// <summary>
        /// Formats ingredient lists for display in meal descriptions
        /// </summary>
        public static string FormatIngredientsList(List<ThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "unknown ingredients";

            // Group ingredients by name to avoid repetition
            var groupedIngredients = ingredients
                .GroupBy(i => i.defName)
                .Select(g => new {
                    Label = StringUtils.CleanIngredientLabel(g.First().label),
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
        /// Check if a meal is vegetarian (contains no meat)
        /// </summary>
        public static bool IsMealVegetarian(List<ThingDef> ingredients)
        {
            return !ingredients.Any(i => IngredientCategorizer.GetIngredientCategory(i) == IngredientCategory.Meat);
        }

        /// <summary>
        /// Check if a meal is carnivore (contains only meat and no plants)
        /// </summary>
        public static bool IsMealCarnivore(List<ThingDef> ingredients)
        {
            var categories = ingredients.Select(i => IngredientCategorizer.GetIngredientCategory(i)).Distinct();
            return categories.Contains(IngredientCategory.Meat) &&
                  !categories.Contains(IngredientCategory.Vegetable) &&
                  !categories.Contains(IngredientCategory.Fruit) &&
                  !categories.Contains(IngredientCategory.Grain);
        }

        /// <summary>
        /// Determines if a ThingDef is a valid cooking ingredient in RimWorld
        /// </summary>
        public static bool IsValidCookingIngredient(ThingDef def)
        {
            // Skip if not ingestible or is a corpse
            if (!def.IsIngestible || def.IsCorpse)
                return false;

            // Skip drugs and alcohol
            if (def.ingestible?.drugCategory != DrugCategory.None)
                return false;

            // Skip prepared meals (they shouldn't be ingredients)
            if (def.defName.StartsWith("Meal") || def.defName == "Kibble" || def.defName == "Pemmican")
                return false;

            // Skip plants (we want the harvested product, not the plant itself)
            if (def.defName.StartsWith("Plant_"))
                return false;

            // Skip serums and medicine
            if (def.defName.EndsWith("Serum") || def.IsMedicine)
                return false;

            // Skip HemogenPack and BabyFood
            if (def.defName == "HemogenPack" || def.defName == "BabyFood")
                return false;

            // Skip things with drug food type
            if (def.ingestible?.foodType == FoodTypeFlags.Liquor)
                return false;

            // Focus on things that are actually used in cooking
            bool isRawFood = def.defName.StartsWith("Raw") ||      // RawPotatoes, RawRice, etc.
                             def.defName.StartsWith("Meat_") ||    // Meat_Cow, Meat_Chicken, etc.
                             def.defName.StartsWith("Egg") ||      // EggChickenUnfertilized, etc.
                             def.defName == "Milk" ||
                             def.defName == "InsectJelly" ||
                             def.defName == "Hay" ||
                             def.defName == "Chocolate";

            return isRawFood;
        }
    }
}