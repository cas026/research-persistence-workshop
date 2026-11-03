using System;
using CI.QuickSave;
using UnityEngine;

public static class SaveSystem
{
    private const string GameKey = "game";
    private const string SummaryKey = "summary";

    private static QuickSaveSettings Settings => new QuickSaveSettings();

    private static string RootFor(string slot) => "save_" + slot;

    public static bool Exists(string slot)
    {
        return QuickSaveWriter.RootExists(RootFor(slot));
    }

    public static void Delete(string slot)
    {
        QuickSaveWriter.DeleteRoot(RootFor(slot));
    }

    public static bool Save(string slot, SaveGame game)
    {
        SlotSummary summary = new SlotSummary
        {
            PlayerName = game.Player.Name,
            Coins = game.Player.Coins,
            SavedAt = DateTime.Now
        };

        try
        {
            QuickSaveWriter.Create(RootFor(slot), Settings)
                           .Write(GameKey, game)
                           .Write(SummaryKey, summary)
                           .Commit();

            return true;
        }
        catch (QuickSaveException e)
        {
            Debug.LogError($"Saving to {slot} failed: {e.Message}");
            return false;
        }
    }

    public static SaveGame Load(string slot)
    {
        if (!Exists(slot))
        {
            return null;
        }

        try
        {
            return QuickSaveReader.Create(RootFor(slot), Settings).Read<SaveGame>(GameKey);
        }
        catch (QuickSaveException e)
        {
            Debug.LogWarning($"Slot {slot} could not be read: {e.Message}");
            return null;
        }
    }

    public static SlotSummary ReadSummary(string slot)
    {
        if (!Exists(slot))
        {
            return null;
        }

        try
        {
            QuickSaveReader reader = QuickSaveReader.Create(RootFor(slot), Settings);
            return reader.TryRead(SummaryKey, out SlotSummary summary) ? summary : null;
        }
        catch (QuickSaveException)
        {
            return null;
        }
    }
}
