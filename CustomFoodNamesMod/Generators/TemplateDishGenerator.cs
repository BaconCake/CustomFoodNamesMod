using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Verse;
using CustomFoodNamesMod.Core;
using CustomFoodNamesMod.Utils;

namespace CustomFoodNamesMod.Generators
{
    /// <summary>
    /// Generates meal names using templates based on ingredient categories
    /// </summary>
    public class TemplateDishGenerator : NameGeneratorBase
    {
        // Types of templates for different meal compositions
        private enum TemplateType
        {
            SingleCategory,    // Only one ingredient category (e.g. all vegetables)
            DualCategory,      // Two ingredient categories (e.g. protein + carb)
            MultiCategory,     // Three or more categories
            Generic            // Fallback generic templates
        }

        // Template storage indexed by meal quality, then template type
        private static readonly Dictionary<MealQuality, Dictionary<TemplateType, List<string>>> Templates =
            new Dictionary<MealQuality, Dictionary<TemplateType, List<string>>>();

        // Adjectives that can be inserted into templates
        private static readonly Dictionary<IngredientCategory, List<string>> CategoryAdjectives =
            new Dictionary<IngredientCategory, List<string>>
            {
                {
                    IngredientCategory.Meat,
                    new List<string> { "tender", "juicy", "roasted", "grilled", "seared", "braised", "slow-cooked", "smoked" }
                },
                {
                    IngredientCategory.Grain,
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
                    IngredientCategory.Egg,
                    new List<string> { "fluffy", "light", "airy", "golden", "delicate", "rich", "savory" }
                },
                {
                    IngredientCategory.Fruit,
                    new List<string> { "sweet", "tangy", "ripe", "juicy", "fresh", "zesty", "vibrant" }
                },
                {
                    IngredientCategory.Fungus,
                    new List<string> { "earthy", "aromatic", "hearty", "wild", "rustic", "umami", "rich" }
                },
                {
                    IngredientCategory.Special,
                    new List<string> { "special", "exotic", "unique", "choice", "premium", "rare", "selected" }
                },
                {
                    IngredientCategory.Other,
                    new List<string> { "mixed", "assorted", "varied", "miscellaneous", "diverse" }
                }
            };

        // Load all templates during static initialization
        static TemplateDishGenerator()
        {
            InitializeDefaultTemplates();
            LoadCustomTemplates();
        }

        /// <summary>
        /// Generate a dish name based on ingredients and meal definition
        /// </summary>
        public override string GenerateName(List<ThingDef> ingredients, ThingDef mealDef)
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
            var categories = IngredientCategorizer.GetAllCategories(ingredients);

            // Determine template type based on category diversity
            TemplateType templateType = DetermineTemplateType(categories);

            // Select a template
            string template = SelectTemplate(quality, templateType);

            // Fill the template with ingredient names
            return FillTemplate(template, ingredients, categories);
        }

        /// <summary>
        /// Generate a description for the meal
        /// </summary>
        public override string GenerateDescription(List<ThingDef> ingredients, ThingDef mealDef)
        {
            if (ingredients == null || ingredients.Count == 0)
                return "A mysterious meal with unknown ingredients.";

            // Determine meal quality
            MealQuality mealQuality = DetermineMealQuality(mealDef);

            // Get ingredient list for description
            string ingredientList = IngredientUtils.FormatIngredientsList(ingredients);

            // Build appropriate description based on meal quality
            switch (mealQuality)
            {
                case MealQuality.Lavish:
                    return $"A lavishly prepared dish containing {ingredientList}. It has been expertly crafted to be both nutritious and delicious.";

                case MealQuality.Fine:
                    return $"A well-prepared dish containing {ingredientList}. It has been skillfully made to balance nutrition and taste.";

                default: // Simple
                    return $"A basic dish containing {ingredientList}. It offers good nutrition although the taste is simple.";
            }
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
                "[grain] bowl",
                "plain [grain] dish",
                "simple [grain] meal",
                "[vegetable] medley",
                "simple [vegetable] plate",
                "mixed [vegetable] dish"
            };

            // Dual category templates
            Templates[MealQuality.Simple][TemplateType.DualCategory] = new List<string>
            {
                "[protein] with [grain]",
                "[protein] and [vegetable] plate",
                "[grain] with [vegetable]",
                "[vegetable] and [protein] dish",
                "simple [protein] [grain] meal",
                "basic [grain] and [vegetable] dish"
            };

            // Multi-category templates
            Templates[MealQuality.Simple][TemplateType.MultiCategory] = new List<string>
            {
                "mixed meal with [protein]",
                "simple stew with [vegetable]",
                "basic [protein] dish with sides",
                "plain meal with [grain]",
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
                "[grain] pilaf",
                "aromatic [grain] dish",
                "garden [vegetable] medley",
                "herbed [vegetable] plate"
            };

