namespace CustomFoodNamesMod.Batch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using CustomFoodNamesMod.Generators;
    using RimWorld;
    using Verse;

    /// <summary>
    /// Unified handler for batch meal naming
    /// </summary>
    public static class BatchMealHandler
    {
        // Dictionary to store batch meal data by job ID
        private static Dictionary<int, BatchJobInfo> batchJobs = new Dictionary<int, BatchJobInfo>();

        // Track job IDs that have already produced a meal
        private static HashSet<int> activeJobs = new HashSet<int>();

        // Global tracking of the last cook for use in emergency scenarios
        private static Dictionary<int, Pawn> mapToLastActiveCook = new Dictionary<int, Pawn>();

        /// <summary>
        /// Register a new batch cooking job
        /// </summary>
        /// <param name="jobId">The jobId<see cref="int"/></param>
        /// <param name="bill">The bill<see cref="Bill"/></param>
        /// <param name="worker">The worker<see cref="Pawn"/></param>
        public static void RegisterBatchJob(int jobId, Bill bill, Pawn worker = null)
        {
            if (bill?.recipe?.ProducedThingDef == null)
                return;

            try
            {
                // Skip if already registered
                if (batchJobs.ContainsKey(jobId))
                    return;

                // Mark this as an active job
                activeJobs.Add(jobId);

                // Store worker name for debugging
                string workerName = worker != null ? worker.Name.ToStringShort : "null";

                // Create new batch job info
                batchJobs[jobId] = new BatchJobInfo
                {
                    JobId = jobId,
                    MealDef = bill.recipe.ProducedThingDef,
                    Ingredients = new List<ThingDef>(),
                    HasProducedMeal = false,
                    Cook = worker
                };

                // Update global cook tracking if we have a valid cook and map
                if (worker != null && worker.Map != null && worker.skills?.GetSkill(SkillDefOf.Cooking)?.Level > 0)
                {
                    mapToLastActiveCook[worker.Map.uniqueID] = worker;
                    Log.Message($"[CustomFoodNames] Updated last active cook for map {worker.Map.uniqueID}: {worker.Name.ToStringShort}");
                }

                Log.Message($"[CustomFoodNames] Registered batch job {jobId} for {bill.recipe.ProducedThingDef.defName}, " +
                            $"Cook: {workerName}, Worker null? {worker == null}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error registering batch job: {ex}");
            }
        }

        /// <summary>
        /// Process a newly created meal and assign it a consistent batch name
        /// </summary>
        /// <param name="meal">The meal<see cref="Thing"/></param>
        /// <param name="jobId">The jobId<see cref="int"/></param>
        /// <param name="usedIngredients">The usedIngredients<see cref="List{Thing}"/></param>
        public static void ProcessNewMeal(Thing meal, int jobId, List<Thing> usedIngredients)
        {
            if (meal == null || !activeJobs.Contains(jobId))
                return;

            try
            {
                // Skip if not a meal
                if (!meal.def.IsIngestible ||
                    (!meal.def.defName.StartsWith("Meal") && !meal.def.defName.Contains("NutrientPaste")))
                    return;

                // Skip survival meals
                if (meal.def.defName.Contains("Survival") || meal.def.defName.Contains("PackagedSurvival"))
                    return;

                // Get the custom name component
                var twc = meal as ThingWithComps;
                if (twc == null) return;

                var customNameComp = twc.GetComp<CompCustomMealName>();
                if (customNameComp == null) return;

                // Get ingredients component
                var ingredientsComp = twc.GetComp<CompIngredients>();
                if (ingredientsComp == null) return;

                // Get or create batch job info
                BatchJobInfo batchInfo;
                if (!batchJobs.TryGetValue(jobId, out batchInfo))
                {
                    Log.Message($"[CustomFoodNames] No batch job info found for job {jobId}");
                    batchInfo = new BatchJobInfo
                    {
                        JobId = jobId,
                        MealDef = meal.def,
                        Ingredients = new List<ThingDef>(),
                        HasProducedMeal = false,
                        Cook = null
                    };
                    batchJobs[jobId] = batchInfo;
                }

                Log.Message($"[CustomFoodNames] Processing meal for job {jobId}, Cook: {(batchInfo.Cook != null ? batchInfo.Cook.Name.ToStringShort : "unknown")}");

                // Set cook name - do this FIRST before any other operations
                if (batchInfo.Cook != null)
                {
                    // Store cook name in meal component
                    try
                    {
                        var cookName = batchInfo.Cook.Name.ToStringShort;
                        customNameComp.CookName = cookName;
                        Log.Message($"[CustomFoodNames] Setting cook name directly from batch: '{cookName}'");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[CustomFoodNames] Error setting cook name: {ex}");
                    }
                }
                else if (meal.Map != null && mapToLastActiveCook.TryGetValue(meal.Map.uniqueID, out Pawn lastCook))
                {
                    // Fallback to the last active cook on this map
                    customNameComp.CookName = lastCook.Name.ToStringShort;
                    Log.Message($"[CustomFoodNames] Using last active cook for map {meal.Map.uniqueID}: {lastCook.Name.ToStringShort}");
                }
                else
                {
                    // Default cook name for traceability
                    customNameComp.CookName = "unknown chef";
                    Log.Message("[CustomFoodNames] Setting default cook name: 'unknown chef'");
                }

                // If this is the first meal from this job, generate a name
                if (!batchInfo.HasProducedMeal)
                {
                    // Use the actual ingredients that were used
                    List<ThingDef> actualIngredients = new List<ThingDef>();
                    foreach (var ingredient in usedIngredients)
                    {
                        if (ingredient?.def != null)
                        {
                            Log.Message($"[CustomFoodNames] Used ingredient: {ingredient.def.defName} ({ingredient.def.label})");
                            actualIngredients.Add(ingredient.def);
                        }
                    }

                    // Fall back to the CompIngredients if needed
                    if (actualIngredients.Count == 0 && ingredientsComp.ingredients.Count > 0)
                    {
                        Log.Message($"[CustomFoodNames] Falling back to CompIngredients");
                        actualIngredients.AddRange(ingredientsComp.ingredients);
                    }

                    if (actualIngredients.Count == 0)
                    {
                        // No ingredients found, use fallback
                        Log.Message($"[CustomFoodNames] No ingredients found, using fallback");
                        actualIngredients = DefDatabase<ThingDef>.AllDefs
                            .Where(def => def.defName.StartsWith("Raw") || def.defName.StartsWith("Meat_"))
                            .Take(2)
                            .ToList();
                    }

                    // Check for twisted meat in ingredients
                    bool hasTwistedMeat = actualIngredients.Any(i =>
                        i.defName.Contains("TwistedMeat") ||
                        i.defName.Contains("Meat_Twisted") ||
                        (i.label != null && i.label.ToLower().Contains("twisted meat")));

                    Log.Message($"[CustomFoodNames] Has twisted meat: {hasTwistedMeat}");

                    // Store the ingredients for this batch
                    batchInfo.Ingredients = actualIngredients;

                    // Generate a name for this batch
                    string dishName;
                    if (meal.def.defName.Contains("NutrientPaste"))
                    {
                        var generator = new NutrientPasteNameGenerator();
                        dishName = generator.GenerateName(actualIngredients, meal.def);
                    }
                    else
                    {
                        var generator = new ProceduralDishNameGenerator();
                        dishName = generator.GenerateName(actualIngredients, meal.def);
                    }

                    // Check if dish name contains "Twisted" without "Meat"
                    if (hasTwistedMeat && dishName.Contains("Twisted") && !dishName.Contains("Twisted Meat"))
                    {
                        // Fix the name by replacing "Twisted" with "Twisted Meat"
                        Log.Message($"[CustomFoodNames] Fixing twisted meat in dish name: {dishName}");
                        dishName = Regex.Replace(dishName, @"\bTwisted\b", "Twisted Meat");
                        Log.Message($"[CustomFoodNames] Fixed dish name: {dishName}");
                    }

                    // Store the name for this job
                    batchInfo.DishName = dishName;
                    batchInfo.HasProducedMeal = true;

                    Log.Message($"[CustomFoodNames] Generated batch name '{dishName}' for job {jobId} with {actualIngredients.Count} ingredients");
                }

                // Assign the batch name to this meal
                customNameComp.AssignedDishName = batchInfo.DishName;

                // Final verification of data
                Log.Message($"[CustomFoodNames] Final verification - DishName: '{customNameComp.AssignedDishName}', Cook: '{customNameComp.CookName}'");

                // Force save if possible
                if (meal.Spawned && meal.Map != null)
                {
                    // This is a bit of a hack but can sometimes help with persistence issues
                    meal.Map.mapPawns.UpdateRegistryForPawn(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error processing new meal: {ex}");
            }
        }

        /// <summary>
        /// Get the stored batch name for a job
        /// </summary>
        /// <param name="jobId">The jobId<see cref="int"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetBatchMealName(int jobId)
        {
            if (batchJobs.TryGetValue(jobId, out var batchInfo) && batchInfo.HasProducedMeal)
            {
                return batchInfo.DishName;
            }

            return null; // No batch name found
        }

        /// <summary>
        /// Get the ingredients for a batch job
        /// </summary>
        /// <param name="jobId">The jobId<see cref="int"/></param>
        /// <returns>The <see cref="List{ThingDef}"/></returns>
        public static List<ThingDef> GetBatchIngredients(int jobId)
        {
            if (batchJobs.TryGetValue(jobId, out var batchInfo) && batchInfo.HasProducedMeal)
            {
                return batchInfo.Ingredients;
            }

            return null; // No batch ingredients found
        }

        /// <summary>
        /// Get the cook for a batch job
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <returns>The cook pawn, or null if not found</returns>
        public static Pawn GetBatchCook(int jobId)
        {
            if (batchJobs.TryGetValue(jobId, out var batchInfo))
            {
                return batchInfo.Cook;
            }

            return null;
        }

        /// <summary>
        /// Get the most reliable cook for a meal, trying different methods
        /// </summary>
        /// <param name="meal">The meal item</param>
        /// <param name="jobId">Optional job ID if known</param>
        /// <returns>The best cook match, or null if not found</returns>
        public static Pawn GetBestCookForMeal(Thing meal, int jobId = -1)
        {
            // Method 1: Try batch job info if job ID is provided
            if (jobId > 0)
            {
                var batchCook = GetBatchCook(jobId);
                if (batchCook != null)
                {
                    Log.Message($"[CustomFoodNames] Found cook from batch job: {batchCook.Name.ToStringShort}");
                    return batchCook;
                }
            }

            // Method 2: Check if the meal is on a map with a last known cook
            if (meal.Map != null && mapToLastActiveCook.TryGetValue(meal.Map.uniqueID, out Pawn lastCook))
            {
                Log.Message($"[CustomFoodNames] Using last known cook for map: {lastCook.Name.ToStringShort}");
                return lastCook;
            }

            // Method 3: Look for pawns currently using cooking stations
            if (meal.Map != null)
            {
                foreach (var pawn in meal.Map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.CurJob?.targetA.Thing?.def.defName.Contains("Stove") == true)
                    {
                        Log.Message($"[CustomFoodNames] Found cook at stove: {pawn.Name.ToStringShort}");
                        return pawn;
                    }
                }
            }

            // Method 4: Last resort - find the closest pawn with cooking skill
            if (meal.Spawned && meal.Map != null)
            {
                var cookingPawns = meal.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p.skills?.GetSkill(SkillDefOf.Cooking)?.Level > 0)
                    .OrderBy(p => p.Position.DistanceTo(meal.Position))
                    .ToList();

                if (cookingPawns.Count > 0)
                {
                    var bestCook = cookingPawns[0];
                    Log.Message($"[CustomFoodNames] Using nearest cook as fallback: {bestCook.Name.ToStringShort}");
                    return bestCook;
                }
            }

            return null;
        }

        /// <summary>
        /// Clean up job data when cooking is complete
        /// </summary>
        /// <param name="jobId">The jobId<see cref="int"/></param>
        public static void CleanupJob(int jobId)
        {
            try
            {
                batchJobs.Remove(jobId);
                activeJobs.Remove(jobId);

                Log.Message($"[CustomFoodNames] Cleaned up batch job {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error cleaning up job: {ex}");
            }
        }
    }
}