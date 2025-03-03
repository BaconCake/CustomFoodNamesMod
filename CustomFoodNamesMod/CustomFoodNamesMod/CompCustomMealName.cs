using Verse;

namespace CustomFoodNamesMod
{
    public class CompCustomMealName : ThingComp
    {
        private string assignedDishName;

        /// <summary>
        /// The dish name that we want to store once it's generated.
        /// </summary>
        public string AssignedDishName
        {
            get => assignedDishName;
            set => assignedDishName = value;
        }

        /// <summary>
        /// This is how RimWorld saves/loads data for items.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref assignedDishName, "assignedDishName", null);
        }
    }
}
