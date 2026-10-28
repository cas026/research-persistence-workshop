using System;

/// <summary>
/// Small summary of a save slot, stored in its own file next to the save itself,
/// so the slot overview doesn't have to load the (possibly large) save data.
/// </summary>
[Serializable]
public class SlotMetadata
{
    public int saveVersion;
    // JsonUtility can't serialize DateTime, so the save time is stored as ticks
    public long savedAtTicks;
    public float playTimeSeconds;
    public string sceneName;
    public string playerName;
    public int level;

    public override string ToString()
    {
        return "SlotMetadata: v" + saveVersion + ", " + new DateTime(savedAtTicks) + ", " + sceneName +
               ", " + playTimeSeconds + "s, " + playerName + " (level " + level + ")";
    }
}
