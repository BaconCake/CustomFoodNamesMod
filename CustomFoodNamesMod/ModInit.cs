using HarmonyLib;
using Verse;
using CustomFoodNamesMod.Patches;

namespace CustomFoodNamesMod
{
    public class MyFoodMod : Mod
    {
        public MyFoodMod(ModContentPack content) : base(content)
        {
            Log.Message("[CustomFoodNames] Initializing mod...");

            var harmony = new Harmony("com.myfoodmod.patch");

            // Apply only the manual patches
            Patch_CompIngredients_MergeIngredients.Apply(harmony);

            // Don't use PatchAll which is causing problems
            // harmony.PatchAll();

            // Apply each patch manually instead
            Patch_Thing_LabelNoCount.Apply(harmony);
            Patch_Thing_DescriptionFlavor.Apply(harmony);
            Patch_JobDriver_DoBill_MakeNewToils.Apply(harmony);
            Patch_GenRecipe_MakeRecipeProducts.Apply(harmony);
            Patch_Pawn_JobTracker_EndCurrentJob.Apply(harmony);

            // Force load the dish name database to verify it's working
            DishNameDatabase.LoadDatabase();

            Log.Message("[CustomFoodNames] Initialization complete");
        }
    }
}