using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;
using CustomFoodNamesMod.Batch;
using CustomFoodNamesMod.Generators;

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
                            string batchName = BatchMealHandler.GetBatchMealName(jobId);

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
                                // Check for twisted meat
                                bool hasTwistedMeat = compIngredients.ingredients.Any(i =>
                                    i.defName.Contains("TwistedMeat") ||
                                    i.defName.Contains("Meat_Twisted") ||
                                    (i.label != null && i.label.ToLower().Contains("twisted meat")));

                                // Use the nutrient paste generator
                                var generator = new NutrientPasteNameGenerator();
                                customNameComp.AssignedDishName = generator.GenerateName(
                                    compIngredients.ingredients,
                                    __instance.def);

                                // Fix any twisted meat references
                                if (hasTwistedMeat &&
                                    customNameComp.AssignedDishName.Contains("Twisted") &&
                                    !customNameComp.AssignedDishName.Contains("Twisted Meat"))
                                {
                                    customNameComp.AssignedDishName = Regex.Replace(
                                        customNameComp.AssignedDishName,
                                        @"\bTwisted\b",
                                        "Twisted Meat");
                                }
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
                            string batchName = BatchMealHandler.GetBatchMealName(jobId);

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
                                // Check for twisted meat
                                bool hasTwistedMeat = compIngredients.ingredients.Any(i =>
                                    i.defName.Contains("TwistedMeat") ||
                                    i.defName.Contains("Meat_Twisted") ||
                                    (i.label != null && i.label.ToLower().Contains("twisted meat")));

                                // Use the procedural generator
                                var generator = new ProceduralDishNameGenerator();
                                customNameComp.AssignedDishName = generator.GenerateName(
                                    compIngredients.ingredients,
                                    __instance.def);

                                // Fix any twisted meat references 
                                if (hasTwistedMeat &&
                                    customNameComp.AssignedDishName.Contains("Twisted") &&
                                    !customNameComp.AssignedDishName.Contains("Twisted Meat"))
                                {
                                    customNameComp.AssignedDishName = Regex.Replace(
                                        customNameComp.AssignedDishName,
                                        @"\bTwisted\b",
                                        "Twisted Meat");
                                }
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