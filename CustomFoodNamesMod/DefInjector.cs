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
            Log.Message("[CustomFoodNames] Starting DefInjector...");

            // Create a list to store all found meal defs
            var allMealDefs = new List<ThingDef>();

            // Find all ThingDefs that are meals or nutrient paste
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.defName.StartsWith("Meal") || def.defName.Contains("NutrientPaste"))
                {
                    allMealDefs.Add(def);
                }
            }

            Log.Message($"[CustomFoodNames] Found {allMealDefs.Count} meal defs");

            // Process each meal def
            foreach (var mealDef in allMealDefs)
            {
                // Ensure comps list exists
                if (mealDef.comps == null)
                {
                    mealDef.comps = new List<CompProperties>();
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
                }
            }

            Log.Message("[CustomFoodNames] DefInjector complete");
        }
    }
}