using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Verse;

namespace CustomFoodNamesMod
{
    public static class DishNameDatabase
    {
        // Single ingredient dish names - now with quality level
        public static Dictionary<string, Dictionary<string, List<string>>> IngredientToQualityDishNames =
            new Dictionary<string, Dictionary<string, List<string>>>();

        // Single ingredient dish names
        public static Dictionary<string, List<string>> IngredientToDishNames = new Dictionary<string, List<string>>();

        // Two ingredient combinations
        public static Dictionary<string, Dictionary<string, List<string>>> IngredientComboDishNames =
            new Dictionary<string, Dictionary<string, List<string>>>();

        // Fallback patterns for ingredients not in database
        private static readonly List<string> FallbackPatterns = new List<string>
        {
            "{0} dish",
            "Simple {0} meal",
            "{0} preparation",
            "Basic {0} plate",
            "Plain {0} serving"
        };

        // List to track missing ingredients we've seen for logging purposes
        private static HashSet<string> reportedMissingIngredients = new HashSet<string>();

        // Flag to enable/disable logging of missing ingredients
        public static bool LogMissingIngredients = false;

        static DishNameDatabase()
        {
            LoadDatabase();
        }

        public static void LoadDatabase()
        {
            try
            {
                Log.Message("[CustomFoodNamesMod] Loading dish name database...");

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
                            // Try XML directly from the fallback path
                            string directXmlPath = Path.Combine(directModPath, "Database", "DishNames.xml");
                            if (File.Exists(directXmlPath))
                            {
                                Log.Message($"[CustomFoodNamesMod] Loading DishNames.xml from fallback path: {directXmlPath}");
                                LoadXmlFromPath(directXmlPath);
                                return;
                            }
                        }

                        Log.Error("[CustomFoodNamesMod] Could not locate mod directory");
                        return;
                    }
                }

                string modRootDir = modContentPack.RootDir;
                string xmlPath = Path.Combine(modRootDir, "Database", "DishNames.xml");

                Log.Message($"[CustomFoodNamesMod] Looking for DishNames.xml at: {xmlPath}");

                if (!File.Exists(xmlPath))
                {
                    Log.Error("[CustomFoodNamesMod] DishNames.xml not found at: " + xmlPath);

                    // Create Database directory if it doesn't exist
                    string databaseDir = Path.Combine(modRootDir, "Database");
                    if (!Directory.Exists(databaseDir))
                    {
                        Directory.CreateDirectory(databaseDir);
                    }

                    // Create the XML file with sample data - now includes combo dishes
                    string sampleXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                    <IngredientDishNames>
                        <Ingredient defName=""RawPotatoes"">
                            <DishName>Kettle Porridge</DishName>
                            <DishName>Mashed Potato Soup</DishName>
                            <DishName>Simple Spud Stew</DishName>
                        </Ingredient>
                        <Ingredient defName=""RawFungus"">
                            <DishName>Fungal Medley</DishName>
                            <DishName>Mushroom Ragout</DishName>
                        </Ingredient>
                        <Ingredient defName=""RawRice"">
                            <DishName>Rice Porridge</DishName>
                            <DishName>Simple Rice Pudding</DishName>
                        </Ingredient>
    
                        <IngredientCombo>
                            <Ingredient1>RawPotatoes</Ingredient1>
                            <Ingredient2>RawFungus</Ingredient2>
                            <DishName>Potato and Mushroom Casserole</DishName>
                            <DishName>Earthy Tuber Stew</DishName>
                        </IngredientCombo>
                    </IngredientDishNames>";

                    File.WriteAllText(xmlPath, sampleXml);
                    Log.Message("[CustomFoodNamesMod] Created sample DishNames.xml file");
                }

