using UnityEngine;
using CI.QuickSave;

public class Cube : MonoBehaviour
{
    private string saveName = "PlayerData";

    void Update()
    {
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * 5f;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * 5f;
        transform.Translate(x, 0, z);
    }

    public void SavePosition()
    {
        QuickSaveWriter.Create(saveName)
            .Write("MyPosition", transform.position)
            
            // De JSON format: { "x": 1.5, "y": 0.0, "z": -2.3 }
            .Commit();
                           
        Debug.Log("<color=green>Position saved</color>");
    }

    public void LoadPosition()
    {
        if (QuickSaveWriter.RootExists(saveName))
        {
            transform.position = QuickSaveReader.Create(saveName).Read<Vector3>("MyPosition");
            Debug.Log("<color=cyan>Position loaded</color>");
        }
        else
        {
            Debug.LogWarning("No save file found to load.");
        }
    }
}