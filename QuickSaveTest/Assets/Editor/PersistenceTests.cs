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
        // Eigen map, zodat de tests geen echte saves overschrijven.
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
        game.Inventory.Add(new InventoryItem { Name = "Sleutel", Amount = 2 });
        game.Inventory.Add(new InventoryItem { Name = "Edelsteen", Amount = 1 });
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
        Assert.IsTrue(SaveSystem.Save(slot, game), "Save geeft false terug, kijk in de Console naar de foutmelding.");

        SaveGame loaded = SaveSystem.Load(slot);

        Assert.IsNotNull(loaded, "Load geeft null terug na een geslaagde Save.");
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
        Assert.AreEqual("Speler", SettingsStore.Load().PlayerName);
    }

    [Test]
    public void Save_CreatesFile()
    {
        SettingsStore.Save(new GameSettings { PlayerName = "Noor" });

        Assert.IsTrue(File.Exists(SettingsFile), "Geen settings.json gevonden. Roep je Commit() aan?");
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
    [TestCase("Noor \"de Baas\" van Dijk")]
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

        Assert.IsTrue(File.Exists(SlotFile("slot1")), "Geen save_slot1.json gevonden. Roep je Commit() aan?");
    }

    [Test]
    public void Save_CreatesOnlyOneFile()
    {
        SaveSystem.Save("slot1", FullGame());

        Assert.AreEqual(1, Directory.GetFiles(SaveFolder).Length, "Save hoort één bestand te schrijven.");
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
        WriteFile(SlotFile("slot1"), "{ dit is geen geldige json");

        Assert.IsTrue(SaveSystem.Exists("slot1"));
    }
}

public class Step3_LoadTests : PersistenceTestBase
{
    private static IEnumerable<string> AllSlots => SaveSlots.All;

    [Test]
    public void Load_IsNullForEmptySlot()
    {
        Assert.IsNull(SaveSystem.Load("slot1"), "Controleer eerst of het slot bestaat.");
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

        Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"Verwacht {expected}, maar kreeg {actual}.");
    }

    [Test]
    public void Load_RestoresRotation()
    {
        Quaternion expected = FullGame().Player.Rotation;
        Quaternion actual = SaveAndLoad(FullGame()).Player.Rotation;

        Assert.Less(Quaternion.Angle(expected, actual), 0.01f, $"Verwacht {expected}, maar kreeg {actual}.");
    }

    [Test]
    public void Load_RestoresInventory()
    {
        List<InventoryItem> inventory = SaveAndLoad(FullGame()).Inventory;

        Assert.AreEqual(2, inventory.Count, "Aantal items in de inventory klopt niet.");
        Assert.AreEqual("Sleutel", inventory[0].Name);
        Assert.AreEqual(2, inventory[0].Amount);
        Assert.AreEqual("Edelsteen", inventory[1].Name);
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

        Assert.AreEqual("Speler", loaded.Player.Name);
        Assert.AreEqual(100f, loaded.Player.Health, 0.001f);
        Assert.AreEqual(0, loaded.Player.Coins);
        Assert.Less(Quaternion.Angle(Quaternion.identity, loaded.Player.Rotation), 0.01f);
        Assert.IsNotNull(loaded.Inventory, "Inventory mag geen null zijn, ook niet als de lijst leeg is.");
        Assert.IsEmpty(loaded.Inventory);
        Assert.IsNotNull(loaded.CollectedIds, "CollectedIds mag geen null zijn, ook niet als de lijst leeg is.");
        Assert.IsEmpty(loaded.CollectedIds);
    }

    [Test]
    public void Load_KeepsSpecialCharacters()
    {
        SaveGame game = FullGame();
        game.Player.Name = "Noor \"de Baas\", Zoë";
        game.Inventory[0].Name = "Sleutel, groot";

        SaveGame loaded = SaveAndLoad(game);

        Assert.AreEqual("Noor \"de Baas\", Zoë", loaded.Player.Name);
        Assert.AreEqual("Sleutel, groot", loaded.Inventory[0].Name);
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

        Assert.AreEqual(7, second.Player.Coins, "Load hoort uit het bestand te lezen, niet uit een eerder object.");
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

        Assert.IsEmpty(loaded.Inventory, "Items die weg zijn horen niet terug te komen na opnieuw opslaan.");
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
                $"Slot {SaveSlots.All[i]} bevat de data van een ander slot.");
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

        Assert.IsNotNull(summary, "Geen summary gevonden. Schrijf je die mee in Save?");
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

        Assert.IsNotNull(summary, "Geen summary gevonden. Schrijf je die mee in Save?");
        Assert.That(summary.SavedAt, Is.InRange(before, after), "SavedAt hoort het moment van opslaan te zijn.");
    }

    [Test]
    public void ReadSummary_IsNullForEmptySlot()
    {
        Assert.IsNull(SaveSystem.ReadSummary("slot1"), "Controleer eerst of het slot bestaat.");
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

        Assert.IsNotNull(summary, "Geen summary gevonden. Schrijf je die mee in Save?");
        Assert.AreEqual("Sam", summary.PlayerName);
        Assert.AreEqual(50, summary.Coins);
    }

    [Test]
    public void ReadSummary_BelongsToSlot()
    {
        SaveSystem.Save("slot1", GameWithCoins(1));
        SaveSystem.Save("slot2", GameWithCoins(2));

        Assert.IsNotNull(SaveSystem.ReadSummary("slot1"), "Geen summary gevonden. Schrijf je die mee in Save?");
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
        Assert.IsFalse(File.Exists(SlotFile("slot1")), "Het bestand staat nog op schijf.");
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
        SettingsStore.Save(new GameSettings { PlayerName = "Instelling" });
        SaveSystem.Save("slot1", FullGame());
        SaveSystem.Delete("slot1");

        Assert.AreEqual("Instelling", SettingsStore.Load().PlayerName);

        SaveSystem.Save("slot1", FullGame());
        SettingsStore.Save(new GameSettings { PlayerName = "Anders" });

        Assert.AreEqual("Noor", SaveSystem.Load("slot1").Player.Name);
    }
}

