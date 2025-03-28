using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

namespace CustomFoodNamesMod.Patches
{
    /// <summary>
    /// Handles consistent naming of meals created in batches - improved version
    /// </summary>
    public static class ImprovedBatchMealHandler
    {
        // Dictionary to store batch meal names by job ID
        private static Dictionary<int, string> batchMealNames = new Dictionary<int, string>();

        // Dictionary to store ingredients by job ID
        private static Dictionary<int, List<ThingDef>> batchIngredients = new Dictionary<int, List<ThingDef>>();

        // Track job IDs that have already produced a meal
        private static HashSet<int> activeJobs = new HashSet<int>();

        /// <summary>
        /// Register the start of a batch cooking job, generating a consistent name for all meals in the batch
        /// </summary>
        public static void RegisterBatchCookingJob(int jobId, Bill bill)
        {
            if (bill?.recipe == null || bill.recipe.ProducedThingDef == null)
                return;

            try
            {
                // Skip if this job is already registered
                if (batchMealNames.ContainsKey(jobId))
                    return;

                // Mark this as an active job
                activeJobs.Add(jobId);

                // This will be filled with actual ingredients when the first meal is made
                batchIngredients[jobId] = new List<ThingDef>();

                Log.Message($"[CustomFoodNames] Registered batch cooking job {jobId} for {bill.recipe.ProducedThingDef.defName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error registering batch cooking job: {ex}");
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

                // If this is the first meal from this job, generate a name
                if (!batchMealNames.ContainsKey(jobId))
                {
                    // Use the actual ingredients that were used
                    List<ThingDef> actualIngredients = new List<ThingDef>();
                    foreach (var ingredient in usedIngredients)
                    {
                        if (ingredient?.def != null)
                            actualIngredients.Add(ingredient.def);
                    }

                    // Fall back to the CompIngredients if needed
                    if (actualIngredients.Count == 0 && ingredientsComp.ingredients.Count > 0)
                    {
                        actualIngredients.AddRange(ingredientsComp.ingredients);
                    }

                    if (actualIngredients.Count == 0)
                    {
                        // No ingredients found, use fallback
                        actualIngredients = DefDatabase<ThingDef>.AllDefs
                            .Where(def => def.defName.StartsWith("Raw") || def.defName.StartsWith("Meat_"))
                            .Take(2)
                            .ToList();
                    }

                    // Store the ingredients for this batch
                    batchIngredients[jobId] = actualIngredients;

                    // Generate a name for this batch
                    string dishName;
                    if (meal.def.defName.Contains("NutrientPaste"))
                    {
                        dishName = NutrientPasteNameGenerator.GenerateNutrientPasteName(actualIngredients);
                    }
                    else
                    {
                        dishName = ProceduralDishNameGenerator.GenerateDishName(actualIngredients, meal.def);
                    }

                    // Store the name for this job
                    batchMealNames[jobId] = dishName;

                    Log.Message($"[CustomFoodNames] Generated batch name '{dishName}' for job {jobId} with {actualIngredients.Count} ingredients");
                }

                // Assign the batch name to this meal
                string batchName = batchMealNames[jobId];
                customNameComp.AssignedDishName = batchName;

                Log.Message($"[CustomFoodNames] Assigned batch name '{batchName}' to meal from job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error processing new meal: {ex}");
            }
        }

        /// <summary>
        /// Clean up job data when cooking is complete
        /// </summary>
        public static void CleanupJob(int jobId)
        {
            try
            {
                // Remove from all tracking collections
                batchMealNames.Remove(jobId);
                batchIngredients.Remove(jobId);
                activeJobs.Remove(jobId);

                Log.Message($"[CustomFoodNames] Cleaned up batch job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error cleaning up job: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch cooking job setup to register batch jobs
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_DoBill))]
    [HarmonyPatch("MakeNewToils")]
    public static class Patch_JobDriver_DoBill_MakeNewToils
    {
        public static void Postfix(JobDriver_DoBill __instance)
        {
            try
            {
                // Basic null checks
                if (__instance?.job == null || __instance.job.bill == null)
                    return;

                var job = __instance.job;
                var bill = job.bill;

                // Only care about cooking jobs
                if (!IsCookingJob(bill))
                    return;

                // Register this as a batch cooking job
                ImprovedBatchMealHandler.RegisterBatchCookingJob(job.loadID, bill);
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error patching JobDriver_DoBill.MakeNewToils: {ex}");
            }
        }

