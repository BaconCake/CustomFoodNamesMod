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
            // Only process meal items
            if (!__instance.def.defName.StartsWith("Meal"))
                return;

            // Add debug logging
            if (Prefs.DevMode)
            {
                Log.Message($"[CustomFoodNames] Processing meal: {__instance.ThingID}, DefName: {__instance.def.defName}");
            }

            if (__instance is ThingWithComps twc)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[CustomFoodNames] Meal has {twc.AllComps.Count()} comps");
                }

                // Get the custom name component
                var customNameComp = twc.GetComp<CompCustomMealName>();

                if (customNameComp == null)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning("[CustomFoodNames] CustomNameComp is missing on meal - this is unexpected");
                    }
                    return;
                }

                // If there's no assigned name yet, generate one
                if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message("[CustomFoodNames] Generating new dish name");
                    }

                    // Get ingredients from the meal
                    var compIngredients = twc.TryGetComp<CompIngredients>();

                    if (compIngredients != null && compIngredients.ingredients.Count > 0)
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Message($"[CustomFoodNames] Found ingredients comp with {compIngredients.ingredients.Count} ingredients");
                        }

                        // Use the procedural generator for more complex meals
                        customNameComp.AssignedDishName = ProceduralDishNameGenerator.GenerateDishName(
                            compIngredients.ingredients,
                            __instance.def);
                    }
                    else
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Warning("[CustomFoodNames] No ingredients found for meal");
                        }
                        customNameComp.AssignedDishName = "Mystery Meal";
                    }

                    if (Prefs.DevMode)
                    {
                        Log.Message($"[CustomFoodNames] Generated dish name: {customNameComp.AssignedDishName}");
                    }
                }

                // Always append the stored name
                __result += $" ({customNameComp.AssignedDishName})";

                if (Prefs.DevMode)
                {
                    Log.Message($"[CustomFoodNames] Final label: {__result}");
                }
            }
        }
    }
}