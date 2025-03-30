using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Verse;

namespace CustomFoodNamesMod.Database
{
    /// <summary>
    /// Handles loading and saving of dish name database
    /// </summary>
    public static class DatabaseLoader
    {
        /// <summary>
        /// Load dish database from XML
        /// </summary>
        public static void LoadDatabase(
            Dictionary<string, List<DishEntry>> singleIngredientEntries,
            Dictionary<string, Dictionary<string, List<DishEntry>>> ingredientCombos,
            Dictionary<string, Dictionary<string, List<DishEntry>>> qualityEntries,
            Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>> threeIngredientCombos)
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
                                LoadXmlFromPath(directXmlPath, singleIngredientEntries, ingredientCombos, qualityEntries, threeIngredientCombos);
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

                    // Create the XML file with sample data
                    CreateSampleXmlFile(xmlPath);
                    Log.Message("[CustomFoodNamesMod] Created sample DishNames.xml file");
                }

                // Load the XML data
                Log.Message("[CustomFoodNamesMod] Loading XML from path: " + xmlPath);
                LoadXmlFromPath(xmlPath, singleIngredientEntries, ingredientCombos, qualityEntries, threeIngredientCombos);
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error in LoadDatabase: " + ex);
            }
        }

        /// <summary>
        /// Load XML from file path
        /// </summary>
        private static void LoadXmlFromPath(
            string path,
            Dictionary<string, List<DishEntry>> singleIngredientEntries,
            Dictionary<string, Dictionary<string, List<DishEntry>>> ingredientCombos,
            Dictionary<string, Dictionary<string, List<DishEntry>>> qualityEntries,
            Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>> threeIngredientCombos)
        {
            try
            {
                XDocument doc = XDocument.Parse(File.ReadAllText(path));

                // Clear existing data
                singleIngredientEntries.Clear();
                ingredientCombos.Clear();
                qualityEntries.Clear();
                threeIngredientCombos.Clear();

                // Load single ingredient dish entries
                foreach (var ingredientElement in doc.Root.Elements("Ingredient"))
                {
                    string defName = ingredientElement.Attribute("defName")?.Value;
                    string quality = ingredientElement.Attribute("mealQuality")?.Value;

                    if (!string.IsNullOrEmpty(defName))
                    {
                        List<DishEntry> dishEntries = LoadDishEntriesFromElement(ingredientElement);

                        // If there's a quality attribute, store in quality dictionary
                        if (!string.IsNullOrEmpty(quality))
                        {
                            if (!qualityEntries.ContainsKey(defName))
                            {
                                qualityEntries[defName] = new Dictionary<string, List<DishEntry>>();
                            }

                            qualityEntries[defName][quality] = dishEntries;
                        }
                        // Otherwise store in regular dictionary
                        else
                        {
                            singleIngredientEntries[defName] = dishEntries;
                        }
                    }
                }

                // Load ingredient combinations
                foreach (var comboElement in doc.Root.Elements("IngredientCombo"))
                {
                    string ingredient1 = comboElement.Element("Ingredient1")?.Value;
                    string ingredient2 = comboElement.Element("Ingredient2")?.Value;
                    string ingredient3 = comboElement.Element("Ingredient3")?.Value;

                    if (string.IsNullOrEmpty(ingredient1))
                        continue;

                    List<DishEntry> dishEntries = LoadDishEntriesFromElement(comboElement);

                    // Handle two-ingredient combinations
                    if (!string.IsNullOrEmpty(ingredient2) && string.IsNullOrEmpty(ingredient3))
                    {
                        // Ensure the dictionaries exist
                        if (!ingredientCombos.ContainsKey(ingredient1))
                        {
                            ingredientCombos[ingredient1] = new Dictionary<string, List<DishEntry>>();
                        }

                        ingredientCombos[ingredient1][ingredient2] = dishEntries;

                        // Also store the reverse combination for easy lookup
                        if (!ingredientCombos.ContainsKey(ingredient2))
                        {
                            ingredientCombos[ingredient2] = new Dictionary<string, List<DishEntry>>();
                        }

                        ingredientCombos[ingredient2][ingredient1] = dishEntries;
                    }
                    // Handle three-ingredient combinations
                    else if (!string.IsNullOrEmpty(ingredient2) && !string.IsNullOrEmpty(ingredient3))
                    {
                        StoreThreeIngredientCombo(threeIngredientCombos, ingredient1, ingredient2, ingredient3, dishEntries);
                    }
                }

                // Log loaded data summary for verification
                Log.Message($"[CustomFoodNamesMod] Successfully loaded DishNames.xml");
                Log.Message($"[CustomFoodNamesMod] - Single ingredients loaded: {singleIngredientEntries.Count}");
                Log.Message($"[CustomFoodNamesMod] - Quality-specific ingredients: {qualityEntries.Count}");
                Log.Message($"[CustomFoodNamesMod] - Two-ingredient combinations: {ingredientCombos.Count}");
                Log.Message($"[CustomFoodNamesMod] - Three-ingredient combinations: {threeIngredientCombos.Count}");
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error loading XML: " + ex);
            }
        }

        /// <summary>
        /// Load dish entries from an XML element
        /// </summary>
        private static List<DishEntry> LoadDishEntriesFromElement(XElement element)
        {
            List<DishEntry> dishEntries = new List<DishEntry>();

            // Check if using new format with <Dish> elements
            var dishElements = element.Elements("Dish").ToList();
            if (dishElements.Any())
            {
                // Process each Dish element
                foreach (var dishElement in dishElements)
                {
                    string name = dishElement.Element("Name")?.Value;
                    string description = dishElement.Element("Description")?.Value;

                    if (!string.IsNullOrEmpty(name))
                    {
                        dishEntries.Add(new DishEntry(name, description));
                    }
                }
            }
            else
            {
                // Backward compatibility with old format using DishName elements
                foreach (var nameElement in element.Elements("DishName"))
                {
                    string name = nameElement.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        dishEntries.Add(new DishEntry(name, null));
                    }
                }
            }

            return dishEntries;
        }

        /// <summary>
        /// Store three-ingredient combination in all permutations for easier lookup
        /// </summary>
        private static void StoreThreeIngredientCombo(
            Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>> threeIngredientCombos,
            string ing1, string ing2, string ing3, List<DishEntry> entries)
        {
            // Create all permutations for the three ingredients for easier lookup
            string[][] permutations = new[]
            {
                new[] { ing1, ing2, ing3 },
                new[] { ing1, ing3, ing2 },
                new[] { ing2, ing1, ing3 },
                new[] { ing2, ing3, ing1 },
                new[] { ing3, ing1, ing2 },
                new[] { ing3, ing2, ing1 }
            };

            foreach (var perm in permutations)
            {
                string a = perm[0];
                string b = perm[1];
                string c = perm[2];

                if (!threeIngredientCombos.ContainsKey(a))
                {
                    threeIngredientCombos[a] = new Dictionary<string, Dictionary<string, List<DishEntry>>>();
                }

                if (!threeIngredientCombos[a].ContainsKey(b))
                {
                    threeIngredientCombos[a][b] = new Dictionary<string, List<DishEntry>>();
                }

                threeIngredientCombos[a][b][c] = entries;
            }
        }

        /// <summary>
        /// Create a sample XML file with example entries
        /// </summary>
        private static void CreateSampleXmlFile(string filePath)
        {
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

            File.WriteAllText(filePath, sampleXml);
        }
    }
}