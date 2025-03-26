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
        }
    }
}