using HarmonyLib;
using RimWorld;
using System.Text;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace CustomFoodNamesMod.Patches
{
    /// <summary>
    /// Patches the DescriptionDetailed method to add custom dish descriptions to meals
    /// </summary>
    [HarmonyPatch(typeof(ThingDef), "DescriptionDetailed", MethodType.Getter)]
    public static class Patch_ThingDef_DescriptionDetailed
    {
        public static void Postfix(ref string __result, ThingDef __instance)
        {
            // Check if this is a meal type
            if (__instance != null && __instance.IsIngestible &&
                (__instance.defName.StartsWith("Meal") || __instance.defName.Contains("NutrientPaste")))
            {
                // We'll add a note that descriptions will be shown on actual meals
                __result += "\n\nIngredient details will be shown on individual meals.";
            }
        }
    }

    /// <summary>
    /// Patches the description display for Thing to add custom dish descriptions
    /// </summary>
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
                // Get the custom name component
                var customNameComp = twc.GetComp<CompCustomMealName>();
                if (customNameComp == null || string.IsNullOrEmpty(customNameComp.AssignedDishName))
                {
                    return;
                }

                // Get ingredients from the meal
                var compIngredients = twc.TryGetComp<CompIngredients>();
                if (compIngredients == null || compIngredients.ingredients.Count == 0)
                {
                    // No ingredients found, add minimal description
                    __result += $"\n\nThis is a {customNameComp.AssignedDishName} with unknown ingredients.";
                    return;
                }

                // Build the ingredients list
                string ingredientsList = FormatIngredientsList(compIngredients.ingredients);

                // Generate the custom description
                string customDescription;
                if (__instance.def.defName.Contains("NutrientPaste"))
                {
                    // Custom description for nutrient paste
                    customDescription = $"\n\nThis is a {customNameComp.AssignedDishName} made from processed {ingredientsList}. " +
                                       "The nutritional value is adequate, but the taste leaves much to be desired.";
                }
                else if (__instance.def.defName.Contains("Lavish"))
                {
                    // Custom description for lavish meals
                    customDescription = $"\n\nThis is a {customNameComp.AssignedDishName}, a lavishly prepared dish containing {ingredientsList}. " +
                                       "It has been expertly crafted to be both nutritious and delicious.";
                }
                else if (__instance.def.defName.Contains("Fine"))
                {
                    // Custom description for fine meals
                    customDescription = $"\n\nThis is a {customNameComp.AssignedDishName}, a well-prepared dish containing {ingredientsList}. " +
                                       "It has been skillfully made to balance nutrition and taste.";
                }
                else
                {
                    // Custom description for simple meals
                    customDescription = $"\n\nThis is a {customNameComp.AssignedDishName}, a basic dish containing {ingredientsList}. " +
                                       "It offers good nutrition although the taste is simple.";
                }

                // Append our custom description to the original
                __result += customDescription;
            }
        }

        /// <summary>
        /// Format the ingredients list for display in the description
        /// </summary>
        private static string FormatIngredientsList(List<ThingDef> ingredients)
        {
            if (ingredients.Count == 0)
                return "unknown ingredients";

            // Group ingredients by name to avoid repetition
            var groupedIngredients = ingredients
                .GroupBy(i => i.defName)
                .Select(g => new {
                    Label = CleanIngredientLabel(g.First().label),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Build a readable list
            var result = new List<string>();
            foreach (var ingredient in groupedIngredients)
            {
                // Only mention count if there's more than one
                if (ingredient.Count > 1)
                    result.Add($"{ingredient.Label} (x{ingredient.Count})");
                else
                    result.Add(ingredient.Label);
            }

            // Format the final list
            if (result.Count == 1)
                return result[0];

            if (result.Count == 2)
                return $"{result[0]} and {result[1]}";

            return string.Join(", ", result.Take(result.Count - 1)) + ", and " + result.Last();
        }

        /// <summary>
        /// Clean up ingredient names for better display
        /// </summary>
        private static string CleanIngredientLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "unknown ingredient";

            // Remove "raw" prefix
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                label,
                @"^raw\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Some specific replacements
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Trim();

            // Ensure first letter is lowercase for listing in text
            if (cleaned.Length > 0)
            {
                cleaned = char.ToLower(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }
    }
}