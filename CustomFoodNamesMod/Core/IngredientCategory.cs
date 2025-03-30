namespace CustomFoodNamesMod.Core
{
    /// <summary>
    /// Unified enum for ingredient categories across the mod
    /// </summary>
    public enum IngredientCategory
    {
        Meat,       // All animal meats, insects, etc.
        Vegetable,  // Plant-based ingredients that aren't grains
        Grain,      // Rice, corn, etc.
        Egg,        // All types of eggs
        Dairy,      // Milk and milk products
        Fruit,      // Berries and fruits
        Fungus,     // Mushrooms and fungi
        Special,    // Ingredients with special naming importance
        Other       // Default category for uncategorized items
    }
}