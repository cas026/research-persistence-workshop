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
        // TODO 2: geef terug of RootFor(slot) bestaat.
        return false;
    }

    public static void Delete(string slot)
    {
        // TODO 4: verwijder RootFor(slot).
    }

    public static bool Save(string slot, SaveGame game)
    {
        SlotSummary summary = new SlotSummary
        {
            PlayerName = game.Player.Name,
            Coins = game.Player.Coins,
            SavedAt = DateTime.Now
        };

        // TODO 2: schrijf game en summary weg in RootFor(slot) met Settings, en geef true terug.
        // TODO 5: vang QuickSaveException op, log de fout en geef false terug.

        return false;
    }

    public static SaveGame Load(string slot)
    {
        // TODO 3: geef null terug als het slot niet bestaat, en lees anders de game uit RootFor(slot) met Settings.
        // TODO 5: vang QuickSaveException op, log een waarschuwing en geef null terug.

        return null;
    }

    public static SlotSummary ReadSummary(string slot)
    {
        // TODO 4: geef null terug als het slot niet bestaat, en lees anders de summary met Settings.
        // TODO 5: vang QuickSaveException op en geef null terug.

        return null;
    }
}
