namespace CustomFoodNamesMod
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Verse;
    using static CustomFoodNamesMod.IngredientCategoryResolver;

    /// <summary>
    /// Generates meal names using templates based on ingredient categories
    /// </summary>
    public static class TemplateDishGenerator
    {
        // Meal quality levels

        /// <summary>
        /// Defines the MealQuality
        /// </summary>
        public enum MealQuality
        {
            /// <summary>
            /// Defines the Simple
            /// </summary>
            Simple,

            /// <summary>
            /// Defines the Fine
            /// </summary>
            Fine,

            /// <summary>
            /// Defines the Lavish
            /// </summary>
            Lavish
        }

        // Types of templates for different meal compositions

        /// <summary>
        /// Defines the TemplateType
        /// </summary>
        private enum TemplateType
        {
            /// <summary>
            /// Defines the SingleCategory
            /// </summary>
            SingleCategory,    // Only one ingredient category (e.g. all vegetables)

            /// <summary>
            /// Defines the DualCategory
            /// </summary>
            DualCategory,      // Two ingredient categories (e.g. protein + carb)

            /// <summary>
            /// Defines the MultiCategory
            /// </summary>
            MultiCategory,     // Three or more categories

            /// <summary>
            /// Defines the Generic
            /// </summary>
            Generic
        }// Fallback generic templates

        // Template storage indexed by meal quality, then template type

        /// <summary>
        /// Defines the Templates
        /// </summary>
        private static readonly Dictionary<MealQuality, Dictionary<TemplateType, List<string>>> Templates =
            new Dictionary<MealQuality, Dictionary<TemplateType, List<string>>>();

        // Adjectives that can be inserted into templates

        /// <summary>
        /// Defines the CategoryAdjectives
        /// </summary>
        private static readonly Dictionary<IngredientCategory, List<string>> CategoryAdjectives =
            new Dictionary<IngredientCategory, List<string>>
            {
                {
                    IngredientCategory.Protein,
                    new List<string> { "tender", "juicy", "roasted", "grilled", "seared", "braised", "slow-cooked", "smoked" }
                },
                {
                    IngredientCategory.Carb,
                    new List<string> { "fluffy", "hearty", "steamed", "seasoned", "toasted", "filling", "starchy" }
                },
                {
                    IngredientCategory.Vegetable,
                    new List<string> { "fresh", "crisp", "garden", "sautéed", "roasted", "steamed", "grilled", "seasonal" }
                },
                {
                    IngredientCategory.Dairy,
                    new List<string> { "creamy", "rich", "buttery", "whipped", "smooth", "velvety", "milky" }
                },
                {
                    IngredientCategory.Fat,
                    new List<string> { "rich", "savory", "unctuous", "decadent", "luxurious", "silky" }
                },
                {
                    IngredientCategory.Sweetener,
                    new List<string> { "sweet", "sugary", "honeyed", "caramelized", "glazed", "syrupy" }
                },
                {
                    IngredientCategory.Flavoring,
                    new List<string> { "aromatic", "fragrant", "spiced", "herbed", "seasoned", "zesty", "savory" }
                },
                {
                    IngredientCategory.Other,
                    new List<string> { "mixed", "assorted", "varied", "miscellaneous", "diverse" }
                }
            };

        // Load all templates during static initialization

        /// <summary>
        /// Initializes static members of the <see cref="TemplateDishGenerator"/> class.
        /// </summary>
        static TemplateDishGenerator()
        {
            InitializeDefaultTemplates();
            LoadCustomTemplates();
        }

        /// <summary>
        /// Set up the default templates for each meal quality and template type
        /// </summary>
        private static void InitializeDefaultTemplates()
        {
            // Initialize the dictionary structure
            foreach (MealQuality quality in Enum.GetValues(typeof(MealQuality)))
            {
                Templates[quality] = new Dictionary<TemplateType, List<string>>();

                foreach (TemplateType type in Enum.GetValues(typeof(TemplateType)))
                {
                    Templates[quality][type] = new List<string>();
                }
            }

            // Simple meal templates
            // Single category templates
            Templates[MealQuality.Simple][TemplateType.SingleCategory] = new List<string>
            {
                "[protein] stew",
                "basic [protein] dish",
                "simple [protein] meal",
                "[carb] bowl",
                "plain [carb] dish",
                "simple [carb] meal",
                "[vegetable] medley",
                "simple [vegetable] plate",
                "mixed [vegetable] dish"
            };

            // Dual category templates
            Templates[MealQuality.Simple][TemplateType.DualCategory] = new List<string>
            {
                "[protein] with [carb]",
                "[protein] and [vegetable] plate",
                "[carb] with [vegetable]",
                "[vegetable] and [protein] dish",
                "simple [protein] [carb] meal",
                "basic [carb] and [vegetable] dish"
            };

            // Multi-category templates
            Templates[MealQuality.Simple][TemplateType.MultiCategory] = new List<string>
            {
                "mixed meal with [protein]",
                "simple stew with [vegetable]",
                "basic [protein] dish with sides",
                "plain meal with [carb]",
                "hodgepodge with [protein]"
            };

            // Generic templates
            Templates[MealQuality.Simple][TemplateType.Generic] = new List<string>
            {
                "simple meal",
                "basic dish",
                "plain food",
                "rustic platter",
                "settler's ration",
                "field meal",
                "modest serving"
            };

            // Fine meal templates
            // Single category templates
            Templates[MealQuality.Fine][TemplateType.SingleCategory] = new List<string>
            {
                "sautéed [protein]",
                "seasoned [protein] plate",
                "[carb] pilaf",
                "aromatic [carb] dish",
                "garden [vegetable] medley",
                "herbed [vegetable] plate"
            };

            // Dual category templates
            Templates[MealQuality.Fine][TemplateType.DualCategory] = new List<string>
            {
                "[protein] with [carb] side",
                "seared [protein] with [vegetable]",
                "[carb] pilaf with [protein]",
                "[vegetable] medley with [protein]",
                "[carb] and [vegetable] plate"
            };

            // Multi-category templates
            Templates[MealQuality.Fine][TemplateType.MultiCategory] = new List<string>
            {
                "[protein] dinner with accompaniments",
                "chef's [carb] with mixed sides",
                "savory [protein] plate with variety",
                "colonial [protein] feast",
                "[carb] mixed plate"
            };

            // Generic templates
            Templates[MealQuality.Fine][TemplateType.Generic] = new List<string>
            {
                "fine meal",
                "quality platter",
                "chef's selection",
                "diner's choice",
                "table special",
                "homestead favorite"
            };

            // Lavish meal templates
            // Single category templates
            Templates[MealQuality.Lavish][TemplateType.SingleCategory] = new List<string>
            {
                "gourmet [protein] entrée",
                "chef's [protein] special",
                "luxury [carb] feast",
                "deluxe [carb] creation",
                "premium [vegetable] platter",
                "gourmet [vegetable] arrangement"
            };

            // Dual category templates
            Templates[MealQuality.Lavish][TemplateType.DualCategory] = new List<string>
            {
                "prime [protein] with [carb] accompaniment",
                "gourmet [protein] atop [vegetable] medley",
                "luxury [carb] garnished with [protein]",
                "chef's [vegetable] with [protein] crown",
                "exquisite [protein] and [carb] dish"
            };

            // Multi-category templates
            Templates[MealQuality.Lavish][TemplateType.MultiCategory] = new List<string>
            {
                "[protein] feast with all the trimmings",
                "gourmet [protein] dinner with sides",
                "deluxe [carb] platter with assortments",
                "luxury sampler featuring [protein]",
                "chef's masterpiece with [protein]"
            };

            // Generic templates
            Templates[MealQuality.Lavish][TemplateType.Generic] = new List<string>
            {
                "lavish feast",
                "gourmet meal",
                "luxury platter",
                "exquisite dining experience",
                "chef's masterpiece",
                "rimworld delicacy",
                "premium selection"
            };
        }

        /// <summary>
        /// Load custom templates from XML file if available
        /// </summary>
        private static void LoadCustomTemplates()
        {
            try
            {
                // Get mod directory
                string modDir = GetModDirectory();
                if (string.IsNullOrEmpty(modDir))
                {
                    Log.Error("[CustomFoodNames] Could not locate mod directory for custom templates");
                    return;
                }

                string templateFile = Path.Combine(modDir, "Database", "DishTemplates.xml");
                if (!File.Exists(templateFile))
                {
                    // Create default template file if it doesn't exist
                    CreateDefaultTemplateFile(templateFile);
                    return;
                }

                // Load the XML templates
                XDocument doc = XDocument.Parse(File.ReadAllText(templateFile));

                foreach (var qualityElement in doc.Root.Elements("MealQuality"))
                {
                    string qualityStr = qualityElement.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(qualityStr))
                        continue;

                    // Try to parse the meal quality
                    if (!Enum.TryParse<MealQuality>(qualityStr, true, out var quality))
                        continue;

                    // Process each template group
                    foreach (var groupElement in qualityElement.Elements("TemplateGroup"))
                    {
                        string typeStr = groupElement.Attribute("type")?.Value;
                        if (string.IsNullOrEmpty(typeStr))
                            continue;

                        // Try to parse the template type
                        if (!Enum.TryParse<TemplateType>(typeStr, true, out var type))
                            continue;

                        // Get all templates in this group
                        var templateList = new List<string>();
                        foreach (var templateElement in groupElement.Elements("Template"))
                        {
                            string template = templateElement.Value?.Trim();
                            if (!string.IsNullOrEmpty(template))
                            {
                                templateList.Add(template);
                            }
                        }

                        // Replace the default templates if we have custom ones
                        if (templateList.Count > 0)
                        {
                            Templates[quality][type] = templateList;
                        }
                    }
                }

                Log.Message("[CustomFoodNames] Loaded custom dish templates");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error loading custom templates: {ex}");
            }
        }

        /// <summary>
        /// Create a default template file with examples
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        private static void CreateDefaultTemplateFile(string filePath)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Create a basic XML template
                string template = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<DishTemplates>
    <!-- Define templates for each meal quality and type -->
    <!-- Template placeholders:
         [protein] - Will be replaced with protein ingredient (meat, etc.)
         [carb] - Will be replaced with carb ingredient (rice, potatoes, etc.)
         [vegetable] - Will be replaced with vegetable ingredient
         [dairy] - Will be replaced with dairy ingredient
         [fat] - Will be replaced with fat ingredient
         [sweetener] - Will be replaced with sweetener ingredient
         [flavoring] - Will be replaced with flavoring ingredient
         [exotic] - Will be replaced with exotic ingredient
    -->
    
    <!-- Simple Meal Templates -->
    <MealQuality name=""Simple"">
        <!-- Single category meals (all vegetables, all meat, etc.) -->
        <TemplateGroup type=""SingleCategory"">
            <Template>[protein] stew</Template>
            <Template>basic [protein] dish</Template>
            <Template>simple [protein] meal</Template>
            <Template>[carb] bowl</Template>
            <Template>plain [carb] dish</Template>
            <Template>simple [carb] meal</Template>
            <Template>[vegetable] medley</Template>
            <Template>simple [vegetable] plate</Template>
            <Template>mixed [vegetable] dish</Template>
        </TemplateGroup>
        
        <!-- Two categories of ingredients -->
        <TemplateGroup type=""DualCategory"">
            <Template>[protein] with [carb]</Template>
            <Template>[protein] and [vegetable] plate</Template>
            <Template>[carb] with [vegetable]</Template>
            <Template>[vegetable] and [protein] dish</Template>
            <Template>simple [protein] [carb] meal</Template>
            <Template>basic [carb] and [vegetable] dish</Template>
        </TemplateGroup>
        
        <!-- Three or more categories of ingredients -->
        <TemplateGroup type=""MultiCategory"">
            <Template>mixed meal with [protein]</Template>
            <Template>simple stew with [vegetable]</Template>
            <Template>basic [protein] dish with sides</Template>
            <Template>plain meal with [carb]</Template>
            <Template>hodgepodge with [protein]</Template>
        </TemplateGroup>
        
        <!-- Generic fallback templates -->
        <TemplateGroup type=""Generic"">
            <Template>simple meal</Template>
            <Template>basic dish</Template>
            <Template>plain food</Template>
            <Template>rustic platter</Template>
            <Template>settler's ration</Template>
            <Template>field meal</Template>
            <Template>modest serving</Template>
        </TemplateGroup>
    </MealQuality>
    
    <!-- Add more meal qualities here with their templates -->
