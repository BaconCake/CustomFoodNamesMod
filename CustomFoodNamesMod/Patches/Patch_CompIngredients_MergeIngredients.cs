using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomFoodNamesMod.Patches
{
    public static class Patch_CompIngredients_MergeIngredients
    {
        // We'll manually patch in the ModInit class instead of using attributes
        public static void Apply(Harmony harmony)
        {
            // Get the original method
            MethodInfo original = AccessTools.Method(typeof(CompIngredients), "MergeIngredients",
                new[] {
                    typeof(List<ThingDef>),
                    typeof(List<ThingDef>),
                    typeof(bool).MakeByRefType(),
                    typeof(ThingDef)
                });

            if (original == null)
            {
                Log.Error("[CustomFoodNames] Failed to find CompIngredients.MergeIngredients method");
                return;
            }

            // Get our postfix method
            MethodInfo postfix = AccessTools.Method(typeof(Patch_CompIngredients_MergeIngredients),
                nameof(MergeIngredients_Postfix));

            // Apply the patch
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message("[CustomFoodNames] Successfully patched CompIngredients.MergeIngredients");
        }

        public static void MergeIngredients_Postfix(CompIngredients __instance)
        {
            try
            {
                // Null check for instance
                if (__instance == null)
                {
                    return;
                }

                // Null check for parent
                ThingWithComps parent = __instance.parent;
                if (parent == null)
                {
                    return;
                }

                // Skip if not an ingestible
                if (!parent.def?.IsIngestible ?? true)
                {
                    return;
                }

                // Skip survival meals
                if (parent.def.defName.Contains("Survival") || parent.def.defName.Contains("PackagedSurvival"))
                    return;

                // Check ingredients collection
                if (__instance.ingredients == null)
                {
                    return;
                }

                // Get the custom name component
                var customNameComp = parent.GetComp<CompCustomMealName>();
                if (customNameComp == null)
                {
                    return;
                }

                // Check if this meal already has a name (from a batch job)
                if (!string.IsNullOrEmpty(customNameComp.AssignedDishName))
                {
                    // Already has a name assigned, don't override it
                    return;
                }

                // Generate a new name based on the updated ingredients
                if (__instance.ingredients.Count > 0)
                {
                    // Check if this is a nutrient paste meal
                    if (parent.def.defName == "MealNutrientPaste" || parent.def.defName.Contains("NutrientPaste"))
                    {
                        // Use our simplified nutrient paste name generator
                        string newDishName = NutrientPasteNameGenerator.GenerateNutrientPasteName(
                            __instance.ingredients);

                        customNameComp.AssignedDishName = newDishName;
                    }
                    else
                    {
                        // Check if this meal is part of a batch job
                        Pawn worker = GetCookingPawn(parent);
                        if (worker != null && worker.CurJob != null)
                        {
                            int jobId = worker.CurJob.loadID;
                            string batchName = BatchMealNameHandler.GetBatchMealName(jobId);

                            if (!string.IsNullOrEmpty(batchName))
                            {
                                // Use the batch name
                                customNameComp.AssignedDishName = batchName;
                                return;
                            }
                        }

                        // No batch name available, use procedural generation
                        string newDishName = ProceduralDishNameGenerator.GenerateDishName(
                            __instance.ingredients,
                            parent.def);

                        customNameComp.AssignedDishName = newDishName;
                    }
                }
                else
                {
                    // Check if this is a nutrient paste meal
                    if (parent.def.defName == "MealNutrientPaste" || parent.def.defName.Contains("NutrientPaste"))
                    {
                        customNameComp.AssignedDishName = "Mystery Nutrient Paste";
                    }
                    else
                    {
                        customNameComp.AssignedDishName = "Mystery Meal";
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in MergeIngredients patch: {ex}");
            }
        }

        // Helper method to try to find the pawn that's cooking this meal
        private static Pawn GetCookingPawn(Thing meal)
        {
            try
            {
                // This is a simplistic approach - in a real implementation, we might need 
                // more sophisticated logic to find the cooking pawn
                var position = meal.Position;
                var map = meal.Map;

                if (map != null)
                {
                    // Look for a pawn doing a cooking job at this position
                    var pawns = map.mapPawns.AllPawns;
                    foreach (var pawn in pawns)
                    {
                        if (pawn.CurJob != null &&
                            pawn.CurJob.targetA.Thing != null &&
                            pawn.CurJob.targetA.Thing.def.defName.Contains("Stove"))
                        {
                            return pawn;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore errors in this helper method
            }

            return null;
        }
    }
}