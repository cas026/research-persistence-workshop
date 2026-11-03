using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CI.QuickSave;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public abstract class PersistenceTestBase
{
    private string _originalLocation;
    private string _testLocation;

    protected string SaveFolder => Path.Combine(_testLocation, "QuickSave");

    [SetUp]
    public void SetUp()
    {
        // A separate folder, so the tests don't overwrite real saves.
        _originalLocation = QuickSaveGlobalSettings.StorageLocation;
        _testLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        QuickSaveGlobalSettings.StorageLocation = _testLocation;
    }

    [TearDown]
    public void TearDown()
    {
        QuickSaveGlobalSettings.StorageLocation = _originalLocation;

        if (Directory.Exists(_testLocation))
        {
            Directory.Delete(_testLocation, true);
        }
    }

    protected string SlotFile(string slot) => Path.Combine(SaveFolder, $"save_{slot}.json");

    protected string SettingsFile => Path.Combine(SaveFolder, "settings.json");

    protected void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(SaveFolder);
        File.WriteAllText(path, content);
    }

    protected static SaveGame FullGame()
    {
        SaveGame game = new SaveGame();
        game.Player.Name = "Noor";
        game.Player.Health = 42.5f;
        game.Player.Coins = 7;
        game.Player.Position = new Vector3(1.5f, -2.25f, 300f);
        game.Player.Rotation = Quaternion.Euler(10f, 90f, 20f);
        game.Inventory.Add(new InventoryItem { Name = "Key", Amount = 2 });
        game.Inventory.Add(new InventoryItem { Name = "Gem", Amount = 1 });
        game.CollectedIds.Add("coin_01");
        game.CollectedIds.Add("key_01");
        return game;
    }

    protected static SaveGame GameWithCoins(int coins)
    {
        SaveGame game = new SaveGame();
        game.Player.Coins = coins;
        return game;
    }

    protected static SaveGame SaveAndLoad(SaveGame game, string slot = "slot1")
    {
        Assert.IsTrue(SaveSystem.Save(slot, game), "Save returned false, check the Console for the error.");

        SaveGame loaded = SaveSystem.Load(slot);

        Assert.IsNotNull(loaded, "Load returned null after a successful Save.");
        return loaded;
    }
}

public class SaveLocationTests
{
    [Test]
    public void PrintSaveFolder()
    {
        Debug.Log(Path.Combine(QuickSaveGlobalSettings.StorageLocation, "QuickSave"));
    }
}

public class Step1_SettingsTests : PersistenceTestBase
{
    [Test]
    public void Load_WithoutFile_ReturnsDefaults()
    {
        Assert.AreEqual("Player", SettingsStore.Load().PlayerName);
    }

    [Test]
    public void Save_CreatesFile()
    {
        SettingsStore.Save(new GameSettings { PlayerName = "Noor" });

        Assert.IsTrue(File.Exists(SettingsFile), "No settings.json found. Did you call Commit()?");
    }

    [Test]
    public void Load_ReturnsSavedName()
    {
        SettingsStore.Save(new GameSettings { PlayerName = "Noor" });

        Assert.AreEqual("Noor", SettingsStore.Load().PlayerName);
    }

    [Test]
    public void Save_OverwritesPreviousName()
    {
        SettingsStore.Save(new GameSettings { PlayerName = "Noor" });
        SettingsStore.Save(new GameSettings { PlayerName = "Sam" });

        Assert.AreEqual("Sam", SettingsStore.Load().PlayerName);
    }

    [TestCase("")]
    [TestCase("Noor \"the Boss\" van Dijk")]
    [TestCase("Zoë, 名前")]
    public void Load_KeepsSpecialCharacters(string name)
    {
        SettingsStore.Save(new GameSettings { PlayerName = name });

        Assert.AreEqual(name, SettingsStore.Load().PlayerName);
    }
}

public class Step2_SaveTests : PersistenceTestBase
{
    private static IEnumerable<string> AllSlots => SaveSlots.All;

    [Test]
    public void Save_ReturnsTrue()
    {
        Assert.IsTrue(SaveSystem.Save("slot1", FullGame()));
    }

