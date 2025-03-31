using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;
using CustomFoodNamesMod.Batch;
using CustomFoodNamesMod.Generators;

namespace CustomFoodNamesMod.Patches
{
    public static class Patch_Thing_LabelNoCount
    {
        public static void Apply(Harmony harmony)
        {
            // Get the original method
            var original = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.LabelNoCount));
            if (original == null)
            {
                Log.Error("[CustomFoodNames] Failed to find Thing.LabelNoCount property getter");
                return;
            }

            // Get our postfix method
            var postfix = AccessTools.Method(typeof(Patch_Thing_LabelNoCount), nameof(Postfix));

            // Apply the patch
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message("[CustomFoodNames] Successfully patched Thing.LabelNoCount");
        }

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
                        Pawn worker = null;
                        int jobId = -1;

                        // Try to find a worker with an active cooking job
                        if (__instance.Map != null)
                        {
                            foreach (var pawn in __instance.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (pawn.CurJob?.bill?.recipe?.ProducedThingDef?.defName == __instance.def.defName)
                                {
                                    worker = pawn;
                                    jobId = pawn.CurJob.loadID;
                                    break;
                                }
                            }
                        }

                        if (jobId > 0)
                        {
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
                            var compIngredients = twc.GetComp<CompIngredients>();

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

                        // IMPROVED COOK TRACKING
                        if (string.IsNullOrEmpty(customNameComp.CookName))
                        {
                            // Try to find the most appropriate cook using our enhanced system
                            Pawn bestCook = BatchMealHandler.GetBestCookForMeal(__instance, worker?.CurJob?.loadID ?? -1);

                            if (bestCook != null)
                            {
                                customNameComp.CookName = bestCook.Name.ToStringShort;
                                Log.Message($"[CustomFoodNames] Set cook name in LabelNoCount using improved system: {customNameComp.CookName}");
                            }
                            else
                            {
                                customNameComp.CookName = "colony chef";
                                Log.Message("[CustomFoodNames] Set default cook name in LabelNoCount: 'colony chef'");
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
                        Pawn worker = null;
                        int jobId = -1;

                        // Try to find a worker with an active cooking job
                        if (__instance.Map != null)
                        {
                            foreach (var pawn in __instance.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (pawn.CurJob?.bill?.recipe?.ProducedThingDef?.defName == __instance.def.defName)
                                {
                                    worker = pawn;
                                    jobId = pawn.CurJob.loadID;
                                    break;
                                }
                            }
                        }

                        if (jobId > 0)
                        {
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
                            var compIngredients = twc.GetComp<CompIngredients>();

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

                        // IMPROVED COOK TRACKING
                        if (string.IsNullOrEmpty(customNameComp.CookName))
                        {
                            // Try to find the most appropriate cook using our enhanced system
                            Pawn bestCook = BatchMealHandler.GetBestCookForMeal(__instance, worker?.CurJob?.loadID ?? -1);

                            if (bestCook != null)
                            {
                                customNameComp.CookName = bestCook.Name.ToStringShort;
                                Log.Message($"[CustomFoodNames] Set cook name in LabelNoCount using improved system: {customNameComp.CookName}");
                            }
                            else
                            {
                                customNameComp.CookName = "colony chef";
                                Log.Message("[CustomFoodNames] Set default cook name in LabelNoCount: 'colony chef'");
                            }
                        }
                    }

                    // Append the stored name for regular meals
                    __result += $" ({customNameComp.AssignedDishName})";
                }
            }
        }

        // Helper method to try to find the pawn that's cooking this meal - Kept for compatibility
        private static Pawn GetCookingPawn(Thing meal)
        {
            try
            {
                return BatchMealHandler.GetBestCookForMeal(meal);
            }
            catch (Exception)
            {
                // Ignore errors in this helper method
                return null;
            }
        }
    }
}