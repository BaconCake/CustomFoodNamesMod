using Verse;

public class CompCustomMealName : ThingComp
{
    private string assignedDishName;
    private string cookName; // Make sure this field exists

    public string AssignedDishName
    {
        get => assignedDishName;
        set => assignedDishName = value;
    }

    public string CookName
    {
        get => cookName;
        set => cookName = value;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref assignedDishName, "assignedDishName", null);
        Scribe_Values.Look(ref cookName, "cookName", null); // Make sure this is here
    }
}