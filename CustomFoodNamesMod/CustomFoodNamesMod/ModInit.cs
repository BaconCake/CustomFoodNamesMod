using HarmonyLib;
using Verse;

namespace CustomFoodNamesMod
{
    public class MyFoodMod : Mod
    {
        public MyFoodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.myfoodmod.patch");
            harmony.PatchAll();
        }
    }
}