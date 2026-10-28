using System;
using Encoders;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the current PlayerData + play time and saves/loads/deletes it in a fixed number of slots.
/// Every slot is two files: slot_n.json (PlayerData) and slot_n.meta.json (SlotMetadata).
/// Has a minimal IMGUI slot menu and [ContextMenu] actions for testing without UI.
/// </summary>
public class SaveSlotManager : MonoBehaviour
{
    public const int CurrentSaveVersion = 1;

    [SerializeField] private int slotCount = 5;
    [SerializeField] private bool showGui = true;
    [Tooltip("Also decode the save data when refreshing the overview, so corrupt data files show up. " +
             "Costs more time with large saves; the metadata alone is much cheaper to read.")]
    [SerializeField] private bool validateDataInOverview = true;

    private SlotPersistence<PlayerData> dataPersistence;
    private SlotPersistence<SlotMetadata> metaPersistence;

    private PlayerData playerData;
    private float playTimeSeconds;

    // Cached because OnGUI runs several times per frame; refreshed after every slot action
    private SlotState[] slotStates;
    private SlotMetadata[] slotMetadata;
    private string status = "";

    public int SlotCount => slotCount;
    public PlayerData PlayerData => playerData;

    void Awake()
    {
        EnsureInitialized();
        Debug.Log("Save slots folder: " + dataPersistence.SaveFolder);
    }

    void Update()
    {
        playTimeSeconds += Time.unscaledDeltaTime;
    }

    // [ContextMenu] methods can also run outside Play mode, before Awake
    private void EnsureInitialized()
    {
        if (dataPersistence != null)
        {
            return;
        }

        dataPersistence = new SlotPersistence<PlayerData>(new JsonEncoder<PlayerData>());
        metaPersistence = new SlotPersistence<SlotMetadata>(new JsonEncoder<SlotMetadata>(), ".meta");
        playerData = CreateDefaultData();
        RefreshSlotInfo();
    }

    private static PlayerData CreateDefaultData()
    {
        return new PlayerData
        {
            playerName = "Player2",
            pet = new PetData
            {
                name = "Peter",
                age = 58,
                health = 100
            },
            level = 5,
            health = 100f
        };
    }

    public string GetDataPath(int slot)
    {
        EnsureInitialized();
        return dataPersistence.GetPath(slot);
    }

    public string GetMetadataPath(int slot)
    {
        EnsureInitialized();
        return metaPersistence.GetPath(slot);
    }

    public bool SaveToSlot(int slot)
    {
        EnsureInitialized();

        var metadata = new SlotMetadata
        {
            saveVersion = CurrentSaveVersion,
            savedAtTicks = DateTime.Now.Ticks,
            playTimeSeconds = playTimeSeconds,
            sceneName = SceneManager.GetActiveScene().name,
            playerName = playerData.playerName,
            level = playerData.level
        };

        var saved = dataPersistence.Save(slot, playerData) && metaPersistence.Save(slot, metadata);
        SetStatus(saved ? $"Saved slot {slot}: {playerData}" : $"Saving slot {slot} failed");
        RefreshSlotInfo();
        return saved;
    }

    public bool LoadFromSlot(int slot)
    {
        EnsureInitialized();

        var state = dataPersistence.TryLoad(slot, out var data);
        if (state != SlotState.Ok)
        {
            SetStatus($"Can't load slot {slot}: {state}");
            RefreshSlotInfo();
            return false;
        }

        playerData = data;

        if (metaPersistence.TryLoad(slot, out var metadata) == SlotState.Ok)
        {
            playTimeSeconds = metadata.playTimeSeconds;
            if (metadata.saveVersion != CurrentSaveVersion)
            {
                Debug.LogWarning($"Slot {slot} was saved with version {metadata.saveVersion}, " +
                                 $"current version is {CurrentSaveVersion}");
            }
        }
        else
        {
            Debug.LogWarning($"Slot {slot} has no readable metadata, play time not restored");
        }

        SetStatus($"Loaded slot {slot}: {playerData}");
        return true;
    }

    public void DeleteSlot(int slot)
    {
        EnsureInitialized();

        var deleted = dataPersistence.Delete(slot) | metaPersistence.Delete(slot);
        SetStatus(deleted ? $"Deleted slot {slot}" : $"Slot {slot} was already empty");
        RefreshSlotInfo();
    }

