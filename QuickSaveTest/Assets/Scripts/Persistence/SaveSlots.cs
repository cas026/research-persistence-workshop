public static class SaveSlots
{
    public const string Auto = "auto";
    public const string Quick = "quick";

    public static readonly string[] Manual = { "slot1", "slot2", "slot3" };
    public static readonly string[] All = { Auto, Quick, "slot1", "slot2", "slot3" };

    public static string DisplayName(string slot)
    {
        switch (slot)
        {
            case Auto: return "Autosave";
            case Quick: return "Quicksave";
            case "slot1": return "Slot 1";
            case "slot2": return "Slot 2";
            case "slot3": return "Slot 3";
            default: return slot;
        }
    }
}
