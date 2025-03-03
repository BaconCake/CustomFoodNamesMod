using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Verse;

namespace CustomFoodNamesMod
{
    public static class DishNameDatabase
    {
        public static Dictionary<string, List<string>> IngredientToDishNames = new Dictionary<string, List<string>>();

        static DishNameDatabase()
        {
            LoadDatabase();
        }

        public static void LoadDatabase()
        {
            try
            {
                Log.Message("[CustomFoodNamesMod] Starting LoadDatabase method...");

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
</IngredientDishNames>";

                    File.WriteAllText(xmlPath, sampleXml);
                }

                // Load the XML data
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

                // Build the dictionary
                IngredientToDishNames = doc.Root
                    .Elements("Ingredient")
                    .ToDictionary(
                        x => x.Attribute("defName")?.Value,
                        x => x.Elements("DishName").Select(d => d.Value).ToList()
                    );

                Log.Message("[CustomFoodNamesMod] Loaded DishNames for ingredients: " +
                    string.Join(", ", IngredientToDishNames.Keys));
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error loading XML: " + ex);
            }
        }

        public static string GetRandomDishName(string ingredientDefName)
        {
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

            return null;
        }
    }
}