</DishTemplates>";

                File.WriteAllText(filePath, template);
                Log.Message($"[CustomFoodNames] Created default dish templates at {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CustomFoodNames] Error creating default template file: {ex}");
            }
        }

        /// <summary>
        /// Generate a dish name based on ingredients and meal quality using templates
        /// </summary>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="mealDef">The mealDef<see cref="ThingDef"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GenerateDishName(List<ThingDef> ingredients, ThingDef mealDef)
        {
            // Sanity check
            if (ingredients == null || ingredients.Count == 0)
                return "Mystery Dish";

            // Determine meal quality from the def name
            MealQuality quality = DetermineMealQuality(mealDef);

            // First try the database for specific combinations
            string databaseName = DishNameDatabase.GetDishNameForIngredients(ingredients, quality.ToString());
            if (!string.IsNullOrEmpty(databaseName))
                return databaseName;

            // Get all ingredient categories present in the meal
            var categories = GetAllCategories(ingredients);

            // Determine template type based on category diversity
            TemplateType templateType = DetermineTemplateType(categories);

            // Select a template
            string template = SelectTemplate(quality, templateType);

            // Fill the template with ingredient names
            return FillTemplate(template, ingredients, categories);
        }

        /// <summary>
        /// Determine the quality level of a meal
        /// </summary>
        /// <param name="mealDef">The mealDef<see cref="ThingDef"/></param>
        /// <returns>The <see cref="MealQuality"/></returns>
        private static MealQuality DetermineMealQuality(ThingDef mealDef)
        {
            if (mealDef == null)
                return MealQuality.Simple;

            string defName = mealDef.defName;

            if (defName.Contains("Lavish"))
                return MealQuality.Lavish;
            else if (defName.Contains("Fine"))
                return MealQuality.Fine;
            else
                return MealQuality.Simple;
        }

        /// <summary>
        /// Determine the appropriate template type based on ingredient categories
        /// </summary>
        /// <param name="categories">The categories<see cref="List{IngredientCategory}"/></param>
        /// <returns>The <see cref="TemplateType"/></returns>
        private static TemplateType DetermineTemplateType(List<IngredientCategory> categories)
        {
            if (categories == null || categories.Count == 0)
                return TemplateType.Generic;

            int uniqueCategories = categories.Count;

            if (uniqueCategories == 1)
                return TemplateType.SingleCategory;
            else if (uniqueCategories == 2)
                return TemplateType.DualCategory;
            else if (uniqueCategories >= 3)
                return TemplateType.MultiCategory;

            return TemplateType.Generic;
        }

        /// <summary>
        /// Select an appropriate template based on meal quality and composition
        /// </summary>
        /// <param name="quality">The quality<see cref="MealQuality"/></param>
        /// <param name="type">The type<see cref="TemplateType"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string SelectTemplate(MealQuality quality, TemplateType type)
        {
            // Get templates for this quality and type
            var templateList = Templates[quality][type];

            // If no templates available, fall back to generic
            if (templateList.Count == 0)
                templateList = Templates[quality][TemplateType.Generic];

            // If still no templates, use a fixed fallback
            if (templateList.Count == 0)
                return "Meal";

            // Select a random template
            return templateList.RandomElement();
        }

        /// <summary>
        /// Fill a template with actual ingredient names
        /// </summary>
        /// <param name="template">The template<see cref="string"/></param>
        /// <param name="ingredients">The ingredients<see cref="List{ThingDef}"/></param>
        /// <param name="categories">The categories<see cref="List{IngredientCategory}"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string FillTemplate(string template, List<ThingDef> ingredients, List<IngredientCategory> categories)
        {
            // Create a working copy of the template
            string result = template;

            // Process each category placeholder in the template
            foreach (IngredientCategory category in Enum.GetValues(typeof(IngredientCategory)))
            {
                string placeholder = $"[{category.ToString().ToLowerInvariant()}]";

                // Check if this placeholder exists in the template
                if (result.Contains(placeholder))
                {
                    // Get ingredients of this category
                    var categoryIngredients = GetIngredientsOfCategory(ingredients, category);

                    // If we have ingredients of this category, use them
                    if (categoryIngredients.Count > 0)
                    {
                        // Select a representative ingredient
                        string ingredientName = GetCleanIngredientName(categoryIngredients.RandomElement());

                        // Add an adjective sometimes (50% chance)
                        if (Rand.Value < 0.5f && CategoryAdjectives.TryGetValue(category, out var adjectives))
                        {
                            ingredientName = $"{adjectives.RandomElement()} {ingredientName}";
                        }

                        // Replace the placeholder
                        result = result.Replace(placeholder, ingredientName);
                    }
                    else
                    {
                        // No ingredients of this category, use something generic
                        result = result.Replace(placeholder, GetGenericCategoryName(category));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clean up an ingredient name for display in a dish name
        /// </summary>
        /// <param name="ingredient">The ingredient<see cref="ThingDef"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetCleanIngredientName(ThingDef ingredient)
        {
            if (ingredient == null)
                return "mystery";

            // Get the base label
            string label = ingredient.label;

            // Remove "raw" prefix
            string cleaned = Regex.Replace(label, @"^raw\s+", "", RegexOptions.IgnoreCase);

            // Remove specific qualifiers
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Replace(" meat", "");

            cleaned = cleaned.Trim();

            // Capitalize first letter
            if (cleaned.Length > 0)
            {
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }

        /// <summary>
        /// Get a generic name for a category when no ingredients are available
        /// </summary>
        /// <param name="category">The category<see cref="IngredientCategory"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetGenericCategoryName(IngredientCategory category)
        {
            switch (category)
            {
                case IngredientCategory.Protein:
                    return "Protein";
                case IngredientCategory.Carb:
                    return "Grains";
                case IngredientCategory.Vegetable:
                    return "Vegetables";
                case IngredientCategory.Dairy:
                    return "Dairy";
                case IngredientCategory.Fat:
                    return "Fat";
                case IngredientCategory.Sweetener:
                    return "Sweetener";
                case IngredientCategory.Flavoring:
                    return "Herbs";
                case IngredientCategory.Exotic:
                    return "Exotic";
                case IngredientCategory.Other:
                default:
                    return "Ingredients";
            }
        }

        /// <summary>
        /// Helper method to get the mod directory
        /// </summary>
        /// <returns>The <see cref="string"/></returns>
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
