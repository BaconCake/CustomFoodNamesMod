using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using CustomFoodNamesMod.Utils;
using CustomFoodNamesMod.Generators;
using CustomFoodNamesMod.Core;
using System;

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

                // EMERGENCY PATCH: Try to recover cook name from saved game data if needed
                if (customNameComp != null && string.IsNullOrEmpty(customNameComp.CookName))
                {
                    try
                    {
                        // Try to find a pawn that might have cooked this meal
                        if (__instance.Spawned && __instance.Map != null)
                        {
                            // Look for a nearby cook with the cooking skill
                            var potentialCooks = __instance.Map.mapPawns.AllPawnsSpawned
                                .Where(p => p.skills?.GetSkill(SkillDefOf.Cooking)?.Level > 0)
                                .OrderBy(p => p.Position.DistanceTo(__instance.Position))
                                .Take(1)
                                .ToList();

                            if (potentialCooks.Count > 0)
                            {
                                var nearestCook = potentialCooks[0];
                                customNameComp.CookName = nearestCook.Name.ToStringShort;
                                Log.Message($"[CustomFoodNames] EMERGENCY: Recovered cook name from nearby cook: {customNameComp.CookName}");
                            }
                            else
                            {
                                // Default if no cook found
                                customNameComp.CookName = "colony chef";
                                Log.Message("[CustomFoodNames] EMERGENCY: Set default cook name: 'colony chef'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[CustomFoodNames] Error in emergency cook name recovery: {ex}");
                        customNameComp.CookName = "unknown chef";
                    }
                }

                // Initialize customDescription variable 
                string customDescription = "";

                // Get ingredients from the meal
                var compIngredients = twc.GetComp<CompIngredients>();
                if (compIngredients == null || compIngredients.ingredients.Count == 0)
                {
                    // No ingredients found, add minimal description
                    __result += $"\n\nThis is a {customNameComp.AssignedDishName} with unknown ingredients.";
                    return;
                }

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
                else if (__instance.def.defName.Contains("NutrientPaste"))
                {
                    var generator = new NutrientPasteNameGenerator();
                    customDescription = $"\n\n{generator.GenerateDescription(compIngredients.ingredients, __instance.def)}";
                }
                else
                {
                    customDescription = $"\n\n{GeneratorSelector.GenerateDescription(compIngredients.ingredients, __instance.def)}";
                }

                // Debug the current cook name value
                if (customNameComp != null)
                {
                    Log.Message($"[CustomFoodNames] Cook name in description: '{customNameComp.CookName}'");
                }

                // Add cook information if available
                if (customNameComp != null && !string.IsNullOrEmpty(customNameComp.CookName))
                {
                    customDescription += $"\n\nPrepared by chef {customNameComp.CookName}.";
                    Log.Message($"[CustomFoodNames] Added cook information to description: {customNameComp.CookName}");
                }
                else
                {
                    customDescription += "\n\nPrepared by an unknown chef.";

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

                    // Add cook information in dev mode
                    __result += $"\n- Cook: {(string.IsNullOrEmpty(customNameComp.CookName) ? "None" : customNameComp.CookName)}";

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