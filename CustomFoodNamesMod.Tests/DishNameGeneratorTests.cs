using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CustomFoodNamesMod.Tests
{
    [TestFixture]
    public class DishNameGeneratorTests
    {
        // Mock classes to replace RimWorld dependencies
        public class MockThingDef
        {
            public string defName;
            public string label;
            public bool IsIngestible = true;

            public MockThingDef(string defName, string label)
            {
                this.defName = defName;
                this.label = label;
            }
        }

        // Store our mock objects
        private MockThingDef mockSimpleMeal;
        private MockThingDef mockFineMeal;
        private MockThingDef mockLavishMeal;
        private MockThingDef mockNutrientPaste;
        private Dictionary<string, MockThingDef> mockIngredients;

        [SetUp]
        public void Setup()
        {
            // Create mock meal defs
            mockSimpleMeal = new MockThingDef("MealSimple", "Simple Meal");
            mockFineMeal = new MockThingDef("MealFine", "Fine Meal");
            mockLavishMeal = new MockThingDef("MealLavish", "Lavish Meal");
            mockNutrientPaste = new MockThingDef("MealNutrientPaste", "Nutrient Paste Meal");

            // Create mock ingredient defs
            mockIngredients = new Dictionary<string, MockThingDef>
            {
                // Meats
                { "RawBeef", new MockThingDef("Meat_Cow", "Raw beef") },
                { "RawPork", new MockThingDef("Meat_Pig", "Raw pork") },
                { "RawChicken", new MockThingDef("Meat_Chicken", "Raw chicken") },
                { "HumanMeat", new MockThingDef("Meat_Human", "Raw human meat") },
                { "ThrumboMeat", new MockThingDef("Meat_Thrumbo", "Raw thrumbo meat") },
                { "InsectMeat", new MockThingDef("Meat_Megaspider", "Raw insect meat") },

                // Vegetables
                { "RawPotatoes", new MockThingDef("RawPotatoes", "Raw potatoes") },
                { "RawFungus", new MockThingDef("RawFungus", "Raw fungus") },
                { "RawAgave", new MockThingDef("RawAgave", "Raw agave") },

                // Grains
                { "RawRice", new MockThingDef("RawRice", "Raw rice") },
                { "RawCorn", new MockThingDef("RawCorn", "Raw corn") },

                // Other
                { "RawBerries", new MockThingDef("RawBerries", "Raw berries") },
                { "Milk", new MockThingDef("Milk", "Milk") },
                { "InsectJelly", new MockThingDef("InsectJelly", "Insect jelly") },
                { "EggChicken", new MockThingDef("EggChickenUnfertilized", "Chicken egg (unfert.)") }
            };
        }

        [Test]
        public void TestSingleIngredients()
        {
            // Test single ingredient meals with different qualities
            TestSingleIngredient("RawPotatoes", mockSimpleMeal, "potato");
            TestSingleIngredient("RawPotatoes", mockFineMeal, "potato");
            TestSingleIngredient("RawPotatoes", mockLavishMeal, "potato");

            TestSingleIngredient("RawRice", mockSimpleMeal, "rice");
            TestSingleIngredient("RawBeef", mockSimpleMeal, "beef");

            // Test that human meat is treated normally
            TestSingleIngredient("HumanMeat", mockSimpleMeal, "human");
            TestSingleIngredient("ThrumboMeat", mockSimpleMeal, "thrumbo");
        }

        [Test]
        public void TestTwoIngredients()
        {
            // Test two-ingredient meals with different qualities
            TestTwoIngredients("RawPotatoes", "RawFungus", mockSimpleMeal);
            TestTwoIngredients("RawPotatoes", "RawFungus", mockFineMeal);
            TestTwoIngredients("RawPotatoes", "RawFungus", mockLavishMeal);

            // Test combinations with human meat
            TestTwoIngredients("HumanMeat", "RawPotatoes", mockSimpleMeal);
            TestTwoIngredients("RawBeef", "RawRice", mockSimpleMeal);
            TestTwoIngredients("HumanMeat", "RawRice", mockFineMeal);
        }

        [Test]
        public void TestComplexMeals()
        {
            // Test more complex meals with multiple ingredients
            TestComplexMeal(new[] { "RawBeef", "RawPotatoes", "RawFungus" }, mockLavishMeal);
            TestComplexMeal(new[] { "HumanMeat", "RawRice", "RawCorn", "Milk" }, mockLavishMeal);
        }

        [Test]
        public void TestNutrientPaste()
        {
            // Test nutrient paste meals
            TestNutrientPaste(new[] { "RawPotatoes", "RawFungus" });
            TestNutrientPaste(new[] { "HumanMeat", "RawRice" });
        }

        // Helper methods for testing
        private void TestSingleIngredient(string ingredientKey, MockThingDef mealDef, string expectedSubstring)
        {
            var ingredients = new List<MockThingDef> { mockIngredients[ingredientKey] };
            string dishName = DishNameGeneratorAdapter.GenerateDishName(ingredients, mealDef);

            Console.WriteLine($"Single ingredient meal with {ingredientKey}: {dishName}");

            // Check that the dish name contains the ingredient name
            Assert.That(dishName.ToLower().Contains(expectedSubstring),
                     $"Dish name should contain {expectedSubstring}");

            // Make sure it's not empty and has a reasonable length
            Assert.That(dishName, Is.Not.Null.Or.Empty);
            Assert.That(dishName.Length, Is.GreaterThan(5));
        }

        private void TestTwoIngredients(string ingredient1Key, string ingredient2Key, MockThingDef mealDef)
        {
            var ingredients = new List<MockThingDef>
            {
                mockIngredients[ingredient1Key],
                mockIngredients[ingredient2Key]
            };

            string dishName = DishNameGeneratorAdapter.GenerateDishName(ingredients, mealDef);

            Console.WriteLine($"Two ingredient meal with {ingredient1Key} and {ingredient2Key}: {dishName}");

            // The meal should mention at least one of the ingredients
            string cleanName1 = CleanIngredientName(mockIngredients[ingredient1Key].label);
            string cleanName2 = CleanIngredientName(mockIngredients[ingredient2Key].label);

            bool containsIngredient = dishName.ToLower().Contains(cleanName1.ToLower()) ||
                                    dishName.ToLower().Contains(cleanName2.ToLower());

            Assert.That(containsIngredient,
                      $"Dish name should contain at least one of the ingredients: {cleanName1} or {cleanName2}");

            // Make sure it's a reasonable length
            Assert.That(dishName.Length, Is.GreaterThan(5));
        }

        private void TestComplexMeal(string[] ingredientKeys, MockThingDef mealDef)
        {
            var ingredients = ingredientKeys.Select(key => mockIngredients[key]).ToList();

            string dishName = DishNameGeneratorAdapter.GenerateDishName(ingredients, mealDef);

            Console.WriteLine($"Complex meal with {string.Join(", ", ingredientKeys)}: {dishName}");

            // Make sure we got a dish name
            Assert.That(dishName, Is.Not.Null.Or.Empty);
            Assert.That(dishName.Length, Is.GreaterThan(5));

            // It should contain at least one of the ingredient names
            bool containsAnyIngredient = ingredientKeys.Any(key => {
                string cleanName = CleanIngredientName(mockIngredients[key].label);
                return dishName.ToLower().Contains(cleanName.ToLower());
            });

            Assert.That(containsAnyIngredient, "Dish name should mention at least one ingredient");
        }

        private void TestNutrientPaste(string[] ingredientKeys)
        {
            var ingredients = ingredientKeys.Select(key => mockIngredients[key]).ToList();

            string dishName = DishNameGeneratorAdapter.GenerateNutrientPasteName(ingredients);

            Console.WriteLine($"Nutrient paste with {string.Join(", ", ingredientKeys)}: {dishName}");

            // Make sure we got a paste name
            Assert.That(dishName, Is.Not.Null.Or.Empty);
            Assert.That(dishName.Length, Is.GreaterThan(5));

            // Should contain a paste-like term
            string[] pasteTerms = { "paste", "sludge", "slop", "goo", "muck", "glop", "mash", "pulp", "gruel", "slurry" };
            bool containsPasteTerm = pasteTerms.Any(term => dishName.ToLower().Contains(term));

            Assert.That(containsPasteTerm, "Nutrient paste name should contain a paste-like term");
        }

        private string CleanIngredientName(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "mystery";

            // Remove "raw" prefix
            string cleanLabel = System.Text.RegularExpressions.Regex.Replace(
                label,
                @"^raw\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove specific terms
            cleanLabel = cleanLabel.Replace(" (unfert.)", "");
            cleanLabel = cleanLabel.Replace(" (fert.)", "");
            cleanLabel = cleanLabel.Replace(" meat", "");

            return cleanLabel.Trim();
        }
    }
}