    [Test]
    public void Save_CreatesFileForSlot()
    {
        SaveSystem.Save("slot1", FullGame());

        Assert.IsTrue(File.Exists(SlotFile("slot1")), "No save_slot1.json found. Did you call Commit()?");
    }

    [Test]
    public void Save_CreatesOnlyOneFile()
    {
        SaveSystem.Save("slot1", FullGame());

        Assert.AreEqual(1, Directory.GetFiles(SaveFolder).Length, "Save should write one file.");
    }

    [TestCaseSource(nameof(AllSlots))]
    public void Exists_IsFalseForEmptySlot(string slot)
    {
        Assert.IsFalse(SaveSystem.Exists(slot));
    }

    [Test]
    public void Exists_IsTrueAfterSave()
    {
        SaveSystem.Save("slot1", FullGame());

        Assert.IsTrue(SaveSystem.Exists("slot1"));
    }

    [Test]
    public void Exists_OnlyForSavedSlot()
    {
        SaveSystem.Save("slot1", FullGame());

        Assert.IsFalse(SaveSystem.Exists("slot2"));
    }

    [Test]
    public void Exists_IsTrueForDamagedFile()
    {
        WriteFile(SlotFile("slot1"), "{ this is not valid json");

        Assert.IsTrue(SaveSystem.Exists("slot1"));
    }
}

public class Step3_LoadTests : PersistenceTestBase
{
    private static IEnumerable<string> AllSlots => SaveSlots.All;

    [Test]
    public void Load_IsNullForEmptySlot()
    {
        Assert.IsNull(SaveSystem.Load("slot1"), "Check first whether the slot exists.");
    }

    [Test]
    public void Load_RestoresPlayerName()
    {
        Assert.AreEqual("Noor", SaveAndLoad(FullGame()).Player.Name);
    }

    [Test]
    public void Load_RestoresHealth()
    {
        Assert.AreEqual(42.5f, SaveAndLoad(FullGame()).Player.Health, 0.001f);
    }

    [Test]
    public void Load_RestoresCoins()
    {
        Assert.AreEqual(7, SaveAndLoad(FullGame()).Player.Coins);
    }

