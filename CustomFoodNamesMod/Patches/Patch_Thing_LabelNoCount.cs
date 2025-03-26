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

            if (__instance is ThingWithComps twc)
            {
                // Get the custom name component
                var customNameComp = twc.GetComp<CompCustomMealName>();

                if (customNameComp == null)
                {
                    return;
                }

                // If there's no assigned name yet, generate one
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

                // Always append the stored name
                __result += $" ({customNameComp.AssignedDishName})";
            }
        }
    }
}