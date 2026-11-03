using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSession
{
    public const string MenuScene = "Main Scene";
    public const string LevelScene = "Game";

    private static GameSettings _settings;
    private static SaveGame _loadedGame;
    private static string _loadedSlot;

    public static SaveGame Current { get; private set; }

    public static string PendingMessage { get; set; }

    public static GameSettings Settings => _settings ?? (_settings = SettingsStore.Load());

    public static void SaveSettings()
    {
        SettingsStore.Save(Settings);
    }

    public static void NewGame(string playerName)
    {
        Current = new SaveGame();
        Current.Player.Name = playerName;
        _loadedSlot = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelScene);
    }

    public static bool Save(string slot)
    {
        PlayerController player = PlayerController.Instance;
        if (Current == null || player == null)
        {
            return false;
        }

        Current.Player.Position = player.transform.position;
        Current.Player.Rotation = player.transform.rotation;

        return SaveSystem.Save(slot, Current);
    }

    public static bool Load(string slot)
    {
        SaveGame loaded = SaveSystem.Load(slot);
        if (loaded == null)
        {
            return false;
        }

        // LoadScene waits until the end of the frame: the old scene must not change this data anymore.
        _loadedGame = loaded;
        _loadedSlot = slot;
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelScene);
        return true;
    }

    public static void BackToMenu()
    {
        Save(SaveSlots.Auto);
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }

    public static string MostRecentSlot()
    {
        return SaveSlots.All
            .Where(slot => SaveSystem.ReadSummary(slot) != null)
            .OrderByDescending(slot => SaveSystem.ReadSummary(slot).SavedAt)
            .FirstOrDefault();
    }

    public static void EnsureGameExists()
    {
        if (_loadedGame != null)
        {
            Current = _loadedGame;
            _loadedGame = null;
        }
        else if (Current == null)
        {
            Current = new SaveGame();
            Current.Player.Name = Settings.PlayerName;
        }
    }

    public static bool TakeRestorePosition()
    {
        if (_loadedSlot == null)
        {
            return false;
        }

        PendingMessage = $"{SaveSlots.DisplayName(_loadedSlot)} loaded";
        _loadedSlot = null;
        return true;
    }

    public static bool IsCollected(string id) => Current.CollectedIds.Contains(id);

    public static void MarkCollected(string id)
    {
        Current.CollectedIds.Add(id);
    }

    public static void AddItem(string itemName)
    {
        InventoryItem item = Current.Inventory.FirstOrDefault(i => i.Name == itemName);

        if (item == null)
        {
            Current.Inventory.Add(new InventoryItem { Name = itemName, Amount = 1 });
        }
        else
        {
            item.Amount++;
        }
    }

    public static bool RemoveItem(string itemName)
    {
        InventoryItem item = Current.Inventory.FirstOrDefault(i => i.Name == itemName);
        if (item == null)
        {
            return false;
        }

        item.Amount--;
        if (item.Amount <= 0)
        {
            Current.Inventory.Remove(item);
        }

        return true;
    }
}
