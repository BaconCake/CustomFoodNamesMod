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
                    return;
                }

                // Null check for parent
                ThingWithComps parent = __instance.parent;
                if (parent == null)
                {
                    return;
                }

                // Check if this comp belongs to a meal
                if (!parent.def?.defName.StartsWith("Meal") ?? true)
                {
                    return;
                }

                // Check ingredients collection
                if (__instance.ingredients == null)
                {
                    return;
                }

                // Get the custom name component
                var customNameComp = parent.GetComp<CompCustomMealName>();
                if (customNameComp == null)
                {
                    return;
                }

                // Generate a new name based on the updated ingredients
                if (__instance.ingredients.Count > 0)
                {
                    // Use procedural generation for names
                    string newDishName = ProceduralDishNameGenerator.GenerateDishName(
                        __instance.ingredients,
                        parent.def);

                    customNameComp.AssignedDishName = newDishName;
                }
                else
                {
                    customNameComp.AssignedDishName = "Mystery Meal";
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in MergeIngredients patch: {ex}");
            }
        }
    }
}