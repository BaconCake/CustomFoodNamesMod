using System.Collections.Generic;
using Verse;

namespace CustomFoodNamesMod.Generators
{
    /// <summary>
    /// Base class for all dish name generators
    /// </summary>
    public abstract class NameGeneratorBase
    {
        /// <summary>
        /// Generate a dish name based on ingredients and meal definition
        /// </summary>
        public abstract string GenerateName(List<ThingDef> ingredients, ThingDef mealDef);

        /// <summary>
        /// Generate a description for the meal
        /// </summary>
        public abstract string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef);

        /// <summary>
        /// Determine meal quality from meal definition
        /// </summary>
        protected MealQuality DetermineMealQuality(ThingDef mealDef)
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