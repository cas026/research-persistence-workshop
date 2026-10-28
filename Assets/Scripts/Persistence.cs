using System;
using System.IO;
using UnityEngine;

public class Persistence<T>
{
    private readonly string saveFolder = Application.persistentDataPath;
    private readonly IEncoder<T> encoder;

    public Persistence(IEncoder<T> encoder)
    {
        this.encoder = encoder;
    }

    private string SavePath =>
        Path.Combine(saveFolder, "save" + encoder.GetExtension());

    
    public void Save(T data)
    {
        var encodedData = encoder.Encode(data);

        Debug.Log(saveFolder);

        File.WriteAllBytes(SavePath, encodedData);
    }

    /// <summary>
    /// Returns the data if present at the location, or null if not present.
    /// </summary>
    /// <returns>The data if present at the location, or null if not present</returns>
    public T Load()
    {
        if (!File.Exists(SavePath))
        {
            return default;
        }

        var encodedData = File.ReadAllBytes(SavePath);
        return encoder.Decode(encodedData);
    }
}