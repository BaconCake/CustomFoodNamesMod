namespace CustomFoodNamesMod.Database
{
    /// <summary>
    /// Contains dish information (name and description)
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