using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using System.Xml.Linq;

namespace CustomFoodNamesMod
{
    /// <summary>
    /// Handles mapping ThingDefs to ingredient categories for template-based dish naming
    /// </summary>
    public static class IngredientCategoryResolver
    {
        // Core ingredient categories
        public enum IngredientCategory
        {
            Protein,    // Meats, insects, tofu, etc.
            Carb,       // Rice, potatoes, bread, etc.
            Vegetable,  // Wild root, leafy greens, etc.
            Dairy,      // Milk, cheese, etc.
            Fat,        // Insect jelly, animal fat, etc.
            Sweetener,  // Honey, fruits, etc.
            Flavoring,  // Herbs, garlic, onions, etc.
            Exotic,     // Human meat, special ingredients, etc.
            Other       // Default fallback
        }

        // Main mapping of ThingDef defNames to categories
        private static readonly Dictionary<string, IngredientCategory> IngredientCategoryMap =
            new Dictionary<string, IngredientCategory>();

        // Cache of ingredient categories to avoid lookups
        private static readonly Dictionary<string, IngredientCategory> IngredientCategoryCache =
            new Dictionary<string, IngredientCategory>();

        // Initialize the categorization system
        static IngredientCategoryResolver()
        {
            InitializeBaseCategoryMap();
            LoadCustomCategoryMappings();
        }

        /// <summary>
        /// Set up base mappings for common vanilla ingredients
        /// </summary>
        private static void InitializeBaseCategoryMap()
        {
            // Proteins (meats) - treat all meats the same way
            string[] proteins = new[] {
                "Meat_Human", "Meat_Megaspider", "Meat_Thrumbo", "Meat_Cow", "Meat_Chicken",
                "Meat_Pig", "Meat_Dog", "Meat_Elephant", "Meat_Alpaca", "Meat_Boomalope",
                "Meat_Boomrat", "Meat_Chameleon", "Meat_Chinchilla", "Meat_Cobra", "Meat_Cougar",
                "Meat_Deer", "Meat_Dromedary", "Meat_Elk", "Meat_Fox", "Meat_Gazelle",
                "Meat_Hare", "Meat_Husky", "Meat_Iguana", "Meat_Muffalo", "Meat_Panther",
                "Meat_Monkey", "Meat_Ostrich", "Meat_Rat", "Meat_Rhinoceros", "Meat_Squirrel",
                "Meat_Tortoise", "Meat_Turkey", "Meat_Wolf", "Meat_Warg", "Meat_YorkshireTerrier"
            };
            foreach (var protein in proteins)
            {
                IngredientCategoryMap[protein] = IngredientCategory.Protein;
            }

            // No longer marking any meats as exotic
            // All meats are treated the same now

            // Carbs
            IngredientCategoryMap["RawRice"] = IngredientCategory.Carb;
            IngredientCategoryMap["RawPotatoes"] = IngredientCategory.Carb;
            IngredientCategoryMap["RawCorn"] = IngredientCategory.Carb;

            // Vegetables
            IngredientCategoryMap["RawFungus"] = IngredientCategory.Vegetable;
            IngredientCategoryMap["RawAgave"] = IngredientCategory.Vegetable;

            // Sweeteners
            IngredientCategoryMap["RawBerries"] = IngredientCategory.Sweetener;
            IngredientCategoryMap["Chocolate"] = IngredientCategory.Sweetener;

            // Dairy
            IngredientCategoryMap["Milk"] = IngredientCategory.Dairy;

            // Fats
            IngredientCategoryMap["InsectJelly"] = IngredientCategory.Fat;

            // Eggs - categorize all eggs as protein
            string[] eggs = new[] {
                "EggChickenUnfertilized", "EggChickenFertilized",
                "EggCobraFertilized", "EggIguanaFertilized",
                "EggTortoiseFertilized", "EggCassowaryFertilized",
                "EggEmuFertilized", "EggOstrichFertilized",
                "EggTurkeyFertilized", "EggDuckUnfertilized",
                "EggDuckFertilized", "EggGooseUnfertilized",
                "EggGooseFertilized"
            };
            foreach (var egg in eggs)
            {
                IngredientCategoryMap[egg] = IngredientCategory.Protein;
            }

            // Animal feed (not really human food)
            IngredientCategoryMap["Hay"] = IngredientCategory.Other;
        }

        /// <summary>
        /// Load any custom category mappings from XML file
        /// </summary>
        private static void LoadCustomCategoryMappings()
        {
            try
            {
                // Get mod directory
                string modDir = GetModDirectory();
                if (string.IsNullOrEmpty(modDir))
                {
                    Log.Error("[CustomFoodNames] Could not locate mod directory for custom category mappings");
                    return;
                }

                string mappingFile = Path.Combine(modDir, "Database", "IngredientCategories.xml");
                if (!File.Exists(mappingFile))
                {
                    // Create default mappings file if it doesn't exist
                    CreateDefaultMappingsFile(mappingFile);
                    return;
                }

                // Load the XML mappings
                XDocument doc = XDocument.Parse(File.ReadAllText(mappingFile));

                foreach (var mapping in doc.Root.Elements("IngredientMapping"))
                {
                    string defName = mapping.Element("DefName")?.Value;
                    string categoryStr = mapping.Element("Category")?.Value;

                    if (string.IsNullOrEmpty(defName) || string.IsNullOrEmpty(categoryStr))
                        continue;

                    // Try to parse the category
                    if (Enum.TryParse<IngredientCategory>(categoryStr, true, out var category))
                    {
                        IngredientCategoryMap[defName] = category;
                    }
                }

                Log.Message($"[CustomFoodNames] Loaded {IngredientCategoryMap.Count} ingredient category mappings");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error loading ingredient category mappings: {ex}");
            }
        }

