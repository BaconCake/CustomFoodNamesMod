using HarmonyLib;
using Verse;
using System;

namespace CustomFoodNamesMod.Patches
{
    // Make sure we're using the right attribute format
    [HarmonyPatch(typeof(ThingWithComps))]
    [HarmonyPatch("PostPostMake")]
    public static class Patch_ThingWithComps_PostPostMake
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

                // Initialize meal comp if needed
                var customNameComp = __instance.GetComp<CompCustomMealName>();
                if (customNameComp != null)
                {
                    Log.Message($"[CustomFoodNames] PostPostMake check - CompCustomMealName exists, DishName: '{customNameComp.AssignedDishName}', Cook: '{customNameComp.CookName}'");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in Patch_ThingWithComps_PostPostMake: {ex}");
            }
        }
    }
}