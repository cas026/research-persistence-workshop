using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Encoders;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

/// <summary>
/// Edge case tests for the save slot research, run them via the component's context menu (⋮ in the Inspector).
/// Results go to the Console; the large save measurement is also written to a CSV file.
/// </summary>
[RequireComponent(typeof(SaveSlotManager))]
public class SaveSlotResearchTests : MonoBehaviour
{
    [Header("Large save")]
    [SerializeField] private int[] largeSaveItemCounts = { 1_000, 100_000, 1_000_000 };
    [SerializeField] private int measureRuns = 3;

    private SaveSlotManager Manager => GetComponent<SaveSlotManager>();

    // ---------- Corrupt files ----------

    [ContextMenu("Corrupt: truncate data of slot 0")]
    private void CorruptTruncate() => CorruptSlot(0, path =>
    {
        var bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes.AsSpan(0, bytes.Length / 2).ToArray());
    }, "cut in half");

    [ContextMenu("Corrupt: garbage data in slot 1")]
    private void CorruptGarbage() => CorruptSlot(1, path =>
        File.WriteAllText(path, "this is not json at all"), "random text");

    [ContextMenu("Corrupt: empty data file in slot 2")]
    private void CorruptEmpty() => CorruptSlot(2, path => File.WriteAllBytes(path, Array.Empty<byte>()), "0 bytes");

    [ContextMenu("Corrupt: garbage metadata in slot 3")]
    private void CorruptMetadata()
    {
        var manager = Manager;
        manager.SaveToSlot(3);
        File.WriteAllText(manager.GetMetadataPath(3), "{\"saveVersion\": 1, \"sceneN");
        Debug.Log("[Corrupt] Slot 3 metadata file truncated");
        manager.RefreshSlotInfo();
        Debug.Log($"[Corrupt] Overview state slot 3: {manager.GetSlotState(3, out _)}");
        manager.LoadFromSlot(3);
    }

    [ContextMenu("Corrupt: add file with bad name (slot_abc.json)")]
    private void CorruptBadFileName()
    {
        var folder = Path.GetDirectoryName(Manager.GetDataPath(0));
        File.WriteAllText(Path.Combine(folder, "slot_abc.json"), "{}");
        Debug.Log("[Corrupt] Added slot_abc.json, run 'Log all slots on disk' on SaveSlotManager to see it is skipped");
    }

    private void CorruptSlot(int slot, Action<string> corrupt, string description)
    {
        var manager = Manager;
        // Start from a valid save so we know the corruption is the only problem
        manager.SaveToSlot(slot);

        var path = manager.GetDataPath(slot);
        corrupt(path);
        Debug.Log($"[Corrupt] Slot {slot} data file corrupted ({description}), now {new FileInfo(path).Length} bytes");

        manager.RefreshSlotInfo();
        Debug.Log($"[Corrupt] Overview state slot {slot}: {manager.GetSlotState(slot, out _)}");

        var loaded = manager.LoadFromSlot(slot);
        Debug.Log($"[Corrupt] Load slot {slot} returned {loaded}, game keeps running with: {manager.PlayerData}");
    }

    // ---------- Version difference ----------

    [ContextMenu("Version: save V1, load as V2 (JSON + Binary)")]
    private void VersionTest()
    {
        var oldSave = new VersionTestDataV1
        {
            playerName = "OldPlayer",
            level = 12,
            health = 75.5f,
            gold = 999
        };
        Debug.Log("[Version] Saved V1: " + oldSave);

        RunVersionTest("JSON", new JsonEncoder<VersionTestDataV1>(), new JsonEncoder<VersionTestDataV2>(), oldSave);
        RunVersionTest("Binary", new BinaryEncoder<VersionTestDataV1>(), new BinaryEncoder<VersionTestDataV2>(), oldSave);
    }

    private static void RunVersionTest(string format, IEncoder<VersionTestDataV1> oldEncoder,
        IEncoder<VersionTestDataV2> newEncoder, VersionTestDataV1 oldSave)
    {
        // Both use the same file name, so V2 reads exactly the file V1 wrote
        var oldPersistence = new SlotPersistence<VersionTestDataV1>(oldEncoder, folderName: "research/version");
        var newPersistence = new SlotPersistence<VersionTestDataV2>(newEncoder, folderName: "research/version");

        oldPersistence.Save(0, oldSave);
        var state = newPersistence.TryLoad(0, out var newData);

        Debug.Log($"[Version] {format}: loading V1 file as V2 -> {state}" +
                  (state == SlotState.Ok ? ": " + newData : "") + $"\nFile: {oldPersistence.GetPath(0)}");

        if (state == SlotState.Ok && newData.saveVersion != VersionTestDataV2.Version)
        {
            Debug.LogWarning($"[Version] {format}: file says saveVersion {newData.saveVersion}, " +
                             $"code expects {VersionTestDataV2.Version} -> a migration step could run here");
        }
    }

    // ---------- Large save ----------

    [ContextMenu("Large save: measure (can freeze the editor for a while)")]
    private void LargeSaveTest()
    {
        var persistence = new SlotPersistence<LargeSaveData>(new JsonEncoder<LargeSaveData>(), folderName: "research/large");
        var csv = new StringBuilder("items;file size (bytes);avg save ms;avg load ms;load ok\n");

        foreach (var count in largeSaveItemCounts)
        {
            var data = LargeSaveData.Create(count);

            // Warm-up run, the first call is always slower (JIT, file cache)
            persistence.Save(0, data);
            persistence.TryLoad(0, out _);

            long saveMs = 0, loadMs = 0;
            var state = SlotState.Empty;
            LargeSaveData loaded = null;

            for (int run = 0; run < measureRuns; run++)
            {
                // The samples show up as named blocks in the Profiler, including their GC Alloc
                // (GC.GetAllocatedBytesForCurrentThread always returns 0 in Unity's Mono)
                Profiler.BeginSample($"LargeSave.Save {count}");
                var stopwatch = Stopwatch.StartNew();
                persistence.Save(0, data);
                saveMs += stopwatch.ElapsedMilliseconds;
                Profiler.EndSample();

                Profiler.BeginSample($"LargeSave.Load {count}");
                stopwatch.Restart();
                state = persistence.TryLoad(0, out loaded);
                loadMs += stopwatch.ElapsedMilliseconds;
                Profiler.EndSample();
            }

            var fileSize = new FileInfo(persistence.GetPath(0)).Length;
            var loadOk = state == SlotState.Ok && loaded.pets.Count == count;

            Debug.Log($"[Large save] {count:N0} items: {fileSize / 1024f / 1024f:F2} MB, " +
                      $"save {saveMs / measureRuns} ms, load {loadMs / measureRuns} ms, " +
                      $"loaded correctly: {loadOk}");
            csv.AppendLine($"{count};{fileSize};{saveMs / measureRuns};{loadMs / measureRuns};{loadOk}");
        }

        persistence.Delete(0);
        var csvPath = Path.Combine(persistence.SaveFolder, "large_save_results.csv");
        File.WriteAllText(csvPath, csv.ToString());
        Debug.Log($"[Large save] Done ({measureRuns} runs per size, {(Application.isEditor ? "Editor" : "Build")}). " +
                  $"Results: {csvPath}");
    }
}

