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

            // Create a list to store all found meal defs
            var allMealDefs = new List<ThingDef>();

            // Find all ThingDefs that are meals
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.defName.StartsWith("Meal"))
                {
                    allMealDefs.Add(def);
                    Log.Warning($"[CustomFoodNames] Found meal def: {def.defName}");
                }
            }

            Log.Warning($"[CustomFoodNames] Total meal defs found: {allMealDefs.Count}");

            // Process each meal def
            foreach (var mealDef in allMealDefs)
            {
                Log.Warning($"[CustomFoodNames] Processing {mealDef.defName}. It has {(mealDef.comps?.Count ?? 0)} comps.");

                // Ensure comps list exists
                if (mealDef.comps == null)
                {
                    mealDef.comps = new List<CompProperties>();
                    Log.Warning($"[CustomFoodNames] Created new comps list for {mealDef.defName}");
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
                    Log.Warning($"[CustomFoodNames] Added CompProperties_CustomMealName to {mealDef.defName}");
                }
                else
                {
                    Log.Warning($"[CustomFoodNames] CompProperties_CustomMealName already exists on {mealDef.defName}");
                }
            }
        }
    }
}
