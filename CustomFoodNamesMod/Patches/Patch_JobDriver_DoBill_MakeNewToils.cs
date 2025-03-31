using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using CustomFoodNamesMod.Batch;

namespace CustomFoodNamesMod.Patches
{
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

                // Get the pawn doing the job
                Pawn worker = __instance.pawn;

                // Only care about cooking jobs
                if (!IsCookingJob(bill))
                    return;

                // Register this as a batch cooking job
                BatchMealHandler.RegisterBatchJob(job.loadID, bill, worker);

                // Log the worker information
                if (worker != null)
                {
                    Log.Message($"[CustomFoodNames] Job registered with worker: {worker.Name.ToStringShort}");
                }
                else
                {
                    Log.Message("[CustomFoodNames] No worker found for cooking job");
                }
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
                {
                    Log.Message("[CustomFoodNames] Null check failed in Patch_GenRecipe_MakeRecipeProducts");
                    return;
                }

                // Log the worker info
                Log.Message($"[CustomFoodNames] Worker processing meal: {worker.Name.ToStringShort}, JobID: {worker.CurJob.loadID}");


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
                    BatchMealHandler.ProcessNewMeal(meal, jobId, ingredients);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error patching GenRecipe.MakeRecipeProducts: {ex}");
            }
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
                    BatchMealHandler.CleanupJob(curJob.loadID);
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