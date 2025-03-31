using System.Collections.Generic;
using Verse;

namespace CustomFoodNamesMod.Batch
{
    /// <summary>
    /// Data class to hold information about a batch cooking job
    /// </summary>
    public class BatchJobInfo
    {
        /// <summary>
        /// The unique job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// The meal ThingDef being produced
        /// </summary>
        public ThingDef MealDef { get; set; }

        /// <summary>
        /// List of ingredients used in this batch
        /// </summary>
        public List<ThingDef> Ingredients { get; set; }

        /// <summary>
        /// The generated dish name for this batch
        /// </summary>
        public string DishName { get; set; }

        /// <summary>
        /// Whether this batch has already produced at least one meal
        /// </summary>
        public bool HasProducedMeal { get; set; }

        public Pawn Cook { get; set; }
    }
}