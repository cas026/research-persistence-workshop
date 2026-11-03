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
        // TODO 2: return whether or not RootFor(slot) exists.
        return false;
    }

    public static void Delete(string slot)
    {
        // TODO 4: delete RootFor(slot).
    }

    public static bool Save(string slot, SaveGame game)
    {
        SlotSummary summary = new SlotSummary
        {
            PlayerName = game.Player.Name,
            Coins = game.Player.Coins,
            SavedAt = DateTime.Now
        };

        // TODO 2: write game en summary to RootFor(slot), and return true.
        // TODO 5: catch QuickSaveException, log the error and return false.

        return false;
    }

    public static SaveGame Load(string slot)
    {
        // TODO 3: return null if slot doesn't exist, otherwise read from RootFor(slot) and return the game.
        // TODO 5: catch QuickSaveException, log a warning and return null.

        return null;
    }

    public static SlotSummary ReadSummary(string slot)
    {
        // TODO 4: return null if slot doesn't exist, otherwise read the summary from RootFor(slot).
        // TODO 5: catch QuickSaveException and return null.

        return null;
    }
}
