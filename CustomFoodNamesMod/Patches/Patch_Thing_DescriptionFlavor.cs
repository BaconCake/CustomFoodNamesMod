using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using CustomFoodNamesMod.Utils;
using CustomFoodNamesMod.Generators;
using CustomFoodNamesMod.Core;

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

                if (customNameComp != null)
                {
                    Log.Message($"[CustomFoodNames] Generating description for meal: {customNameComp.AssignedDishName}, Cook: {(customNameComp.CookName ?? "not set")}");

                    // Check if cookName is null or empty but the component exists
                    if (string.IsNullOrEmpty(customNameComp.CookName))
                    {
                        Log.Message("[CustomFoodNames] CookName property is null or empty, but component exists");
                    }
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

                // Add cook information if available
                if (customNameComp != null && !string.IsNullOrEmpty(customNameComp.CookName))
                {
                    customDescription += $"\n\nPrepared by chef {customNameComp.CookName}.";
                    Log.Message($"[CustomFoodNames] Added cook information to description: {customNameComp.CookName}");
                }
                else
                {
                    // Be more specific about why cook information is missing
                    if (customNameComp == null)
                    {
                        Log.Message("[CustomFoodNames] No custom name component found");
                    }
                    else
                    {
                        Log.Message("[CustomFoodNames] No cook information available for this meal");
                    }
                }

                // Append our custom description to the original
                __result += customDescription;

                // Add developer mode info if developer mode is enabled
                if (Prefs.DevMode)
                {
                    __result += "\n\n<color=#A9A9A9><i>--- Developer Info ---</i></color>\n";

                    // List all ingredients with their categories
                    __result += "<color=#A9A9A9>Ingredients:\n";
                    foreach (var ingredient in compIngredients.ingredients)
                    {
                        var category = IngredientCategorizer.GetIngredientCategory(ingredient);
                        __result += $"- {ingredient.label} ({ingredient.defName}): {category}\n";
                    }

                    // Add meal properties
                    bool isVegetarian = IngredientUtils.IsMealVegetarian(compIngredients.ingredients);
                    bool isCarnivore = IngredientUtils.IsMealCarnivore(compIngredients.ingredients);
                    __result += $"\nMeal Properties:\n";
                    __result += $"- Quality: {mealQuality}\n";
                    __result += $"- Vegetarian: {isVegetarian}\n";
                    __result += $"- Carnivore: {isCarnivore}\n";

                    // Get primary category of the meal
                    IngredientCategory primaryCategory = IngredientCategorizer.GetPrimaryMealCategory(compIngredients.ingredients);
                    __result += $"- Primary Category: {primaryCategory}\n";

                    // List all unique categories present
                    var categories = compIngredients.ingredients
                        .Select(i => IngredientCategorizer.GetIngredientCategory(i))
                        .Distinct()
                        .ToList();
                    __result += $"- All Categories: {string.Join(", ", categories)}\n";

                    // Get dominant ingredient
                    ThingDef dominantIngredient = IngredientCategorizer.GetDominantIngredient(compIngredients.ingredients);
                    if (dominantIngredient != null)
                    {
                        __result += $"- Dominant Ingredient: {dominantIngredient.label} ({dominantIngredient.defName})\n";
                    }

                    // Add naming source information
                    __result += "\nNaming Info:\n";
                    __result += $"- Assigned Name: {customNameComp.AssignedDishName}\n";
                    __result += $"- Name Source: ";
                    __result += (dishInfo != null && !string.IsNullOrEmpty(dishInfo.Name))
                        ? "Database (custom)"
                        : "Procedural (generated)";

                    // Add batch job info if possible
                    if (__instance.def.defName.StartsWith("Meal") && !__instance.def.defName.Contains("NutrientPaste"))
                    {
                        var mealTypeInfo = __instance.def.defName.Contains("Lavish") ? "Lavish"
                            : __instance.def.defName.Contains("Fine") ? "Fine"
                            : "Simple";
                        __result += $"\n\nMeal Type: {mealTypeInfo}";

                        if (isVegetarian)
                            __result += " (Vegetarian)";
                        else if (isCarnivore)
                            __result += " (Carnivore)";
                    }

                    // Close the color tag
                    __result += "</color>";
                }
            }
        }
    }
}