                // Load the XML data
                Log.Message("[CustomFoodNamesMod] Loading XML from path: " + xmlPath);
                LoadXmlFromPath(xmlPath);
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error in LoadDatabase: " + ex);
            }
        }

        private static void LoadXmlFromPath(string path)
        {
            try
            {
                XDocument doc = XDocument.Parse(File.ReadAllText(path));

                // Clear existing data
                IngredientToDishNames.Clear();
                IngredientComboDishNames.Clear();
                IngredientToQualityDishNames.Clear();

                // Load single ingredient dish names
                foreach (var ingredientElement in doc.Root.Elements("Ingredient"))
                {
                    string defName = ingredientElement.Attribute("defName")?.Value;
                    string quality = ingredientElement.Attribute("mealQuality")?.Value;

                    if (!string.IsNullOrEmpty(defName))
                    {
                        List<string> dishNames = ingredientElement.Elements("DishName")
                            .Select(d => d.Value)
                            .ToList();

                        // If there's a quality attribute, store in quality dictionary
                        if (!string.IsNullOrEmpty(quality))
                        {
                            if (!IngredientToQualityDishNames.ContainsKey(defName))
                            {
                                IngredientToQualityDishNames[defName] = new Dictionary<string, List<string>>();
                            }

                            IngredientToQualityDishNames[defName][quality] = dishNames;
                        }
                        // Otherwise store in regular dictionary
                        else
                        {
                            IngredientToDishNames[defName] = dishNames;
                        }
                    }
                }

                // Load ingredient combinations
                foreach (var comboElement in doc.Root.Elements("IngredientCombo"))
                {
                    string ingredient1 = comboElement.Element("Ingredient1")?.Value;
                    string ingredient2 = comboElement.Element("Ingredient2")?.Value;

                    if (string.IsNullOrEmpty(ingredient1) || string.IsNullOrEmpty(ingredient2))
                        continue;

                    List<string> dishNames = comboElement.Elements("DishName")
                        .Select(d => d.Value)
                        .ToList();

                    // Ensure the dictionaries exist
                    if (!IngredientComboDishNames.ContainsKey(ingredient1))
                    {
                        IngredientComboDishNames[ingredient1] = new Dictionary<string, List<string>>();
                    }

                    IngredientComboDishNames[ingredient1][ingredient2] = dishNames;

                    // Also store the reverse combination for easy lookup
                    if (!IngredientComboDishNames.ContainsKey(ingredient2))
                    {
                        IngredientComboDishNames[ingredient2] = new Dictionary<string, List<string>>();
                    }

                    IngredientComboDishNames[ingredient2][ingredient1] = dishNames;
                }

                // Log loaded data summary for verification
                Log.Message($"[CustomFoodNamesMod] Successfully loaded DishNames.xml");
                Log.Message($"[CustomFoodNamesMod] - Single ingredients loaded: {IngredientToDishNames.Count}");
                Log.Message($"[CustomFoodNamesMod] - Quality-specific ingredients: {IngredientToQualityDishNames.Count}");
                Log.Message($"[CustomFoodNamesMod] - Ingredient combinations: {IngredientComboDishNames.Count}");

                // Log each loaded ingredient for verification
                Log.Message("[CustomFoodNamesMod] Loaded single ingredients: " +
                    string.Join(", ", IngredientToDishNames.Keys));

                // Log quality-specific ingredients
                foreach (var ingredient in IngredientToQualityDishNames.Keys)
                {
                    Log.Message($"[CustomFoodNamesMod] Quality variations for {ingredient}: " +
                        string.Join(", ", IngredientToQualityDishNames[ingredient].Keys));
                }

                // Log ingredient combinations
                Log.Message("[CustomFoodNamesMod] Loaded ingredient combinations: " +
                    string.Join(", ", IngredientComboDishNames.Keys));
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error loading XML: " + ex);
            }
        }

        /// <summary>
        /// Gets a dish name for a list of ingredients, using the database for small combos
        /// and falling back to procedural generation for complex combinations
        /// </summary>
        public static string GetDishNameForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery meal";

            if (ingredients.Count == 1)
            {
                // Single ingredient - now with quality
                string randomDishName = GetRandomDishName(ingredients[0].defName, mealQuality);
                return !string.IsNullOrEmpty(randomDishName)
                    ? randomDishName
                    : ingredients[0].label + " dish";
            }
            else if (ingredients.Count == 2)
            {
                // Two ingredients - try to get a combo dish name
                string randomComboDishName = GetRandomComboDishName(
                    ingredients[0].defName,
                    ingredients[1].defName);

                if (!string.IsNullOrEmpty(randomComboDishName))
                    return randomComboDishName;

                // Fall back to using the first ingredient with quality
                string randomDishName = GetRandomDishName(ingredients[0].defName, mealQuality);
                return !string.IsNullOrEmpty(randomDishName)
                    ? randomDishName + " with " + ingredients[1].label
                    : ingredients[0].label + " and " + ingredients[1].label + " dish";
            }

            // For more complex combinations, use the procedural generator
            // but we'll return null here so the call site can decide what to do
            return null;
        }

        /// <summary>
        /// Gets a random dish name for a single ingredient, with fallback to generated names
        /// </summary>
        public static string GetRandomDishName(string ingredientDefName, string mealQuality = null)
        {
            // Try quality-specific lookup first if quality is provided
            if (!string.IsNullOrEmpty(mealQuality) &&
                IngredientToQualityDishNames.TryGetValue(ingredientDefName, out var qualityDict) &&
                qualityDict.TryGetValue(mealQuality, out List<string> qualityDishNames) &&
                qualityDishNames.Count > 0)
            {
                return qualityDishNames.RandomElement();
            }

            // Try direct lookup
            if (IngredientToDishNames.TryGetValue(ingredientDefName, out List<string> dishNames) && dishNames.Count > 0)
            {
                return dishNames.RandomElement();
            }

            // Try case-insensitive lookup
            var matchingKey = IngredientToDishNames.Keys.FirstOrDefault(k =>
                string.Equals(k, ingredientDefName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null && IngredientToDishNames[matchingKey].Count > 0)
            {
                return IngredientToDishNames[matchingKey].RandomElement();
            }

            // No exact match found, try to get the ingredient label and generate a name
            var ingredientDef = DefDatabase<ThingDef>.GetNamed(ingredientDefName, false);
            if (ingredientDef != null)
            {
                // Log missing ingredient only once
                if (LogMissingIngredients && !reportedMissingIngredients.Contains(ingredientDefName))
                {
                    Log.Message($"[CustomFoodNames] Missing dish name for ingredient: {ingredientDefName} (label: {ingredientDef.label})");
                    reportedMissingIngredients.Add(ingredientDefName);
                }

                // Clean up the ingredient label
                string cleanLabel = CleanIngredientLabel(ingredientDef.label);

                // Select a random pattern and apply it
                string pattern = FallbackPatterns.RandomElement();
                return string.Format(pattern, cleanLabel);
            }

            // Last resort fallback
            return "Mystery dish";
        }

        /// <summary>
        /// Gets a random dish name for a combination of two ingredients
        /// </summary>
        public static string GetRandomComboDishName(string ingredient1, string ingredient2)
        {
            // Return null if we don't have both ingredients
            if (string.IsNullOrEmpty(ingredient1) || string.IsNullOrEmpty(ingredient2))
                return null;

            // Check if we have a combo for these ingredients
            if (IngredientComboDishNames.TryGetValue(ingredient1, out var innerDict) &&
                innerDict.TryGetValue(ingredient2, out List<string> dishNames) &&
                dishNames.Count > 0)
            {
                return dishNames.RandomElement();
            }

            // We didn't find a match, return null
            return null;
        }

        /// <summary>
        /// Cleans up ingredient labels for better dish names
        /// </summary>
        private static string CleanIngredientLabel(string label)
        {
            // Remove "raw" prefix if present
            string cleanLabel = Regex.Replace(label, @"^raw\s+", "", RegexOptions.IgnoreCase);

            // Capitalize first letter
            if (cleanLabel.Length > 0)
            {
                cleanLabel = char.ToUpperInvariant(cleanLabel[0]) + cleanLabel.Substring(1);
            }

            return cleanLabel;
        }
    }
}