    [Test]
    public void Load_RestoresPosition()
    {
        Vector3 expected = FullGame().Player.Position;
        Vector3 actual = SaveAndLoad(FullGame()).Player.Position;

        Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"Expected {expected}, but got {actual}.");
    }

    [Test]
    public void Load_RestoresRotation()
    {
        Quaternion expected = FullGame().Player.Rotation;
        Quaternion actual = SaveAndLoad(FullGame()).Player.Rotation;

        Assert.Less(Quaternion.Angle(expected, actual), 0.01f, $"Expected {expected}, but got {actual}.");
    }

    [Test]
    public void Load_RestoresInventory()
    {
        List<InventoryItem> inventory = SaveAndLoad(FullGame()).Inventory;

        Assert.AreEqual(2, inventory.Count, "The number of inventory items is wrong.");
        Assert.AreEqual("Key", inventory[0].Name);
        Assert.AreEqual(2, inventory[0].Amount);
        Assert.AreEqual("Gem", inventory[1].Name);
        Assert.AreEqual(1, inventory[1].Amount);
    }

    [Test]
    public void Load_RestoresCollectedIds()
    {
        CollectionAssert.AreEqual(new[] { "coin_01", "key_01" }, SaveAndLoad(FullGame()).CollectedIds);
    }

    [Test]
    public void Load_NewGame_KeepsDefaultsAndEmptyLists()
    {
        SaveGame loaded = SaveAndLoad(new SaveGame());

        Assert.AreEqual("Player", loaded.Player.Name);
        Assert.AreEqual(100f, loaded.Player.Health, 0.001f);
        Assert.AreEqual(0, loaded.Player.Coins);
        Assert.Less(Quaternion.Angle(Quaternion.identity, loaded.Player.Rotation), 0.01f);
        Assert.IsNotNull(loaded.Inventory, "Inventory must not be null, even when the list is empty.");
        Assert.IsEmpty(loaded.Inventory);
        Assert.IsNotNull(loaded.CollectedIds, "CollectedIds must not be null, even when the list is empty.");
        Assert.IsEmpty(loaded.CollectedIds);
    }

    [Test]
    public void Load_KeepsSpecialCharacters()
    {
        SaveGame game = FullGame();
        game.Player.Name = "Noor \"the Boss\", Zoë";
        game.Inventory[0].Name = "Key, large";

        SaveGame loaded = SaveAndLoad(game);

        Assert.AreEqual("Noor \"the Boss\", Zoë", loaded.Player.Name);
        Assert.AreEqual("Key, large", loaded.Inventory[0].Name);
    }

    [Test]
    public void Load_ReadsFromFile()
    {
        SaveSystem.Save("slot1", FullGame());

        SaveGame first = SaveSystem.Load("slot1");
        first.Player.Coins = 999;
        first.Inventory.Clear();
        first.CollectedIds.Clear();

        SaveGame second = SaveSystem.Load("slot1");

        Assert.AreEqual(7, second.Player.Coins, "Load should read from the file, not from an earlier object.");
        Assert.AreEqual(2, second.Inventory.Count);
        Assert.AreEqual(2, second.CollectedIds.Count);
    }

    [Test]
    public void Save_OverwritesPreviousValues()
    {
        SaveSystem.Save("slot1", FullGame());

        SaveGame updated = FullGame();
        updated.Player.Name = "Sam";
        updated.Player.Coins = 50;
        updated.Player.Position = Vector3.one;

        SaveGame loaded = SaveAndLoad(updated);

        Assert.AreEqual("Sam", loaded.Player.Name);
        Assert.AreEqual(50, loaded.Player.Coins);
        Assert.Less(Vector3.Distance(Vector3.one, loaded.Player.Position), 0.001f);
    }

    [Test]
    public void Save_RemovedItemsStayRemoved()
    {
        SaveSystem.Save("slot1", FullGame());

        SaveGame updated = FullGame();
        updated.Inventory.Clear();
        updated.CollectedIds.RemoveAt(0);

        SaveGame loaded = SaveAndLoad(updated);

        Assert.IsEmpty(loaded.Inventory, "Removed items should not come back after saving again.");
        CollectionAssert.AreEqual(new[] { "key_01" }, loaded.CollectedIds);
    }

    [Test]
    public void SaveAfterLoad_DoesNotDuplicateLists()
    {
        SaveSystem.Save("slot1", FullGame());
        SaveSystem.Save("slot1", SaveSystem.Load("slot1"));

        SaveGame loaded = SaveSystem.Load("slot1");

        Assert.AreEqual(2, loaded.Inventory.Count);
        Assert.AreEqual(2, loaded.CollectedIds.Count);
    }

    [TestCaseSource(nameof(AllSlots))]
    public void EverySlot_CanSaveAndLoad(string slot)
    {
        Assert.AreEqual("Noor", SaveAndLoad(FullGame(), slot).Player.Name);
    }

    [Test]
    public void Slots_AreIndependent()
    {
        for (int i = 0; i < SaveSlots.All.Length; i++)
        {
            SaveSystem.Save(SaveSlots.All[i], GameWithCoins(i + 1));
        }

        for (int i = 0; i < SaveSlots.All.Length; i++)
        {
            Assert.AreEqual(i + 1, SaveSystem.Load(SaveSlots.All[i]).Player.Coins,
                $"Slot {SaveSlots.All[i]} contains data from another slot.");
        }
    }
}

public class Step4_SummaryAndDeleteTests : PersistenceTestBase
{
    [Test]
    public void ReadSummary_ReturnsNameAndCoins()
    {
        SaveSystem.Save("slot1", FullGame());

        SlotSummary summary = SaveSystem.ReadSummary("slot1");

        Assert.IsNotNull(summary, "No summary found. Do you write it in Save?");
        Assert.AreEqual("Noor", summary.PlayerName);
        Assert.AreEqual(7, summary.Coins);
    }

    [Test]
    public void ReadSummary_ReturnsCurrentTime()
    {
        DateTime before = DateTime.Now.AddSeconds(-1);
        SaveSystem.Save("slot1", FullGame());
        DateTime after = DateTime.Now.AddSeconds(1);

        SlotSummary summary = SaveSystem.ReadSummary("slot1");

        Assert.IsNotNull(summary, "No summary found. Do you write it in Save?");
        Assert.That(summary.SavedAt, Is.InRange(before, after), "SavedAt should be the moment of saving.");
    }

