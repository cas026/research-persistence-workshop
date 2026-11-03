using System;
using CI.QuickSave;
using UnityEngine;

public static class SaveSystem
{
    private const string GameKey = "game";
    private const string SummaryKey = "summary";

    private static QuickSaveSettings Settings => new QuickSaveSettings { SecurityMode = SecurityMode.None };
    // private static QuickSaveSettings Settings => new QuickSaveSettings { SecurityMode = SecurityMode.Aes, Password = "abcdef" };

    private static string RootFor(string slot) => "save_" + slot;

    public static bool Exists(string slot)
    {
        return QuickSaveReader.RootExists(RootFor(slot));
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

        try {
            var writer = QuickSaveWriter.Create(RootFor(slot), Settings);
            writer.Write(GameKey, game);
            writer.Write(SummaryKey, summary);
            writer.Commit();

            return true;
        }
        catch (QuickSaveException e)
        {
            Debug.LogError(e);
            return false;
        }
    }

    public static SaveGame Load(string slot)
    {
        if (!Exists(slot)) return null;
         try {
            var reader = QuickSaveReader.Create(RootFor(slot), Settings);
            if (reader.TryRead<SaveGame>(GameKey, out var result)) return result;
        }
        catch (QuickSaveException e)
        {
            Debug.LogWarning(e);
            return null;
        }

        return null;
    }

    public static SlotSummary ReadSummary(string slot)
    {
        if (!Exists(slot)) return null;

        try
        {
            var reader = QuickSaveReader.Create(RootFor(slot), Settings);
            if (reader.TryRead<SlotSummary>(SummaryKey, out var result)) return result;
        }
        catch (QuickSaveException)
        {
            return null;
        }

        return null;
    }
}
