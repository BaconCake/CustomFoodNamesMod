using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using CustomFoodNamesMod.Utils;
using CustomFoodNamesMod.Generators;

namespace CustomFoodNamesMod.Patches
{
    [HarmonyPatch(typeof(Thing), "DescriptionFlavor", MethodType.Getter)]
    public static class Patch_Thing_DescriptionFlavor
    {
        public static void Postfix(ref string __result, Thing __instance)
        {
            // Check if this is a meal item
            if (__instance is ThingWithComps twc &&
                __instance.def.IsIngestible &&
                (__instance.def.defName.StartsWith("Meal") || __instance.def.defName.Contains("NutrientPaste")))
            {
                // Skip survival meals
                if (__instance.def.defName.Contains("Survival") || __instance.def.defName.Contains("PackagedSurvival"))
                    return;

                // Get the custom name component
                var customNameComp = twc.GetComp<CompCustomMealName>();
                if (customNameComp == null || string.IsNullOrEmpty(customNameComp.AssignedDishName))
                {
                    return;
                }

                // Get ingredients from the meal
                var compIngredients = twc.GetComp<CompIngredients>();
                if (compIngredients == null || compIngredients.ingredients.Count == 0)
                {
                    // No ingredients found, add minimal description
                    __result += $"\n\nThis is a {customNameComp.AssignedDishName} with unknown ingredients.";
                    return;
                }

                // First try to get a custom description from the database
                string customDescription = null;

                // Determine meal quality
                string mealQuality = "Simple";
                if (__instance.def.defName.Contains("Fine"))
                    mealQuality = "Fine";
                else if (__instance.def.defName.Contains("Lavish"))
                    mealQuality = "Lavish";

                // Try to get dish info from database
                var dishInfo = DishNameDatabase.GetDishInfoForIngredients(
                    compIngredients.ingredients,
                    mealQuality);

                if (dishInfo != null && !string.IsNullOrEmpty(dishInfo.Description))
                {
                    // We found a custom description in the database
                    customDescription = $"\n\n{dishInfo.Description}";
                }

                // If no custom description was found, generate one
                if (string.IsNullOrEmpty(customDescription))
                {
                    if (__instance.def.defName.Contains("NutrientPaste"))
                    {
                        var generator = new NutrientPasteNameGenerator();
                        customDescription = $"\n\n{generator.GenerateDescription(compIngredients.ingredients, __instance.def)}";
                    }
                    else
                    {
                        customDescription = $"\n\n{GeneratorSelector.GenerateDescription(compIngredients.ingredients, __instance.def)}";
                    }
                }

                // Append our custom description to the original
                __result += customDescription;

                // Add developer mode info if developer mode is enabled
                if (Prefs.DevMode)
                {
                    __result += "\n\n<color=#A9A9A9><i>--- Developer Info ---</i></color>\n";

                    // Add ingredient category information
                    __result += "<color=#A9A9A9>Ingredient categories: ";
                    var categories = compIngredients.ingredients
                        .Select(i => Core.IngredientCategorizer.GetIngredientCategory(i).ToString())
                        .Distinct()
                        .ToList();
                    __result += string.Join(", ", categories);

                    // Add description source information
                    __result += "\nDescription source: ";
                    __result += (dishInfo != null && !string.IsNullOrEmpty(dishInfo.Description))
                        ? "Database (custom)"
                        : "Procedural (generated)";

                    __result += "</color>";
                }
            }
        }
    }
}