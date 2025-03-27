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

                    // If there's no assigned name yet, generate one
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

                    // Append the stored name for regular meals
                    __result += $" ({customNameComp.AssignedDishName})";
                }
            }
        }
    }
}