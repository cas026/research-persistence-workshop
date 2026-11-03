using CI.QuickSave;
using UnityEngine;

public class GameSettings
{
    public string PlayerName = "Player";
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
            Debug.LogWarning($"Settings could not be read: {e.Message}");
            // QuickSaveWriter.Create cannot open a damaged file either.
            QuickSaveWriter.DeleteRoot(Root);
        }

        return settings;
    }
}
