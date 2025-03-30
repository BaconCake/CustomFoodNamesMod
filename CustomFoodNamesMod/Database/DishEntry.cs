namespace CustomFoodNamesMod.Database
{
    /// <summary>
    /// Represents a dish entry in the database
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
}