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
            .Write("pn", settings.PlayerName)
            .Commit();
    }

    public static GameSettings Load()
    {
        GameSettings settings = new GameSettings();

        if (!QuickSaveReader.RootExists(Root)) return settings;
        
        try {
            var reader = QuickSaveReader.Create(Root);
            if (reader.TryRead<string>("pn", out var result)) settings.PlayerName = result;
        } 
        catch (QuickSaveException e)
        {
            Debug.LogWarning(e);
            QuickSaveWriter.DeleteRoot(Root);
        }

        return settings;
    }
}
