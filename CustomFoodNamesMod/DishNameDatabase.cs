namespace CustomFoodNamesMod
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Verse;

    /// <summary>
    /// Defines the <see cref="DishNameDatabase" />
    /// </summary>
    public static class DishNameDatabase
    {
        /// <summary>
        /// Defines the IngredientToQualityDishEntries
        /// </summary>
        public static Dictionary<string, Dictionary<string, List<DishEntry>>> IngredientToQualityDishEntries =
            new Dictionary<string, Dictionary<string, List<DishEntry>>>();

        /// <summary>
        /// Defines the IngredientToDishEntries
        /// </summary>
        public static Dictionary<string, List<DishEntry>> IngredientToDishEntries =
            new Dictionary<string, List<DishEntry>>();

        /// <summary>
        /// Defines the IngredientComboDishEntries
        /// </summary>
        public static Dictionary<string, Dictionary<string, List<DishEntry>>> IngredientComboDishEntries =
            new Dictionary<string, Dictionary<string, List<DishEntry>>>();

        /// <summary>
        /// Defines the ThreeIngredientComboDishEntries
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>> ThreeIngredientComboDishEntries =
            new Dictionary<string, Dictionary<string, Dictionary<string, List<DishEntry>>>>();

        // Fallback patterns for ingredients not in database

        /// <summary>
        /// Defines the FallbackPatterns
        /// </summary>
        private static readonly List<string> FallbackPatterns = new List<string>
        {
            "{0} dish",
            "Simple {0} meal",
            "{0} preparation",
            "Basic {0} plate",
            "Plain {0} serving"
        };

        // List to track missing ingredients we've seen for logging purposes

        /// <summary>
        /// Defines the reportedMissingIngredients
        /// </summary>
        private static HashSet<string> reportedMissingIngredients = new HashSet<string>();

        // Flag to enable/disable logging of missing ingredients

        /// <summary>
        /// Defines the LogMissingIngredients
        /// </summary>
        public static bool LogMissingIngredients = false;

        /// <summary>
        /// Initializes static members of the <see cref="DishNameDatabase"/> class.
        /// </summary>
        static DishNameDatabase()
        {
            LoadDatabase();
        }

        /// <summary>
        /// The LoadDatabase
        /// </summary>
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

        /// <summary>
        /// The LoadXmlFromPath
        /// </summary>
        /// <param name="path">The path<see cref="string"/></param>
        private static void LoadXmlFromPath(string path)
        {
            try
            {
                XDocument doc = XDocument.Parse(File.ReadAllText(path));

                // Clear existing data
                IngredientToDishEntries.Clear();
                IngredientComboDishEntries.Clear();
                IngredientToQualityDishEntries.Clear();
                ThreeIngredientComboDishEntries.Clear();

                // Load single ingredient dish entries
                foreach (var ingredientElement in doc.Root.Elements("Ingredient"))
                {
                    string defName = ingredientElement.Attribute("defName")?.Value;
                    string quality = ingredientElement.Attribute("mealQuality")?.Value;

                    if (!string.IsNullOrEmpty(defName))
                    {
                        List<DishEntry> dishEntries = new List<DishEntry>();

                        // Check if using new format with <Dish> elements
                        var dishElements = ingredientElement.Elements("Dish").ToList();
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
                            foreach (var nameElement in ingredientElement.Elements("DishName"))
                            {
                                string name = nameElement.Value;
                                if (!string.IsNullOrEmpty(name))
                                {
                                    dishEntries.Add(new DishEntry(name, null));
                                }
                            }
                        }

                        // If there's a quality attribute, store in quality dictionary
                        if (!string.IsNullOrEmpty(quality))
                        {
                            if (!IngredientToQualityDishEntries.ContainsKey(defName))
                            {
                                IngredientToQualityDishEntries[defName] = new Dictionary<string, List<DishEntry>>();
                            }

                            IngredientToQualityDishEntries[defName][quality] = dishEntries;
                        }
                        // Otherwise store in regular dictionary
                        else
                        {
                            IngredientToDishEntries[defName] = dishEntries;
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

                    List<DishEntry> dishEntries = new List<DishEntry>();

                    // Check if using new format with <Dish> elements
                    var dishElements = comboElement.Elements("Dish").ToList();
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
                        foreach (var nameElement in comboElement.Elements("DishName"))
                        {
                            string name = nameElement.Value;
                            if (!string.IsNullOrEmpty(name))
                            {
                                dishEntries.Add(new DishEntry(name, null));
                            }
                        }
                    }

                    // Handle two-ingredient combinations
                    if (!string.IsNullOrEmpty(ingredient2) && string.IsNullOrEmpty(ingredient3))
                    {
                        // Ensure the dictionaries exist
                        if (!IngredientComboDishEntries.ContainsKey(ingredient1))
                        {
                            IngredientComboDishEntries[ingredient1] = new Dictionary<string, List<DishEntry>>();
                        }

                        IngredientComboDishEntries[ingredient1][ingredient2] = dishEntries;

                        // Also store the reverse combination for easy lookup
                        if (!IngredientComboDishEntries.ContainsKey(ingredient2))
                        {
                            IngredientComboDishEntries[ingredient2] = new Dictionary<string, List<DishEntry>>();
                        }

                        IngredientComboDishEntries[ingredient2][ingredient1] = dishEntries;
                    }
                    // Handle three-ingredient combinations
                    else if (!string.IsNullOrEmpty(ingredient2) && !string.IsNullOrEmpty(ingredient3))
                    {
                        StoreThreeIngredientCombo(ingredient1, ingredient2, ingredient3, dishEntries);
                    }
                }

                // Log loaded data summary for verification
                Log.Message($"[CustomFoodNamesMod] Successfully loaded DishNames.xml");
                Log.Message($"[CustomFoodNamesMod] - Single ingredients loaded: {IngredientToDishEntries.Count}");
                Log.Message($"[CustomFoodNamesMod] - Quality-specific ingredients: {IngredientToQualityDishEntries.Count}");
                Log.Message($"[CustomFoodNamesMod] - Two-ingredient combinations: {IngredientComboDishEntries.Count}");
                Log.Message($"[CustomFoodNamesMod] - Three-ingredient combinations: {ThreeIngredientComboDishEntries.Count}");
            }
            catch (Exception ex)
            {
                Log.Error("[CustomFoodNamesMod] Error loading XML: " + ex);
            }
        }

        // Add this helper method to store three-ingredient combinations

        /// <summary>
        /// The StoreThreeIngredientCombo
        /// </summary>
        /// <param name="ing1">The ing1<see cref="string"/></param>
        /// <param name="ing2">The ing2<see cref="string"/></param>
        /// <param name="ing3">The ing3<see cref="string"/></param>
        /// <param name="entries">The entries<see cref="List{DishEntry}"/></param>
        private static void StoreThreeIngredientCombo(string ing1, string ing2, string ing3, List<DishEntry> entries)
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

                if (!ThreeIngredientComboDishEntries.ContainsKey(a))
                {
                    ThreeIngredientComboDishEntries[a] = new Dictionary<string, Dictionary<string, List<DishEntry>>>();
                }

                if (!ThreeIngredientComboDishEntries[a].ContainsKey(b))
                {
                    ThreeIngredientComboDishEntries[a][b] = new Dictionary<string, List<DishEntry>>();
                }

                ThreeIngredientComboDishEntries[a][b][c] = entries;
            }
        }

        /// <summary>
        /// Gets dish info (name and description) for a list of ingredients
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetDishInfoForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            if (ingredients == null || ingredients.Count == 0)
                return new DishInfo("Mystery meal", "A meal of mysterious origin and content.");

            // Create a deterministic seed based on the ingredients
            // This ensures the same meal will always get the same dish
            // But different meals with the same ingredients will get different dishes
            int seed = 0;
            foreach (var ing in ingredients)
            {
                seed += ing.defName.GetHashCode();
            }

            // Add a unique identifier for this specific meal to the seed
            // For example, you could use the meal's ID or creation timestamp
            // This ensures different meals with the same ingredients get different dishes
            if (Rand.Value < 0.8f) // 80% chance to add variation
            {
                seed += GenTicks.TicksGame;  // Use current game tick for randomness
            }

            if (!string.IsNullOrEmpty(mealQuality))
            {
                seed += mealQuality.GetHashCode();
            }

            Rand.PushState(seed);

            DishInfo result;
            if (ingredients.Count == 1)
            {
                // Single ingredient
                result = GetRandomDishInfo(ingredients[0].defName, mealQuality);
            }
            else if (ingredients.Count == 2)
            {
                // Two ingredients
                result = GetRandomComboDishInfo(ingredients[0].defName, ingredients[1].defName);

                // Fall back to single ingredient if no combo found
                if (result == null)
                {
                    result = GetRandomDishInfo(ingredients[0].defName, mealQuality);
                    if (result != null)
                    {
                        result = new DishInfo(
                            result.Name + " with " + CleanIngredientLabel(ingredients[1].label),
                            result.Description + " " + CleanIngredientLabel(ingredients[1].label) + " adds a complementary flavor.");
                    }
                }
            }
            else if (ingredients.Count == 3)
            {
                // Three ingredients
                result = GetRandomThreeComboDishInfo(
                    ingredients[0].defName,
                    ingredients[1].defName,
                    ingredients[2].defName);
            }
            else
            {
                // For more complex combinations, return null
                result = null;
            }

            Rand.PopState(); // Restore previous random state
            return result;
        }

        /// <summary>
        /// Gets a dish name for a list of ingredients, using the database for small combos
        /// and falling back to procedural generation for complex combinations
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetDishNameForIngredients(List<ThingDef> ingredients, string mealQuality = null)
        {
            DishInfo info = GetDishInfoForIngredients(ingredients, mealQuality);

            if (info != null)
                return info.Name;

            // Fall back to a simple name if no dish info found
            if (ingredients.Count > 0)
                return ingredients[0].label + " dish";

            return "Mystery meal";
        }

        /// <summary>
        /// Gets dish info for a single ingredient, with fallback to generated names
        /// </summary>
        /// <param name="ingredientDefName">The ingredientDefName<see cref="string"/></param>
        /// <param name="mealQuality">The mealQuality<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomDishInfo(string ingredientDefName, string mealQuality = null)
        {
            // The random state is already set in the parent method, so we don't need to push it again

            // Try quality-specific lookup first if quality is provided
            if (!string.IsNullOrEmpty(mealQuality) &&
                IngredientToQualityDishEntries.TryGetValue(ingredientDefName, out var qualityDict) &&
                qualityDict.TryGetValue(mealQuality, out List<DishEntry> qualityDishEntries) &&
                qualityDishEntries.Count > 0)
            {
                var entry = qualityDishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // Try direct lookup
            if (IngredientToDishEntries.TryGetValue(ingredientDefName, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // Try case-insensitive lookup
            var matchingKey = IngredientToDishEntries.Keys.FirstOrDefault(k =>
                string.Equals(k, ingredientDefName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null && IngredientToDishEntries[matchingKey].Count > 0)
            {
                var entry = IngredientToDishEntries[matchingKey].RandomElement();
                return new DishInfo(entry.Name, entry.Description);
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
                string name = string.Format(pattern, cleanLabel);
                string description = $"A simple dish made with {cleanLabel}.";

                return new DishInfo(name, description);
            }

            // Last resort fallback
            return new DishInfo("Mystery dish", "A meal of unknown origin and questionable content.");
        }

        /// <summary>
        /// Gets dish info for a combination of two ingredients
        /// </summary>
        /// <param name="ingredient1">The ingredient1<see cref="string"/></param>
        /// <param name="ingredient2">The ingredient2<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomComboDishInfo(string ingredient1, string ingredient2)
        {
            // Return null if we don't have both ingredients
            if (string.IsNullOrEmpty(ingredient1) || string.IsNullOrEmpty(ingredient2))
                return null;

            // Check if we have a combo for these ingredients
            if (IngredientComboDishEntries.TryGetValue(ingredient1, out var innerDict) &&
                innerDict.TryGetValue(ingredient2, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // We didn't find a match, return null
            return null;
        }

        /// <summary>
        /// Gets dish info for a combination of three ingredients
        /// </summary>
        /// <param name="ingredient1">The ingredient1<see cref="string"/></param>
        /// <param name="ingredient2">The ingredient2<see cref="string"/></param>
        /// <param name="ingredient3">The ingredient3<see cref="string"/></param>
        /// <returns>The <see cref="DishInfo"/></returns>
        public static DishInfo GetRandomThreeComboDishInfo(string ingredient1, string ingredient2, string ingredient3)
        {
            // Return null if we don't have all three ingredients
            if (string.IsNullOrEmpty(ingredient1) ||
                string.IsNullOrEmpty(ingredient2) ||
                string.IsNullOrEmpty(ingredient3))
                return null;

            // Check if we have a combo for these ingredients
            if (ThreeIngredientComboDishEntries.TryGetValue(ingredient1, out var dict1) &&
                dict1.TryGetValue(ingredient2, out var dict2) &&
                dict2.TryGetValue(ingredient3, out List<DishEntry> dishEntries) &&
                dishEntries.Count > 0)
            {
                var entry = dishEntries.RandomElement();
                return new DishInfo(entry.Name, entry.Description);
            }

            // We didn't find a match, return null
            return null;
        }

        /// <summary>
        /// Cleans up ingredient labels for better dish names
        /// </summary>
        /// <param name="label">The label<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
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

        /// <summary>
        /// Defines the <see cref="DishEntry" />
        /// </summary>
        public class DishEntry
        {
            /// <summary>
            /// Gets or sets the Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the Description
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DishEntry"/> class.
            /// </summary>
            /// <param name="name">The name<see cref="string"/></param>
            /// <param name="description">The description<see cref="string"/></param>
            public DishEntry(string name, string description)
            {
                Name = name;
                Description = description ?? "A delicious meal."; // Default description
            }
        }

        /// <summary>
        /// Defines the <see cref="DishInfo" />
        /// </summary>
        public class DishInfo
        {
            /// <summary>
            /// Gets or sets the Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the Description
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DishInfo"/> class.
            /// </summary>
            /// <param name="name">The name<see cref="string"/></param>
            /// <param name="description">The description<see cref="string"/></param>
            public DishInfo(string name, string description)
            {
                Name = name;
                Description = description ?? "";
            }
        }
    }
}
