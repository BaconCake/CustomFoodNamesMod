using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace CustomFoodNamesMod.Patches
{
    [HarmonyPatch(typeof(Thing), "LabelNoCount", MethodType.Getter)]
    public static class Patch_Thing_LabelNoCount
    {
        public static void Postfix(ref string __result, Thing __instance)
        {
            // Check if it's a food item
            if (!(__instance.def.IsIngestible))
                return;

            // Skip survival meals
            if (__instance.def.defName.Contains("Survival") || __instance.def.defName.Contains("PackagedSurvival"))
                return;

            // Handle nutrient paste meals
            if (__instance.def.defName == "MealNutrientPaste" || __instance.def.defName.Contains("NutrientPaste"))
            {
                if (__instance is ThingWithComps twc)
                {
                    // Get the custom name component
                    var customNameComp = twc.GetComp<CompCustomMealName>();

                    if (customNameComp == null)
                    {
                        return;
                    }

                    // If there's no assigned name yet, check if it's part of a batch
                    if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                    {
                        // Check if this meal is part of a batch job
                        Pawn worker = GetCookingPawn(__instance);
                        if (worker != null && worker.CurJob != null)
                        {
                            int jobId = worker.CurJob.loadID;
                            string batchName = BatchMealNameHandler.GetBatchMealName(jobId);

                            if (!string.IsNullOrEmpty(batchName))
                            {
                                // Use the batch name
                                customNameComp.AssignedDishName = batchName;
                            }
                        }

                        // If still no name, generate one
                        if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                        {
                            // Get ingredients from the meal
                            var compIngredients = twc.TryGetComp<CompIngredients>();

                            if (compIngredients != null && compIngredients.ingredients.Count > 0)
                            {
                                // Use the simplified nutrient paste generator
                                customNameComp.AssignedDishName = NutrientPasteNameGenerator.GenerateNutrientPasteName(
                                    compIngredients.ingredients);
                            }
                            else
                            {
                                customNameComp.AssignedDishName = "Mystery Nutrient Paste";
                            }
                        }
                    }

                    // Replace the entire name with our custom name for nutrient paste
                    __result = __result + $" ({customNameComp.AssignedDishName})";
                    return;
                }
            }

            // Only process other meal items (not nutrient paste)
            else if (__instance.def.defName.StartsWith("Meal"))
            {
                if (__instance is ThingWithComps twc)
                {
                    // Get the custom name component
                    var customNameComp = twc.GetComp<CompCustomMealName>();

                    if (customNameComp == null)
                    {
                        return;
                    }

                    // If there's no assigned name yet, check if it's part of a batch first
                    if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                    {
                        // Check if this meal is part of a batch job
                        Pawn worker = GetCookingPawn(__instance);
                        if (worker != null && worker.CurJob != null)
                        {
                            int jobId = worker.CurJob.loadID;
                            string batchName = BatchMealNameHandler.GetBatchMealName(jobId);

                            if (!string.IsNullOrEmpty(batchName))
                            {
                                // Use the batch name
                                customNameComp.AssignedDishName = batchName;
                            }
                        }

                        // If still no name, generate one
                        if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                        {
                            // Get ingredients from the meal
                            var compIngredients = twc.TryGetComp<CompIngredients>();

                            if (compIngredients != null && compIngredients.ingredients.Count > 0)
                            {
                                // Use the procedural generator for more complex meals
                                customNameComp.AssignedDishName = ProceduralDishNameGenerator.GenerateDishName(
                                    compIngredients.ingredients,
                                    __instance.def);
                            }
                            else
                            {
                                customNameComp.AssignedDishName = "Mystery Meal";
                            }
                        }
                    }

                    // Append the stored name for regular meals
                    __result += $" ({customNameComp.AssignedDishName})";
                }
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