using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;
using static CustomFoodNamesMod.IngredientCategoryResolver;

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
                FindUncategorizedIngredients();
            }
        }

        /// <summary>
        /// Determines if a ThingDef is animal feed and not meant for human consumption
        /// </summary>
        private static bool IsAnimalFeedOnly(ThingDef def)
        {
            // Check the name for common animal feed terms
            string defName = def.defName.ToLowerInvariant();
            string label = def.label.ToLowerInvariant();

            // Check special cases by name
            if (defName == "hay" || label == "hay")
                return true;

            // Check if it's only for animals based on food type
            if (def.ingestible != null)
            {
                // If it's not for humans but is for herbivores or carnivores, it's likely animal feed
                if (!def.ingestible.foodType.HasFlag(FoodTypeFlags.Meal) &&
                    (def.ingestible.foodType.HasFlag(FoodTypeFlags.Plant) ||
                     def.ingestible.foodType.HasFlag(FoodTypeFlags.Tree) ||
                     def.ingestible.foodType.HasFlag(FoodTypeFlags.Corpse)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find all ingredients that don't have a category assigned yet
        /// </summary>
        public static void FindUncategorizedIngredients()
        {
            try
            {
                // Get all ThingDefs that are valid cooking ingredients
                var allPotentialIngredients = DefDatabase<ThingDef>.AllDefs
                    .Where(def => IsValidCookingIngredient(def))
                    .ToList();

                Log.Message($"[CustomFoodNames] Found {allPotentialIngredients.Count} potential ingredients");

                // Track uncategorized ingredients
                var uncategorizedIngredients = new List<ThingDef>();

                // Check which ingredients don't have explicit category mappings
                foreach (var ingredient in allPotentialIngredients)
                {
                    // Get the category using our resolver
                    var category = GetIngredientCategory(ingredient);

                    // If it's "Other" or the category was inferred (not explicitly mapped),
                    // add it to our uncategorized list, but exclude animal feed
                    if (category == IngredientCategory.Other && !IsAnimalFeedOnly(ingredient))
                    {
                        Log.Message($"[CustomFoodNames] Uncategorized ingredient: {ingredient.defName} - Label: {ingredient.label}");
                        uncategorizedIngredients.Add(ingredient);
                    }
                }

                // Save uncategorized ingredients to a file
                string modDir = GetModDirectory();
                if (!string.IsNullOrEmpty(modDir))
                {
                    string uncategorizedPath = Path.Combine(modDir, "UncategorizedIngredients.xml");

                    // Create XML template focused only on ingredient list
                    string xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
                                     "<IngredientCategories>\n" +
                                     "    <!-- These ingredients need to be categorized -->\n" +
                                     "    <!-- Available categories: Protein, Carb, Vegetable, Dairy, Fat, Sweetener, Flavoring, Exotic, Other -->\n\n";

                    foreach (var ingredient in uncategorizedIngredients)
                    {
                        xmlContent += $"    <IngredientMapping>\n" +
                                      $"        <DefName>{ingredient.defName}</DefName>\n" +
                                      $"        <!-- {ingredient.label} -->\n" +
                                      $"        <Category>Other</Category> <!-- Update with correct category -->\n" +
                                      $"    </IngredientMapping>\n\n";
                    }

                    xmlContent += "</IngredientCategories>";

                    File.WriteAllText(uncategorizedPath, xmlContent);
                    Log.Message($"[CustomFoodNames] Written uncategorized ingredients to: {uncategorizedPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error in FindUncategorizedIngredients: {ex}");
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
        /// Helper method to get the mod directory
        /// </summary>
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