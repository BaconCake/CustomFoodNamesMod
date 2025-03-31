using System;
using HarmonyLib;
using Verse;

namespace CustomFoodNamesMod.Patches
{
    [HarmonyPatch(typeof(ThingWithComps))]
    [HarmonyPatch("ExposeData")]
    public static class Patch_ThingWithComps_ExposeData
    {
        [HarmonyPostfix]
        public static void Postfix(ThingWithComps __instance)
        {
            try
            {
                // Only process meal items
                if (!__instance.def.IsIngestible ||
                    (!__instance.def.defName.StartsWith("Meal") && !__instance.def.defName.Contains("NutrientPaste")))
                    return;

                // Skip survival meals
                if (__instance.def.defName.Contains("Survival") || __instance.def.defName.Contains("PackagedSurvival"))
                    return;

                // Get the custom name component
                var customNameComp = __instance.GetComp<CompCustomMealName>();
                if (customNameComp != null)
                {
                    if (Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        Log.Message($"[CustomFoodNames] ExposeData (Loading) - DishName: '{customNameComp.AssignedDishName}', Cook: '{customNameComp.CookName}'");
                    }
                    else if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        Log.Message($"[CustomFoodNames] ExposeData (Saving) - DishName: '{customNameComp.AssignedDishName}', Cook: '{customNameComp.CookName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in Patch_ThingWithComps_ExposeData: {ex}");
            }
        }
    }
}