        /// <summary>
        /// Create a default mappings file with examples
        /// </summary>
        private static void CreateDefaultMappingsFile(string filePath)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Create a basic XML template
                string template = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<IngredientCategories>
    <!-- Examples of custom ingredient mappings -->
    <!-- Available categories: Protein, Carb, Vegetable, Dairy, Fat, Sweetener, Flavoring, Exotic, Other -->
    
    <IngredientMapping>
        <DefName>RawRice</DefName>
        <Category>Carb</Category>
    </IngredientMapping>
    
    <IngredientMapping>
        <DefName>RawPotatoes</DefName>
        <Category>Carb</Category>
    </IngredientMapping>
    
    <IngredientMapping>
        <DefName>RawFungus</DefName>
        <Category>Vegetable</Category>
    </IngredientMapping>
    
    <IngredientMapping>
        <DefName>RawBerries</DefName>
        <Category>Sweetener</Category>
    </IngredientMapping>
    
    <IngredientMapping>
        <DefName>Milk</DefName>
        <Category>Dairy</Category>
    </IngredientMapping>
    
    <!-- Meats are automatically categorized as Protein -->
    <!-- Add your custom ingredients here -->
</IngredientCategories>";

                File.WriteAllText(filePath, template);
                Log.Message($"[CustomFoodNames] Created default ingredient category mappings at {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error creating default mappings file: {ex}");
            }
        }

        /// <summary>
        /// Get the category for a given ingredient
        /// </summary>
        public static IngredientCategory GetIngredientCategory(ThingDef ingredient)
        {
            if (ingredient == null)
                return IngredientCategory.Other;

            // Check cache first
            if (IngredientCategoryCache.TryGetValue(ingredient.defName, out var cachedCategory))
                return cachedCategory;

            // Check our main map
            if (IngredientCategoryMap.TryGetValue(ingredient.defName, out var category))
            {
                IngredientCategoryCache[ingredient.defName] = category;
                return category;
            }

            // Try to infer category by name patterns
            category = InferCategoryByName(ingredient);

            // Cache for future lookups
            IngredientCategoryCache[ingredient.defName] = category;
            return category;
        }

        /// <summary>
        /// Try to determine ingredient category by defName or label patterns
        /// </summary>
        private static IngredientCategory InferCategoryByName(ThingDef ingredient)
        {
            string defName = ingredient.defName;
            string label = ingredient.label.ToLowerInvariant();

            // Egg detection
            if (defName.StartsWith("Egg") || label.Contains("egg"))
                return IngredientCategory.Protein;

            // Meat detection
            if (defName.StartsWith("Meat_") || label.Contains("meat"))
                return IngredientCategory.Protein;

            // Vegetable inference
            if (defName.StartsWith("Plant") ||
                label.Contains("vegetable") ||
                label.Contains("leaf") ||
                label.Contains("cabbage") ||
                label.Contains("lettuce"))
                return IngredientCategory.Vegetable;

            // Fruit/sweetener detection
            if (label.Contains("fruit") ||
                label.Contains("berry") ||
                label.Contains("sweet") ||
                label.Contains("honey"))
                return IngredientCategory.Sweetener;

            // Basic carb detection
            if (label.Contains("rice") ||
                label.Contains("potato") ||
                label.Contains("bread") ||
                label.Contains("corn") ||
                label.Contains("grain") ||
                label.Contains("wheat"))
                return IngredientCategory.Carb;

            // Dairy detection
            if (label.Contains("milk") ||
                label.Contains("cheese") ||
                label.Contains("cream") ||
                label.Contains("yogurt") ||
                label.Contains("butter"))
                return IngredientCategory.Dairy;

            // Animal feed detection - not human food
            if (label.Contains("hay") ||
                label.Contains("feed") ||
                label.Contains("kibble"))
                return IngredientCategory.Other;

            // General fallback for raw food
            if (defName.StartsWith("Raw"))
                return IngredientCategory.Vegetable;

            // Default fallback
            return IngredientCategory.Other;
        }

        /// <summary>
        /// Get all ingredients of a specific category from a list
        /// </summary>
        public static List<ThingDef> GetIngredientsOfCategory(List<ThingDef> ingredients, IngredientCategory category)
        {
            return ingredients
                .Where(i => GetIngredientCategory(i) == category)
                .ToList();
        }

        /// <summary>
        /// Get the most common ingredient category in a meal
        /// </summary>
        public static IngredientCategory GetDominantCategory(List<ThingDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return IngredientCategory.Other;

            // Count ingredient categories
            var categoryCounts = new Dictionary<IngredientCategory, int>();

            foreach (var ingredient in ingredients)
            {
                var category = GetIngredientCategory(ingredient);

                if (!categoryCounts.ContainsKey(category))
                    categoryCounts[category] = 0;

                categoryCounts[category]++;
            }

            // Return the most common category
            return categoryCounts
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get a list of all categories present in a meal's ingredients
        /// </summary>
        public static List<IngredientCategory> GetAllCategories(List<ThingDef> ingredients)
        {
            return ingredients
                .Select(i => GetIngredientCategory(i))
                .Distinct()
                .ToList();
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