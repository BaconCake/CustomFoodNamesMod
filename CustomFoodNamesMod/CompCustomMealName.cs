using System;
using Verse;

namespace CustomFoodNamesMod
{
    public class CompCustomMealName : ThingComp
    {
        private string assignedDishName = "";
        private string cookName = "";

        public string AssignedDishName
        {
            get => assignedDishName ?? "";
            set => assignedDishName = value ?? "";
        }

        public string CookName
        {
            get => cookName ?? "";
            set => cookName = value ?? "";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // Use SaveLoad methods with explicit default strings
            // This ensures the values are properly saved to the save file
            Scribe_Values.Look(ref assignedDishName, "assignedDishName", "");
            Scribe_Values.Look(ref cookName, "cookName", "");

            // Add debug output to track loading/saving
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Log.Message($"[CustomFoodNames] Loading meal data - DishName: '{assignedDishName}', Cook: '{cookName}'");
            }
            else if (Scribe.mode == LoadSaveMode.Saving)
            {
                Log.Message($"[CustomFoodNames] Saving meal data - DishName: '{assignedDishName}', Cook: '{cookName}'");
            }
        }

        // Add methods to explicitly set and get values for direct access
        public override string CompInspectStringExtra()
        {
            if (!string.IsNullOrEmpty(cookName))
            {
                return $"Prepared by: {cookName}";
            }
            return base.CompInspectStringExtra();
        }

        // Add a PostMake method to ensure comp is properly initialized
        public override void PostPostMake()
        {
            base.PostPostMake();

            // Ensure fields are initialized
            if (assignedDishName == null) assignedDishName = "";
            if (cookName == null) cookName = "";
        }
    }
}