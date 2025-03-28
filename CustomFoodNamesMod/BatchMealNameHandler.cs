using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Handles consistent naming of meals created in batches
    /// </summary>
    public static class BatchMealNameHandler
    {
        // Dictionary to store batch meal names by job ID
        private static Dictionary<int, string> batchMealNames = new Dictionary<int, string>();

        // Dictionary to store ingredients by job ID
        private static Dictionary<int, List<ThingDef>> batchIngredients = new Dictionary<int, List<ThingDef>>();

        /// <summary>
        /// Register the start of a batch cooking job, generating a consistent name for all meals in the batch
        /// </summary>
        public static void RegisterBatchJob(int jobId, List<ThingDef> ingredients, ThingDef mealDef)
        {
            if (ingredients == null || ingredients.Count == 0 || mealDef == null)
                return;

            try
            {
                // Generate a dish name for this batch of meals
                string dishName;

                // First check if it's a nutrient paste meal
                if (mealDef.defName == "MealNutrientPaste" || mealDef.defName.Contains("NutrientPaste"))
                {
                    dishName = NutrientPasteNameGenerator.GenerateNutrientPasteName(ingredients);
                }
                else
                {
                    // Use procedural generation for regular meals
                    dishName = ProceduralDishNameGenerator.GenerateDishName(ingredients, mealDef);
                }

                // Store the name for this job
                batchMealNames[jobId] = dishName;

                // Store the ingredients for this job (in case we need them later)
                batchIngredients[jobId] = new List<ThingDef>(ingredients);

                Log.Message($"[CustomFoodNames] Registered batch name '{dishName}' for job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error registering batch job: {ex}");
            }
        }

        /// <summary>
        /// Get the dish name for a meal that's part of a batch job
        /// </summary>
        public static string GetBatchMealName(int jobId)
        {
            if (batchMealNames.TryGetValue(jobId, out string dishName))
            {
                return dishName;
            }

            return null; // No batch name found, will fall back to individual generation
        }

        /// <summary>
        /// Get the ingredients for a meal that's part of a batch job
        /// </summary>
        public static List<ThingDef> GetBatchIngredients(int jobId)
        {
            if (batchIngredients.TryGetValue(jobId, out List<ThingDef> ingredients))
            {
                return ingredients;
            }

            return null; // No batch ingredients found
        }

        /// <summary>
        /// Clean up the stored names for a job after it's complete
        /// </summary>
        public static void CleanupBatchJob(int jobId)
        {
            batchMealNames.Remove(jobId);
            batchIngredients.Remove(jobId);
        }

        /// <summary>
        /// Assign the appropriate batch name to a newly created meal
        /// </summary>
        public static void AssignBatchNameToMeal(Thing meal, int jobId)
        {
            if (meal == null || !(meal is ThingWithComps))
                return;

            try
            {
                // Get the custom name component
                var twc = meal as ThingWithComps;
                var customNameComp = twc.GetComp<CompCustomMealName>();

                if (customNameComp == null)
                    return;

                // Get the stored dish name for this batch
                string batchName = GetBatchMealName(jobId);

                if (!string.IsNullOrEmpty(batchName))
                {
                    // Assign the batch name to this meal
                    customNameComp.AssignedDishName = batchName;

                    // Also ensure this meal has the right ingredients
                    var compIngredients = twc.TryGetComp<CompIngredients>();
                    if (compIngredients != null)
                    {
                        var batchIngredients = GetBatchIngredients(jobId);
                        if (batchIngredients != null && batchIngredients.Count > 0)
                        {
                            // Ensure the ingredients are properly set
                            // This might not be necessary if RimWorld already handles this correctly
                            compIngredients.ingredients.Clear();
                            foreach (var ingredient in batchIngredients)
                            {
                                compIngredients.ingredients.Add(ingredient);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error assigning batch name to meal: {ex}");
            }
        }
    }
}