using HarmonyLib;
using Verse;
using CustomFoodNamesMod.Patches;

namespace CustomFoodNamesMod
{
    public class MyFoodMod : Mod
    {
        public MyFoodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.myfoodmod.patch");

            // Apply the manual patch for MergeIngredients
            Patch_CompIngredients_MergeIngredients.Apply(harmony);

            // Patch everything else automatically
            harmony.PatchAll();

            Log.Message("[CustomFoodNames] Initialization complete");
        }
    }
}