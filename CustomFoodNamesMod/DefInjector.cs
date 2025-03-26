namespace CustomFoodNamesMod
{
    using System.Collections.Generic;
    using Verse;

    /// <summary>
    /// Defines the <see cref="DefInjector" />
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DefInjector
    {
        /// <summary>
        /// Initializes static members of the <see cref="DefInjector"/> class.
        /// </summary>
        static DefInjector()
        {
            Log.Warning("[CustomFoodNames] Starting DefInjector...");

            // List of all meal defs to modify
            string[] mealDefNames = new string[] { "MealSimple", "MealFine", "MealLavish" };

            foreach (string mealDefName in mealDefNames)
            {
                ThingDef mealDef = DefDatabase<ThingDef>.GetNamed(mealDefName, false);

                if (mealDef == null)
                {
                    Log.Error($"[CustomFoodNames] Could not find {mealDefName} def!");
                    continue;
                }

                Log.Warning($"[CustomFoodNames] Found {mealDefName} def. It has {(mealDef.comps?.Count ?? 0)} comps.");

                // Ensure comps list exists
                if (mealDef.comps == null)
                {
                    mealDef.comps = new List<CompProperties>();
                    Log.Warning($"[CustomFoodNames] Created new comps list for {mealDefName}");
                }

                // Check if our comp is already added
                bool hasOurComp = false;
                foreach (var comp in mealDef.comps)
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
                    mealDef.comps.Add(customNameProps);
                    Log.Warning($"[CustomFoodNames] Added CompProperties_CustomMealName to {mealDefName}");
                }
                else
                {
                    Log.Warning($"[CustomFoodNames] CompProperties_CustomMealName already exists on {mealDefName}");
                }
            }
        }
    }
}
