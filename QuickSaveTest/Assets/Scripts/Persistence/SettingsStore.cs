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
        // TODO 1: schrijf settings.PlayerName weg in Root.
    }

    public static GameSettings Load()
    {
        GameSettings settings = new GameSettings();

        // TODO 1: lees de naam uit Root als dat bestand bestaat.
        // TODO 5: vang QuickSaveException op, log een waarschuwing en verwijder het kapotte bestand.

        return settings;
    }
}
