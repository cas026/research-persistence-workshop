using CI.QuickSave;
using UnityEngine;

public class GameSettings
{
    public string PlayerName = "Speler";
}

public static class SettingsStore
{
    private const string Root = "settings";

    public static void Save(GameSettings settings)
    {
        QuickSaveWriter.Create(Root)
                       .Write("PlayerName", settings.PlayerName)
                       .Commit();
    }

    public static GameSettings Load()
    {
        GameSettings settings = new GameSettings();

        if (!QuickSaveReader.RootExists(Root))
        {
            return settings;
        }

        try
        {
            QuickSaveReader reader = QuickSaveReader.Create(Root);

            if (reader.TryRead("PlayerName", out string playerName)) settings.PlayerName = playerName;
        }
        catch (QuickSaveException e)
        {
            Debug.LogWarning($"Instellingen konden niet worden gelezen: {e.Message}");
            // QuickSaveWriter.Create kan een kapot bestand ook niet openen.
            QuickSaveWriter.DeleteRoot(Root);
        }

        return settings;
    }
}
