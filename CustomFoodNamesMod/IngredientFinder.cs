using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomFoodNamesMod
{
    [StaticConstructorOnStartup]
    public static class IngredientFinder
    {
        static IngredientFinder()
        {
            // Only run this in debug mode or when specifically triggered
            if (Prefs.DevMode)
            {
                Log.Message("[CustomFoodNames] Running ingredient finder...");
                FindAllPotentialIngredients();
            }
        }

        public static void FindAllPotentialIngredients()
        {
            try
            {
                // Get all ThingDefs that are valid cooking ingredients
                var allPotentialIngredients = DefDatabase<ThingDef>.AllDefs
                    .Where(def => IsValidCookingIngredient(def))
                    .ToList();

                Log.Message($"[CustomFoodNames] Found {allPotentialIngredients.Count} potential ingredients");

                // Create lists to track ingredients
                var ingredientsWithDishNames = new HashSet<string>(
                    DishNameDatabase.IngredientToDishNames.Keys);

                var ingredientsWithoutDishNames = new List<ThingDef>();

                // Log info about each ingredient
                foreach (var ingredient in allPotentialIngredients)
                {
                    if (ingredientsWithDishNames.Contains(ingredient.defName))
                    {
                        Log.Message($"[CustomFoodNames] Already have dish name for: {ingredient.defName} - Label: {ingredient.label}");
                    }
                    else
                    {
                        Log.Message($"[CustomFoodNames] Missing dish name for: {ingredient.defName} - Label: {ingredient.label}");
                        ingredientsWithoutDishNames.Add(ingredient);
                    }
                }

                // Save missing ingredients to a file
                string modDir = GetModDirectory();
                if (!string.IsNullOrEmpty(modDir))
                {
                    string missingIngredientsPath = Path.Combine(modDir, "MissingIngredients.xml");

                    // Create XML template
                    string xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
                                     "<IngredientDishNames>\n";

                    foreach (var ingredient in ingredientsWithoutDishNames)
                    {
                        xmlContent += $"    <Ingredient defName=\"{ingredient.defName}\">\n" +
                                      $"        <!-- {ingredient.label} -->\n" +
                                      $"        <DishName>{CleanIngredientName(ingredient.label)} Dish</DishName>\n" +
                                      $"        <DishName>Simple {CleanIngredientName(ingredient.label)} Meal</DishName>\n" +
                                      $"        <DishName>{CleanIngredientName(ingredient.label)} Preparation</DishName>\n" +
                                      $"    </Ingredient>\n";
                    }

                    xmlContent += "</IngredientDishNames>";

                    File.WriteAllText(missingIngredientsPath, xmlContent);
                    Log.Message($"[CustomFoodNames] Written missing ingredients template to: {missingIngredientsPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in FindAllPotentialIngredients: {ex}");
            }
        }

        /// <summary>
        /// Determines if a ThingDef is a valid cooking ingredient in RimWorld
        /// </summary>
        private static bool IsValidCookingIngredient(ThingDef def)
        {
            // Skip if not ingestible or is a corpse
            if (!def.IsIngestible || def.IsCorpse)
                return false;

            // Skip drugs and alcohol
            if (def.ingestible?.drugCategory != DrugCategory.None)
                return false;

            // Skip prepared meals (they shouldn't be ingredients)
            if (def.defName.StartsWith("Meal") || def.defName == "Kibble" || def.defName == "Pemmican")
                return false;

            // Skip plants (we want the harvested product, not the plant itself)
            if (def.defName.StartsWith("Plant_"))
                return false;

            // Skip serums and medicine
            if (def.defName.EndsWith("Serum") || def.IsMedicine)
                return false;

            // Skip HemogenPack and BabyFood
            if (def.defName == "HemogenPack" || def.defName == "BabyFood")
                return false;

            // Skip things with drug food type
            if (def.ingestible?.foodType == FoodTypeFlags.Liquor)
                return false;

            // Focus on things that are actually used in cooking
            bool isRawFood = def.defName.StartsWith("Raw") ||      // RawPotatoes, RawRice, etc.
                             def.defName.StartsWith("Meat_") ||    // Meat_Cow, Meat_Chicken, etc.
                             def.defName.StartsWith("Egg") ||      // EggChickenUnfertilized, etc.
                             def.defName == "Milk" ||
                             def.defName == "InsectJelly" ||
                             def.defName == "Hay" ||
                             def.defName == "Chocolate";

            return isRawFood;
        }

        /// <summary>
        /// Cleans up the ingredient name for better dish name generation
        /// </summary>
        private static string CleanIngredientName(string label)
        {
            // Remove "raw" prefix if present (case insensitive)
            string cleanLabel = System.Text.RegularExpressions.Regex.Replace(
                label,
                @"^raw\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Capitalize first letter if needed
            if (!string.IsNullOrEmpty(cleanLabel) && char.IsLower(cleanLabel[0]))
            {
                cleanLabel = char.ToUpper(cleanLabel[0]) + cleanLabel.Substring(1);
            }

            return cleanLabel;
        }

        private static string GetModDirectory()
        {
            try
            {
                // Search for our mod by assembly
                var modContentPack = LoadedModManager.RunningModsListForReading
                    .FirstOrDefault(m => m.assemblies.loadedAssemblies
                        .Any(a => a.GetType().Namespace?.StartsWith("CustomFoodNamesMod") == true));

                if (modContentPack == null)
                {
                    // Try finding by packageId
                    modContentPack = LoadedModManager.RunningModsListForReading
                        .FirstOrDefault(m => m.PackageId == "DeinName.CustomFoodNames");

                    if (modContentPack == null)
                    {
                        // Direct path as fallback
                        string modsFolderPath = GenFilePaths.ModsFolderPath;
                        string directModPath = Path.Combine(modsFolderPath, "CustomFoodNames");

                        if (Directory.Exists(directModPath))
                        {
                            return directModPath;
                        }

                        Log.Error("[CustomFoodNames] Could not locate mod directory");
                        return null;
                    }
                }

                return modContentPack.RootDir;
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error getting mod directory: {ex}");
                return null;
            }
        }
    }
}