    public void ChangeData()
    {
        EnsureInitialized();

        playerData.level += 1;
        playerData.health -= 10;
        SetStatus($"Edited Level: {playerData.level} Health: {playerData.health}");
    }

    public SlotState GetSlotState(int slot, out SlotMetadata metadata)
    {
        EnsureInitialized();

        metadata = slotMetadata[slot];
        return slotStates[slot];
    }

    public void RefreshSlotInfo()
    {
        EnsureInitialized();

        slotStates = new SlotState[slotCount];
        slotMetadata = new SlotMetadata[slotCount];

        for (int slot = 0; slot < slotCount; slot++)
        {
            var metaState = metaPersistence.TryLoad(slot, out slotMetadata[slot]);
            var dataExists = dataPersistence.Exists(slot);

            if (metaState == SlotState.Empty && !dataExists)
            {
                slotStates[slot] = SlotState.Empty;
            }
            else if (metaState != SlotState.Ok || !dataExists)
            {
                // One of the two files is missing or unreadable
                slotStates[slot] = SlotState.Corrupt;
            }
            else if (validateDataInOverview)
            {
                slotStates[slot] = dataPersistence.TryLoad(slot, out _);
            }
            else
            {
                slotStates[slot] = SlotState.Ok;
            }
        }
    }

    private void SetStatus(string message)
    {
        status = message;
        Debug.Log(message);
    }

    private static string FormatPlayTime(float seconds) =>
        TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

    private string DescribeSlot(int slot)
    {
        var label = $"Slot {slot}: ";
        switch (slotStates[slot])
        {
            case SlotState.Empty:
                return label + "Empty";
            case SlotState.Corrupt:
                return label + "CORRUPT";
            default:
                var metadata = slotMetadata[slot];
                return label + $"{metadata.playerName} lvl {metadata.level} | {metadata.sceneName} | " +
                       $"{new DateTime(metadata.savedAtTicks):dd-MM HH:mm:ss} | {FormatPlayTime(metadata.playTimeSeconds)}";
        }
    }

    void OnGUI()
    {
        if (!showGui || playerData == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 640, Screen.height - 20), GUI.skin.box);

        GUILayout.Label($"Current: {playerData.playerName} | level {playerData.level} | " +
                        $"{playerData.health} hp | play time {FormatPlayTime(playTimeSeconds)}");
        if (GUILayout.Button("Level +1 / Health -10", GUILayout.Width(200)))
        {
            ChangeData();
        }

        GUILayout.Space(10);

        for (int slot = 0; slot < slotCount; slot++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(DescribeSlot(slot), GUILayout.Width(420));

            if (GUILayout.Button("Save"))
            {
                SaveToSlot(slot);
            }

            GUI.enabled = slotStates[slot] == SlotState.Ok;
            if (GUILayout.Button("Load"))
            {
                LoadFromSlot(slot);
            }

            GUI.enabled = slotStates[slot] != SlotState.Empty;
            if (GUILayout.Button("Delete"))
            {
                DeleteSlot(slot);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Refresh (after editing files by hand)", GUILayout.Width(260)))
        {
            RefreshSlotInfo();
        }

        GUILayout.Label(status);
        GUILayout.EndArea();
    }

    [ContextMenu("Save slot 0")] private void SaveSlot0() => SaveToSlot(0);
    [ContextMenu("Save slot 1")] private void SaveSlot1() => SaveToSlot(1);
    [ContextMenu("Load slot 0")] private void LoadSlot0() => LoadFromSlot(0);
    [ContextMenu("Load slot 1")] private void LoadSlot1() => LoadFromSlot(1);
    [ContextMenu("Delete slot 0")] private void DeleteSlot0() => DeleteSlot(0);
    [ContextMenu("Delete slot 1")] private void DeleteSlot1() => DeleteSlot(1);
    [ContextMenu("Change data")] private void ChangeDataMenu() => ChangeData();

    [ContextMenu("Log all slots on disk")]
    private void LogAllSlots()
    {
        EnsureInitialized();
        Debug.Log("Data slots on disk: [" + string.Join(", ", dataPersistence.GetAllSlots()) + "], " +
                  "metadata slots on disk: [" + string.Join(", ", metaPersistence.GetAllSlots()) + "]");
    }

    [ContextMenu("Open save folder")]
    private void OpenSaveFolder()
    {
        EnsureInitialized();
        Application.OpenURL("file://" + dataPersistence.SaveFolder);
    }
}
