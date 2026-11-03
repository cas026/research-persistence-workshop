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
        
        // Use quicksave to save data location position
                           
    }

    public void LoadPosition()
    {
        
        // Use quicksave to load data location position

    }
}