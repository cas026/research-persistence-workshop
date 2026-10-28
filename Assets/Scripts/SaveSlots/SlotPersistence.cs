using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Like Persistence&lt;T&gt;, but with one file per slot: [folder]/slot_[n][suffix][extension].
/// Wraps the existing IEncoder&lt;T&gt; so Persistence&lt;T&gt; itself stays untouched.
/// </summary>
public class SlotPersistence<T>
{
    private const string FilePrefix = "slot_";

    private readonly IEncoder<T> encoder;
    private readonly string suffix;

    public string SaveFolder { get; }

    /// <param name="encoder">Encoder that decides the file format.</param>
    /// <param name="suffix">Extra part of the file name, e.g. ".meta" gives slot_0.meta.json.</param>
    /// <param name="folderName">Sub folder of Application.persistentDataPath.</param>
    public SlotPersistence(IEncoder<T> encoder, string suffix = "", string folderName = "saves")
    {
        this.encoder = encoder;
        this.suffix = suffix;
        SaveFolder = Path.Combine(Application.persistentDataPath, folderName);
        Directory.CreateDirectory(SaveFolder);
    }

    public string GetPath(int slot) =>
        Path.Combine(SaveFolder, FilePrefix + slot + suffix + encoder.GetExtension());

    public bool Exists(int slot) => File.Exists(GetPath(slot));

    public bool Save(int slot, T data)
    {
        try
        {
            File.WriteAllBytes(GetPath(slot), encoder.Encode(data));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Saving slot {slot} to {GetPath(slot)} failed: {e}");
            return false;
        }
    }

    /// <summary>
    /// Loads a slot without throwing: Empty if there is no file, Corrupt if it can't be read or decoded.
    /// </summary>
    public SlotState TryLoad(int slot, out T data)
    {
        data = default;
        var path = GetPath(slot);

        if (!File.Exists(path))
        {
            return SlotState.Empty;
        }

        try
        {
            var encodedData = File.ReadAllBytes(path);
            if (encodedData.Length == 0)
            {
                Debug.LogWarning($"Slot {slot} is corrupt: {path} is empty (0 bytes)");
                return SlotState.Corrupt;
            }

            data = encoder.Decode(encodedData);
            if (data == null)
            {
                Debug.LogWarning($"Slot {slot} is corrupt: decoding {path} returned null");
                return SlotState.Corrupt;
            }

            return SlotState.Ok;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Slot {slot} is corrupt: {e.GetType().Name}: {e.Message}");
            data = default;
            return SlotState.Corrupt;
        }
    }

    /// <returns>True if a file was deleted.</returns>
    public bool Delete(int slot)
    {
        var path = GetPath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Deleting slot {slot} ({path}) failed: {e}");
            return false;
        }
    }

    /// <summary>
    /// All slot numbers that have a file on disk, sorted. Files that don't match the
    /// naming scheme (e.g. slot_abc.json, or slot_0.meta.json for the data files) are skipped.
    /// </summary>
    public List<int> GetAllSlots()
    {
        var slots = new List<int>();
        var extension = encoder.GetExtension();

        foreach (var file in Directory.GetFiles(SaveFolder, FilePrefix + "*" + suffix + extension))
        {
            var fileName = Path.GetFileName(file);
            var numberLength = fileName.Length - FilePrefix.Length - suffix.Length - extension.Length;
            if (numberLength <= 0 || !fileName.EndsWith(suffix + extension))
            {
                continue;
            }

            if (int.TryParse(fileName.Substring(FilePrefix.Length, numberLength), out var slot))
            {
                slots.Add(slot);
            }
        }

        slots.Sort();
        return slots;
    }
}