public class Step5_DamagedFileTests : PersistenceTestBase
{
    private static readonly string[] DamagedContents =
    {
        "{ dit is geen geldige json",
        "",
        "null",
        "[1, 2, 3]",
        "{}",
        "{\"onbekend\":1}"
    };

    [Test]
    public void Settings_MissingKey_ReturnsDefaults()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{\"onbekend\":1}");

        GameSettings settings = null;
        Assert.DoesNotThrow(() => settings = SettingsStore.Load());

        Assert.AreEqual("Speler", settings.PlayerName);
    }

    [Test]
    public void Settings_DamagedFile_ReturnsDefaults()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{ dit is geen geldige json");

        GameSettings settings = null;
        Assert.DoesNotThrow(() => settings = SettingsStore.Load(), "Vang QuickSaveException op bij het lezen.");

        Assert.AreEqual("Speler", settings.PlayerName);
    }

    [Test]
    public void Settings_SaveWorksAfterLoadingDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SettingsFile, "{ dit is geen geldige json");
        SettingsStore.Load();

        Assert.DoesNotThrow(() => SettingsStore.Save(new GameSettings { PlayerName = "Noor" }),
            "QuickSaveWriter.Create kan een kapot bestand niet openen. Verwijder het bestand als het lezen mislukt.");

        Assert.AreEqual("Noor", SettingsStore.Load().PlayerName);
    }

    [TestCaseSource(nameof(DamagedContents))]
    public void Load_ReturnsNullForDamagedFile(string content)
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), content);

        SaveGame loaded = null;
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Vang QuickSaveException op in Load.");

        Assert.IsNull(loaded);
    }

    [TestCaseSource(nameof(DamagedContents))]
    public void ReadSummary_ReturnsNullForDamagedFile(string content)
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), content);

        SlotSummary summary = null;
        Assert.DoesNotThrow(() => summary = SaveSystem.ReadSummary("slot1"), "Vang QuickSaveException op in ReadSummary.");

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
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Vang QuickSaveException op in Load.");

        Assert.IsNull(loaded);
    }

    [Test]
    public void DamagedFile_DoesNotAffectOtherSlots()
    {
        LogAssert.ignoreFailingMessages = true;
        SaveSystem.Save("slot2", GameWithCoins(2));
        WriteFile(SlotFile("slot1"), "{ dit is geen geldige json");

        SaveGame loaded = null;
        Assert.DoesNotThrow(() => loaded = SaveSystem.Load("slot1"), "Vang QuickSaveException op in Load.");

        Assert.IsNull(loaded);
        Assert.AreEqual(2, SaveSystem.Load("slot2").Player.Coins);
    }

    [Test]
    public void Save_ReturnsFalseOnDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), "{ dit is geen geldige json");

        bool saved = true;
        Assert.DoesNotThrow(() => saved = SaveSystem.Save("slot1", FullGame()), "Vang QuickSaveException op in Save.");

        Assert.IsFalse(saved, "QuickSaveWriter.Create kan een kapot bestand niet openen, dus Save hoort false terug te geven.");
    }

    [Test]
    public void Delete_RemovesDamagedFile()
    {
        LogAssert.ignoreFailingMessages = true;
        WriteFile(SlotFile("slot1"), "{ dit is geen geldige json");

        SaveSystem.Delete("slot1");

        Assert.IsFalse(SaveSystem.Exists("slot1"));
        Assert.IsTrue(SaveSystem.Save("slot1", FullGame()), "Na het wissen hoort het slot weer bruikbaar te zijn.");
    }

    [Test]
    public void MostRecentSlot_SkipsDamagedSlots()
    {
        LogAssert.ignoreFailingMessages = true;
        SaveSystem.Save("slot1", FullGame());
        Thread.Sleep(20);
        SaveSystem.Save("slot2", FullGame());
        WriteFile(SlotFile("slot2"), "{ dit is geen geldige json");

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
            "De spelernaam staat leesbaar in het bestand. Zet SecurityMode op Aes en geef een Password mee.");
    }

    [Test]
    public void EncryptedSave_CanBeLoaded()
    {
        SaveGame loaded = SaveAndLoad(FullGame());

        Assert.AreEqual("Noor", loaded.Player.Name);
        Assert.AreEqual("Noor", SaveSystem.ReadSummary("slot1").PlayerName,
            "Gebruik dezelfde QuickSaveSettings in Save, Load en ReadSummary.");
    }
}
