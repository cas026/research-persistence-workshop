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
        // TODO 1: write settings.PlayerName to Root.
    }

    public static GameSettings Load()
    {
        GameSettings settings = new GameSettings();

        // TODO 1: read the name from Root if the file exists.
        // TODO 5: catch QuickSaveException, log a warning and delete the corrupt file.

        return settings;
    }
}