/// <summary>The "old" save format.</summary>
[Serializable]
public class VersionTestDataV1
{
    public int saveVersion = 1;
    public string playerName;
    public int level;
    public float health;
    public int gold;

    public override string ToString() =>
        $"V1(saveVersion {saveVersion}, playerName {playerName}, level {level}, health {health}, gold {gold})";
}

/// <summary>
/// The "new" save format compared to V1: level renamed to playerLevel, health changed from float to string,
/// gold removed and mana added (with a default of 50).
/// </summary>
[Serializable]
public class VersionTestDataV2
{
    public const int Version = 2;

    public int saveVersion = Version;
    public string playerName;
    public int playerLevel;
    public string health;
    public int mana = 50;

    public override string ToString() =>
        $"V2(saveVersion {saveVersion}, playerName {playerName}, playerLevel {playerLevel}, " +
        $"health \"{health}\", mana {mana})";
}

[Serializable]
public class LargeSaveData
{
    public string playerName;
    public List<PetData> pets = new();

    public static LargeSaveData Create(int petCount)
    {
        var data = new LargeSaveData { playerName = "BigSave" };
        for (int i = 0; i < petCount; i++)
        {
            data.pets.Add(new PetData { name = "Pet" + i, age = i % 100, health = i % 100 });
        }

        return data;
    }
}