    [Test]
    public void ReadSummary_IsNullForEmptySlot()
    {
        Assert.IsNull(SaveSystem.ReadSummary("slot1"), "Check first whether the slot exists.");
    }

    [Test]
    public void ReadSummary_FollowsLatestSave()
    {
        SaveSystem.Save("slot1", FullGame());

        SaveGame updated = FullGame();
        updated.Player.Name = "Sam";
        updated.Player.Coins = 50;
        SaveSystem.Save("slot1", updated);

        SlotSummary summary = SaveSystem.ReadSummary("slot1");

        Assert.IsNotNull(summary, "No summary found. Do you write it in Save?");
        Assert.AreEqual("Sam", summary.PlayerName);
        Assert.AreEqual(50, summary.Coins);
    }

    [Test]
    public void ReadSummary_BelongsToSlot()
    {
        SaveSystem.Save("slot1", GameWithCoins(1));
        SaveSystem.Save("slot2", GameWithCoins(2));

        Assert.IsNotNull(SaveSystem.ReadSummary("slot1"), "No summary found. Do you write it in Save?");
        Assert.AreEqual(1, SaveSystem.ReadSummary("slot1").Coins);
        Assert.AreEqual(2, SaveSystem.ReadSummary("slot2").Coins);
    }

    [Test]
    public void MostRecentSlot_IsNullWithoutSaves()
    {
        Assert.IsNull(GameSession.MostRecentSlot());
    }

    [Test]
    public void MostRecentSlot_IsLastSavedSlot()
    {
        SaveSystem.Save("slot1", FullGame());
        Thread.Sleep(20);
        SaveSystem.Save("slot2", FullGame());

        Assert.AreEqual("slot2", GameSession.MostRecentSlot());

        Thread.Sleep(20);
        SaveSystem.Save("slot1", FullGame());

        Assert.AreEqual("slot1", GameSession.MostRecentSlot());
    }

    [Test]
    public void Delete_RemovesSave()
    {
        SaveSystem.Save("slot1", FullGame());

        SaveSystem.Delete("slot1");

        Assert.IsFalse(SaveSystem.Exists("slot1"));
        Assert.IsFalse(File.Exists(SlotFile("slot1")), "The file is still on disk.");
        Assert.IsNull(SaveSystem.Load("slot1"));
        Assert.IsNull(SaveSystem.ReadSummary("slot1"));
    }

    [Test]
    public void Delete_KeepsOtherSlots()
    {
        SaveSystem.Save("slot1", GameWithCoins(1));
        SaveSystem.Save("slot2", GameWithCoins(2));

        SaveSystem.Delete("slot1");

        Assert.AreEqual(2, SaveSystem.Load("slot2").Player.Coins);
    }

    [Test]
    public void Delete_EmptySlot_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => SaveSystem.Delete("slot1"));
    }

    [Test]
    public void Save_WorksAfterDelete()
    {
        SaveSystem.Save("slot1", FullGame());
        SaveSystem.Delete("slot1");

        Assert.AreEqual(5, SaveAndLoad(GameWithCoins(5)).Player.Coins);
    }

    [Test]
    public void Settings_AndSaves_DoNotAffectEachOther()
    {
        SettingsStore.Save(new GameSettings { PlayerName = "Setting" });
        SaveSystem.Save("slot1", FullGame());
        SaveSystem.Delete("slot1");

        Assert.AreEqual("Setting", SettingsStore.Load().PlayerName);

        SaveSystem.Save("slot1", FullGame());
        SettingsStore.Save(new GameSettings { PlayerName = "Other" });

        Assert.AreEqual("Noor", SaveSystem.Load("slot1").Player.Name);
    }
}

public class Step5_DamagedFileTests : PersistenceTestBase
{
    private static readonly string[] DamagedContents =
    {
        "{ this is not valid json",
        "",
        "null",
        "[1, 2, 3]",
        "{}",
        "{\"unknown\":1}"
    };

    [Test]
    public void Settings_MissingKey_ReturnsDefaults()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{\"unknown\":1}");

        GameSettings settings = null;
        Assert.DoesNotThrow(() => settings = SettingsStore.Load());

