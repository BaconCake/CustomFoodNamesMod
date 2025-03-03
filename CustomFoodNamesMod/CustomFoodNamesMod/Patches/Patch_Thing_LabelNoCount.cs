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
            // Add debug logging
            if (__instance.def.defName == "MealSimple")
            {
                Log.Message($"Found MealSimple: {__instance.ThingID}");

                if (__instance is ThingWithComps twc)
                {
                    Log.Message($"MealSimple is a ThingWithComps with {twc.AllComps.Count()} comps");

                    // Log all comp types for debugging
                    foreach (var comp in twc.AllComps)
                    {
                        Log.Message($"Comp type: {comp.GetType().Name}");
                    }

                    var customNameComp = twc.GetComp<CompCustomMealName>();
                    Log.Message($"CustomNameComp found: {customNameComp != null}");

                    if (customNameComp == null)
                    {
                        Log.Warning("CustomNameComp is missing on MealSimple - patch may not be working");
                        return;
                    }

                    // If there's no assigned name yet, generate one
                    if (string.IsNullOrEmpty(customNameComp.AssignedDishName))
                    {
                        Log.Message("Generating new dish name");
                        var compIngredients = twc.TryGetComp<CompIngredients>();

                        if (compIngredients != null)
                        {
                            Log.Message($"Found ingredients comp with {compIngredients.ingredients.Count} ingredients");

                            if (compIngredients.ingredients.Count == 1)
                            {
                                var ingredientDef = compIngredients.ingredients[0];
                                Log.Message($"Single ingredient: {ingredientDef.defName}");

                                string randomDishName = DishNameDatabase.GetRandomDishName(ingredientDef.defName);
                                Log.Message($"Random dish name: {randomDishName ?? "null"}");

                                if (!string.IsNullOrEmpty(randomDishName))
                                {
                                    customNameComp.AssignedDishName = randomDishName;
                                }
                                else
                                {
                                    customNameComp.AssignedDishName = ingredientDef.label + " dish";
                                }
                            }
                            else
                            {
                                customNameComp.AssignedDishName = "Multi-ingredient meal";
                            }
                        }
                        else
                        {
                            Log.Warning("No ingredients comp found on MealSimple");
                            customNameComp.AssignedDishName = "Mystery meal";
                        }

                        Log.Message($"Final dish name: {customNameComp.AssignedDishName}");
                    }
                    else
                    {
                        Log.Message($"Using existing dish name: {customNameComp.AssignedDishName}");
                    }

                    // Always append the stored name
                    __result += $" ({customNameComp.AssignedDishName})";
                    Log.Message($"Updated label: {__result}");
                }
                else
                {
                    Log.Error("MealSimple is not a ThingWithComps! This is unexpected.");
                }
            }
        }
    }
}