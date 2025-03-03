using System.Collections.Generic;
using Verse;

namespace CustomFoodNamesMod
{
    [StaticConstructorOnStartup]
    public static class DefInjector
    {
        static DefInjector()
        {
            Log.Warning("[CustomFoodNames] Starting DefInjector...");

            ThingDef mealSimple = DefDatabase<ThingDef>.GetNamed("MealSimple", false);

            if (mealSimple == null)
            {
                Log.Error("[CustomFoodNames] Could not find MealSimple def!");
                return;
            }

            Log.Warning($"[CustomFoodNames] Found MealSimple def. It has {(mealSimple.comps?.Count ?? 0)} comps.");

            // Ensure comps list exists
            if (mealSimple.comps == null)
            {
                mealSimple.comps = new List<CompProperties>();
                Log.Warning("[CustomFoodNames] Created new comps list for MealSimple");
            }

            // Check if our comp is already added
            bool hasOurComp = false;
            foreach (var comp in mealSimple.comps)
            {
                if (comp is CompProperties_CustomMealName)
                {
                    hasOurComp = true;
                    break;
                }
            }

            if (!hasOurComp)
            {
                CompProperties_CustomMealName customNameProps = new CompProperties_CustomMealName();
                mealSimple.comps.Add(customNameProps);
                Log.Warning("[CustomFoodNames] Added CompProperties_CustomMealName to MealSimple");
            }
            else
            {
                Log.Warning("[CustomFoodNames] CompProperties_CustomMealName already exists on MealSimple");
            }

            // Print all comp types after modification
            Log.Warning("[CustomFoodNames] MealSimple comps after modification:");
            foreach (var comp in mealSimple.comps)
            {
                Log.Warning($"[CustomFoodNames] - {comp.GetType().Name}");
            }
        }
    }
}