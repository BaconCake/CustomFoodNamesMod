using System;
using Verse;

namespace CustomFoodNamesMod
{
    public class CompProperties_CustomMealName : CompProperties
    {
        public CompProperties_CustomMealName()
        {
            // Tells the game which comp class to use
            compClass = typeof(CompCustomMealName);

            // Add debug logging when this comp is created
            Log.Warning($"[CustomFoodNames] CompProperties_CustomMealName constructor called. compClass = {compClass.Name}");
        }
    }
}