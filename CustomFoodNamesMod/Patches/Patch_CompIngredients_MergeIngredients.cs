using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomFoodNamesMod.Patches
{
    public static class Patch_CompIngredients_MergeIngredients
    {
        // We'll manually patch in the ModInit class instead of using attributes
        public static void Apply(Harmony harmony)
        {
            // Get the original method
            MethodInfo original = AccessTools.Method(typeof(CompIngredients), "MergeIngredients",
                new[] {
                    typeof(List<ThingDef>),
                    typeof(List<ThingDef>),
                    typeof(bool).MakeByRefType(),
                    typeof(ThingDef)
                });

            if (original == null)
            {
                Log.Error("[CustomFoodNames] Failed to find CompIngredients.MergeIngredients method");
                return;
            }

            // Get our postfix method
            MethodInfo postfix = AccessTools.Method(typeof(Patch_CompIngredients_MergeIngredients),
                nameof(MergeIngredients_Postfix));

            // Apply the patch
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message("[CustomFoodNames] Successfully patched CompIngredients.MergeIngredients");
        }

        public static void MergeIngredients_Postfix(CompIngredients __instance)
        {
            try
            {
                // Null check for instance
                if (__instance == null)
                {
                    Log.Warning("[CustomFoodNames] MergeIngredients postfix called with null __instance");
                    return;
                }

                // Null check for parent
                ThingWithComps parent = __instance.parent;
                if (parent == null)
                {
                    Log.Warning("[CustomFoodNames] __instance.parent is null in MergeIngredients patch");
                    return;
                }

                // Check if this comp belongs to a meal
                if (!parent.def?.defName.StartsWith("Meal") ?? true)
                {
                    return;
                }

                if (Prefs.DevMode)
                {
                    Log.Message($"[CustomFoodNames] Ingredients merged for {parent.ThingID}");
                }

                // Check ingredients collection
                if (__instance.ingredients == null)
                {
                    Log.Warning("[CustomFoodNames] __instance.ingredients is null in MergeIngredients patch");
                    return;
                }

                // Get the custom name component
                var customNameComp = parent.GetComp<CompCustomMealName>();
                if (customNameComp == null)
                {
                    Log.Warning("[CustomFoodNames] CustomNameComp is missing after ingredient merge");
                    return;
                }

                // Generate a new name based on the updated ingredients
                if (__instance.ingredients.Count > 0)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[CustomFoodNames] Ingredients after merge: {__instance.ingredients.Count}");
                        foreach (var ingredient in __instance.ingredients)
                        {
                            if (ingredient == null)
                            {
                                Log.Warning("[CustomFoodNames] Null ingredient found in list");
                                continue;
                            }
                            Log.Message($"[CustomFoodNames] - Ingredient: {ingredient.defName}");
                        }
                    }

                    // Use procedural generation for names
                    string newDishName = ProceduralDishNameGenerator.GenerateDishName(
                        __instance.ingredients,
                        parent.def);

                    customNameComp.AssignedDishName = newDishName;

                    if (Prefs.DevMode)
                    {
                        Log.Message($"[CustomFoodNames] Updated dish name after ingredient merge: {newDishName}");
                    }
                }
                else
                {
                    customNameComp.AssignedDishName = "Mystery Meal";
                    if (Prefs.DevMode)
                    {
                        Log.Warning("[CustomFoodNames] No ingredients found after merge");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in MergeIngredients patch: {ex}");
            }
        }
    }
}