            // Dual category templates
            Templates[MealQuality.Fine][TemplateType.DualCategory] = new List<string>
            {
                "[protein] with [grain] side",
                "seared [protein] with [vegetable]",
                "[grain] pilaf with [protein]",
                "[vegetable] medley with [protein]",
                "[grain] and [vegetable] plate"
            };

            // Multi-category templates
            Templates[MealQuality.Fine][TemplateType.MultiCategory] = new List<string>
            {
                "[protein] dinner with accompaniments",
                "chef's [grain] with mixed sides",
                "savory [protein] plate with variety",
                "colonial [protein] feast",
                "[grain] mixed plate"
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
                "luxury [grain] feast",
                "deluxe [grain] creation",
                "premium [vegetable] platter",
                "gourmet [vegetable] arrangement"
            };

            // Dual category templates
            Templates[MealQuality.Lavish][TemplateType.DualCategory] = new List<string>
            {
                "prime [protein] with [grain] accompaniment",
                "gourmet [protein] atop [vegetable] medley",
                "luxury [grain] garnished with [protein]",
                "chef's [vegetable] with [protein] crown",
                "exquisite [protein] and [grain] dish"
            };

            // Multi-category templates
            Templates[MealQuality.Lavish][TemplateType.MultiCategory] = new List<string>
            {
                "[protein] feast with all the trimmings",
                "gourmet [protein] dinner with sides",
                "deluxe [grain] platter with assortments",
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
         [grain] - Will be replaced with grain ingredient (rice, potatoes, etc.)
         [vegetable] - Will be replaced with vegetable ingredient
         [dairy] - Will be replaced with dairy ingredient
         [egg] - Will be replaced with egg ingredient
         [fruit] - Will be replaced with fruit ingredient
         [fungus] - Will be replaced with fungus ingredient
    -->
    
    <!-- Simple Meal Templates -->
    <MealQuality name=""Simple"">
        <!-- Single category meals (all vegetables, all meat, etc.) -->
        <TemplateGroup type=""SingleCategory"">
            <Template>[protein] stew</Template>
            <Template>basic [protein] dish</Template>
            <Template>simple [protein] meal</Template>
            <Template>[grain] bowl</Template>
            <Template>plain [grain] dish</Template>
            <Template>simple [grain] meal</Template>
            <Template>[vegetable] medley</Template>
            <Template>simple [vegetable] plate</Template>
            <Template>mixed [vegetable] dish</Template>
        </TemplateGroup>
        
        <!-- Two categories of ingredients -->
        <TemplateGroup type=""DualCategory"">
            <Template>[protein] with [grain]</Template>
            <Template>[protein] and [vegetable] plate</Template>
            <Template>[grain] with [vegetable]</Template>
            <Template>[vegetable] and [protein] dish</Template>
            <Template>simple [protein] [grain] meal</Template>
            <Template>basic [grain] and [vegetable] dish</Template>
        </TemplateGroup>
        
        <!-- Three or more categories of ingredients -->
        <TemplateGroup type=""MultiCategory"">
            <Template>mixed meal with [protein]</Template>
            <Template>simple stew with [vegetable]</Template>
            <Template>basic [protein] dish with sides</Template>
            <Template>plain meal with [grain]</Template>
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
        /// Determine the appropriate template type based on ingredient categories
        /// </summary>
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
                    var categoryIngredients = IngredientCategorizer.GetIngredientsOfCategory(ingredients, category);

                    // If we have ingredients of this category, use them
                    if (categoryIngredients.Count > 0)
                    {
                        // Select a representative ingredient
                        string ingredientName = StringUtils.GetCapitalizedLabel(categoryIngredients.RandomElement().label);

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
        /// Get a generic name for a category when no ingredients are available
        /// </summary>
        private static string GetGenericCategoryName(IngredientCategory category)
        {
            switch (category)
            {
                case IngredientCategory.Meat:
                    return "Protein";
                case IngredientCategory.Grain:
                    return "Grains";
                case IngredientCategory.Vegetable:
                    return "Vegetables";
                case IngredientCategory.Dairy:
                    return "Dairy";
                case IngredientCategory.Egg:
                    return "Eggs";
                case IngredientCategory.Fruit:
                    return "Fruits";
                case IngredientCategory.Fungus:
                    return "Fungi";
                case IngredientCategory.Special:
                    return "Special";
                case IngredientCategory.Other:
                default:
                    return "Ingredients";
            }
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