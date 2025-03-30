using System.Collections.Generic;
using Verse;

namespace CustomFoodNamesMod.Generators
{
    /// <summary>
    /// Selects the appropriate generator for different meal types
    /// </summary>
    public static class GeneratorSelector
    {
        /// <summary>
        /// Get the appropriate generator for a meal
        /// </summary>
        public static NameGeneratorBase GetGenerator(ThingDef mealDef)
        {
            if (mealDef == null)
                return new ProceduralDishNameGenerator(); // Default

            // Check for nutrient paste
            if (mealDef.defName == "MealNutrientPaste" || mealDef.defName.Contains("NutrientPaste"))
                return new NutrientPasteNameGenerator();

            // Default to procedural for now - could be expanded with more conditions
            return new ProceduralDishNameGenerator();
        }

        /// <summary>
        /// Generate a dish name using the appropriate generator
        /// </summary>
        public static string GenerateName(List<ThingDef> ingredients, ThingDef mealDef)
        {
            var generator = GetGenerator(mealDef);
            return generator.GenerateName(ingredients, mealDef);
        }

        /// <summary>
        /// Generate a description using the appropriate generator
        /// </summary>
        public static string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef)
        {
            var generator = GetGenerator(mealDef);
            return generator.GenerateDescription(ingredients, mealDef);
        }
    }
}