        Assert.AreEqual("Player", settings.PlayerName);
    }

    [Test]
    public void Settings_DamagedFile_ReturnsDefaults()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{ this is not valid json");

        GameSettings settings = null;
        Assert.DoesNotThrow(() => settings = SettingsStore.Load(), "Catch QuickSaveException when reading.");

        Assert.AreEqual("Player", settings.PlayerName);
    }

    [Test]
    public void Settings_SaveWorksAfterLoadingDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{ this is not valid json");
        SettingsStore.Load();

        Assert.DoesNotThrow(() => SettingsStore.Save(new GameSettings { PlayerName = "Noor" }),
            "QuickSaveWriter.Create cannot open a damaged file. Delete the file when reading fails.");

        Assert.AreEqual("Noor", SettingsStore.Load().PlayerName);
    }

    [TestCaseSource(nameof(DamagedContents))]
    public void Load_ReturnsNullForDamagedFile(string content)
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), content);

        SaveGame loaded = null;
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Catch QuickSaveException in Load.");

        Assert.IsNull(loaded);
    }

    [TestCaseSource(nameof(DamagedContents))]
    public void ReadSummary_ReturnsNullForDamagedFile(string content)
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), content);

        SlotSummary summary = null;
        Assert.DoesNotThrow(() => summary = SaveSystem.ReadSummary("slot1"), "Catch QuickSaveException in ReadSummary.");

        Assert.IsNull(summary);
    }

    [Test]
    public void Load_ReturnsNullForHalfWrittenFile()
    {
        LogAssert.ignoreFailingMessages = true;
        SaveSystem.Save("slot1", FullGame());
        string content = File.ReadAllText(SlotFile("slot1"));
        File.WriteAllText(SlotFile("slot1"), content.Substring(0, content.Length / 2));

        SaveGame loaded = null;
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Catch QuickSaveException in Load.");

        Assert.IsNull(loaded);
    }

    [Test]
    public void DamagedFile_DoesNotAffectOtherSlots()
    {
        LogAssert.ignoreFailingMessages = true;
        SaveSystem.Save("slot2", GameWithCoins(2));
        WriteFile(SlotFile("slot1"), "{ this is not valid json");

        SaveGame loaded = null;
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Catch QuickSaveException in Load.");

        Assert.IsNull(loaded);
        Assert.AreEqual(2, SaveSystem.Load("slot2").Player.Coins);
    }

    [Test]
    public void Save_ReturnsFalseOnDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), "{ this is not valid json");

        bool saved = true;
        Assert.DoesNotThrow(() => saved = SaveSystem.Save("slot1", FullGame()), "Catch QuickSaveException in Save.");

        Assert.IsFalse(saved, "QuickSaveWriter.Create cannot open a damaged file, so Save should return false.");
    }

    [Test]
    public void Delete_RemovesDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), "{ this is not valid json");

        SaveSystem.Delete("slot1");

        Assert.IsFalse(SaveSystem.Exists("slot1"));
        Assert.IsTrue(SaveSystem.Save("slot1", FullGame()), "The slot should be usable again after deleting.");
    }

    [Test]
    public void MostRecentSlot_SkipsDamagedSlots()
    {
        LogAssert.ignoreFailingMessages = true;
        SaveSystem.Save("slot1", FullGame());
        Thread.Sleep(20);
        SaveSystem.Save("slot2", FullGame());
        WriteFile(SlotFile("slot2"), "{ this is not valid json");

        Assert.AreEqual("slot1", GameSession.MostRecentSlot());
    }
}

[Explicit("Extra: AES")]
[Category("Extra")]
public class Step6_ExtraEncryptionTests : PersistenceTestBase
{
    [Test]
    public void SaveFile_IsNotReadable()
    {
        SaveSystem.Save("slot1", FullGame());

        StringAssert.DoesNotContain("Noor", File.ReadAllText(SlotFile("slot1")),
            "The player name is readable in the file. Set SecurityMode to Aes and provide a Password.");
    }

    [Test]
    public void EncryptedSave_CanBeLoaded()
    {
        SaveGame loaded = SaveAndLoad(FullGame());

        Assert.AreEqual("Noor", loaded.Player.Name);
        Assert.AreEqual("Noor", SaveSystem.ReadSummary("slot1").PlayerName,
            "Use the same QuickSaveSettings in Save, Load and ReadSummary.");
    }
}
