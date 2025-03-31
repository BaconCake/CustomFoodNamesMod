using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CustomFoodNamesMod.Generators;
using RimWorld;
using Verse;

namespace CustomFoodNamesMod.Batch
{
    /// <summary>
    /// Unified handler for batch meal naming
    /// </summary>
    public static class BatchMealHandler
    {
        // Dictionary to store batch meal data by job ID
        private static Dictionary<int, BatchJobInfo> batchJobs = new Dictionary<int, BatchJobInfo>();

        // Track job IDs that have already produced a meal
        private static HashSet<int> activeJobs = new HashSet<int>();

        /// <summary>
        /// Register a new batch cooking job
        /// </summary>
        public static void RegisterBatchJob(int jobId, Bill bill)
        {
            if (bill?.recipe?.ProducedThingDef == null)
                return;

            try
            {
                // Skip if already registered
                if (batchJobs.ContainsKey(jobId))
                    return;

                // Mark this as an active job
                activeJobs.Add(jobId);

                // Create new batch job info
                batchJobs[jobId] = new BatchJobInfo
                {
                    JobId = jobId,
                    MealDef = bill.recipe.ProducedThingDef,
                    Ingredients = new List<ThingDef>(),
                    HasProducedMeal = false
                };

                Log.Message($"[CustomFoodNames] Registered batch job {jobId} for {bill.recipe.ProducedThingDef.defName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error registering batch job: {ex}");
            }
        }

        /// <summary>
        /// Process a newly created meal and assign it a consistent batch name
        /// </summary>
        public static void ProcessNewMeal(Thing meal, int jobId, List<Thing> usedIngredients)
        {
            if (meal == null || !activeJobs.Contains(jobId))
                return;

            try
            {
                // Skip if not a meal
                if (!meal.def.IsIngestible ||
                    (!meal.def.defName.StartsWith("Meal") && !meal.def.defName.Contains("NutrientPaste")))
                    return;

                // Skip survival meals
                if (meal.def.defName.Contains("Survival") || meal.def.defName.Contains("PackagedSurvival"))
                    return;

                // Get the custom name component
                var twc = meal as ThingWithComps;
                if (twc == null) return;

                var customNameComp = twc.GetComp<CompCustomMealName>();
                if (customNameComp == null) return;

                // Get ingredients component
                var ingredientsComp = twc.GetComp<CompIngredients>();
                if (ingredientsComp == null) return;

                // Get or create batch job info
                BatchJobInfo batchInfo;
                if (!batchJobs.TryGetValue(jobId, out batchInfo))
                {
                    batchInfo = new BatchJobInfo
                    {
                        JobId = jobId,
                        MealDef = meal.def,
                        Ingredients = new List<ThingDef>(),
                        HasProducedMeal = false
                    };
                    batchJobs[jobId] = batchInfo;
                }

                // If this is the first meal from this job, generate a name
                if (!batchInfo.HasProducedMeal)
                {
                    // Use the actual ingredients that were used
                    List<ThingDef> actualIngredients = new List<ThingDef>();
                    foreach (var ingredient in usedIngredients)
                    {
                        if (ingredient?.def != null)
                        {
                            Log.Message($"[CustomFoodNames] Used ingredient: {ingredient.def.defName} ({ingredient.def.label})");
                            actualIngredients.Add(ingredient.def);
                        }
                    }

                    // Fall back to the CompIngredients if needed
                    if (actualIngredients.Count == 0 && ingredientsComp.ingredients.Count > 0)
                    {
                        Log.Message($"[CustomFoodNames] Falling back to CompIngredients");
                        actualIngredients.AddRange(ingredientsComp.ingredients);
                    }

                    if (actualIngredients.Count == 0)
                    {
                        // No ingredients found, use fallback
                        Log.Message($"[CustomFoodNames] No ingredients found, using fallback");
                        actualIngredients = DefDatabase<ThingDef>.AllDefs
                            .Where(def => def.defName.StartsWith("Raw") || def.defName.StartsWith("Meat_"))
                            .Take(2)
                            .ToList();
                    }

                    // Check for twisted meat in ingredients
                    bool hasTwistedMeat = actualIngredients.Any(i =>
                        i.defName.Contains("TwistedMeat") ||
                        i.defName.Contains("Meat_Twisted") ||
                        (i.label != null && i.label.ToLower().Contains("twisted meat")));

                    Log.Message($"[CustomFoodNames] Has twisted meat: {hasTwistedMeat}");

                    // Store the ingredients for this batch
                    batchInfo.Ingredients = actualIngredients;

                    // Generate a name for this batch
                    string dishName;
                    if (meal.def.defName.Contains("NutrientPaste"))
                    {
                        var generator = new NutrientPasteNameGenerator();
                        dishName = generator.GenerateName(actualIngredients, meal.def);
                    }
                    else
                    {
                        var generator = new ProceduralDishNameGenerator();
                        dishName = generator.GenerateName(actualIngredients, meal.def);
                    }

                    // Check if dish name contains "Twisted" without "Meat"
                    if (hasTwistedMeat && dishName.Contains("Twisted") && !dishName.Contains("Twisted Meat"))
                    {
                        // Fix the name by replacing "Twisted" with "Twisted Meat"
                        Log.Message($"[CustomFoodNames] Fixing twisted meat in dish name: {dishName}");
                        dishName = Regex.Replace(dishName, @"\bTwisted\b", "Twisted Meat");
                        Log.Message($"[CustomFoodNames] Fixed dish name: {dishName}");
                    }

                    // Store the name for this job
                    batchInfo.DishName = dishName;
                    batchInfo.HasProducedMeal = true;

                    Log.Message($"[CustomFoodNames] Generated batch name '{dishName}' for job {jobId} with {actualIngredients.Count} ingredients");
                }

                // Assign the batch name to this meal
                customNameComp.AssignedDishName = batchInfo.DishName;

                Log.Message($"[CustomFoodNames] Assigned batch name '{batchInfo.DishName}' to meal from job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error processing new meal: {ex}");
            }
        }

        /// <summary>
        /// Get the stored batch name for a job
        /// </summary>
        public static string GetBatchMealName(int jobId)
        {
            if (batchJobs.TryGetValue(jobId, out var batchInfo) && batchInfo.HasProducedMeal)
            {
                return batchInfo.DishName;
            }

            return null; // No batch name found
        }

        /// <summary>
        /// Get the ingredients for a batch job
        /// </summary>
        public static List<ThingDef> GetBatchIngredients(int jobId)
        {
            if (batchJobs.TryGetValue(jobId, out var batchInfo) && batchInfo.HasProducedMeal)
            {
                return batchInfo.Ingredients;
            }

            return null; // No batch ingredients found
        }

        /// <summary>
        /// Clean up job data when cooking is complete
        /// </summary>
        public static void CleanupJob(int jobId)
        {
            try
            {
                batchJobs.Remove(jobId);
                activeJobs.Remove(jobId);

                Log.Message($"[CustomFoodNames] Cleaned up batch job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error cleaning up job: {ex}");
            }
        }
    }
}