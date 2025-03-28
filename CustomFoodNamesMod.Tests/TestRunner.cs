using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace CustomFoodNamesMod.Tests
{
    /// <summary>
    /// A simple test runner that can be used to run the dish name generation tests
    /// without having to launch RimWorld.
    /// </summary>
    public static class TestRunner
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Running DishNameGenerator Tests ===");
            Console.WriteLine();

            // Create test fixture instance
            var testFixture = new DishNameGeneratorTests();

            // Get all test methods using reflection
            var testMethods = typeof(DishNameGeneratorTests).GetMethods(
                BindingFlags.Public | BindingFlags.Instance);

            int passed = 0;
            int failed = 0;
            List<string> failedTests = new List<string>();

            foreach (var method in testMethods)
            {
                // Check if this is a test method
                if (method.GetCustomAttribute<TestAttribute>() != null)
                {
                    Console.WriteLine($"Running test: {method.Name}");

                    try
                    {
                        // Call the setup method before each test
                        typeof(DishNameGeneratorTests)
                            .GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance)
                            .Invoke(testFixture, null);

                        // Execute the test method
                        method.Invoke(testFixture, null);
                        Console.WriteLine($"PASSED: {method.Name}");
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions from failed tests
                        if (ex is TargetInvocationException && ex.InnerException != null)
                        {
                            Console.WriteLine($"FAILED: {method.Name}");
                            Console.WriteLine($"Error: {ex.InnerException.Message}");
                            failedTests.Add($"{method.Name}: {ex.InnerException.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"FAILED: {method.Name}");
                            Console.WriteLine($"Error: {ex.Message}");
                            failedTests.Add($"{method.Name}: {ex.Message}");
                        }
                        failed++;
                    }

                    Console.WriteLine();
                }
            }

            // Print summary
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Total tests: {passed + failed}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");

            if (failed > 0)
            {
                Console.WriteLine("\nFailed tests:");
                foreach (var test in failedTests)
                {
                    Console.WriteLine($"- {test}");
                }
            }
        }

        // Call this method from your main code or a debug function to run the tests
        public static void Main()
        {
            RunTests();
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}