        private static bool IsCookingJob(Bill bill)
        {
            if (bill?.recipe?.ProducedThingDef == null)
                return false;

            var producedDef = bill.recipe.ProducedThingDef;

            // Check if it's producing a meal
            return (producedDef.defName.StartsWith("Meal") ||
                    producedDef.defName.Contains("NutrientPaste")) &&
                   !producedDef.defName.Contains("Survival") &&
                   !producedDef.defName.Contains("PackagedSurvival");
        }
    }

    /// <summary>
    /// Patch recipe product creation to assign batch names using actual ingredients used
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe))]
    [HarmonyPatch("MakeRecipeProducts")]
    public static class Patch_GenRecipe_MakeRecipeProducts
    {
        public static void Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients)
        {
            try
            {
                // Basic null checks
                if (__result == null || worker == null || worker.CurJob == null || ingredients == null)
                    return;

                // Skip if not a meal recipe
                if (recipeDef?.ProducedThingDef == null ||
                    (!recipeDef.ProducedThingDef.defName.StartsWith("Meal") &&
                     !recipeDef.ProducedThingDef.defName.Contains("NutrientPaste")))
                    return;

                // Skip survival meals
                if (recipeDef.ProducedThingDef.defName.Contains("Survival") ||
                    recipeDef.ProducedThingDef.defName.Contains("PackagedSurvival"))
                    return;

                // Get the current job ID
                int jobId = worker.CurJob.loadID;

                // Get all produced meals
                var resultList = __result.ToList();
                if (resultList.Count == 0)
                    return;

                // Process each produced meal
                foreach (var meal in resultList)
                {
                    ImprovedBatchMealHandler.ProcessNewMeal(meal, jobId, ingredients);
                }

                // Only clean up if this is the last product in the batch
                if (IsLastItemInBatch(worker.CurJob))
                {
                    ImprovedBatchMealHandler.CleanupJob(jobId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error patching GenRecipe.MakeRecipeProducts: {ex}");
            }
        }

        private static bool IsLastItemInBatch(Job job)
        {
            // Basic detection - in a real implementation we would need better logic
            // For now, just return false to never clean up during production
            // We'll rely on pawn job completion to clean up instead
            return false;
        }
    }

    /// <summary>
    /// Patch job end to clean up batch data when cooking is done
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker))]
    [HarmonyPatch("EndCurrentJob")]
    public static class Patch_Pawn_JobTracker_EndCurrentJob
    {
        public static void Prefix(Pawn_JobTracker __instance)
        {
            try
            {
                // Get the pawn and current job
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                Job curJob = __instance.curJob;

                if (pawn == null || curJob == null)
                    return;

                // If this is a cooking job, clean up the batch
                if (curJob.bill != null && IsCookingBill(curJob.bill))
                {
                    ImprovedBatchMealHandler.CleanupJob(curJob.loadID);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error patching Pawn_JobTracker.EndCurrentJob: {ex}");
            }
        }

        private static bool IsCookingBill(Bill bill)
        {
            if (bill?.recipe?.ProducedThingDef == null)
                return false;

            var producedDef = bill.recipe.ProducedThingDef;

            // Check if it's producing a meal
            return (producedDef.defName.StartsWith("Meal") ||
                    producedDef.defName.Contains("NutrientPaste")) &&
                   !producedDef.defName.Contains("Survival") &&
                   !producedDef.defName.Contains("PackagedSurvival");
        }
    }
}