using Encoders;
using UnityEngine;

public class PersistenceManager : MonoBehaviour
{
    private PlayerData playerData;
    private Persistence<PlayerData> persistence;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // persistence = new Persistence<PlayerData>(new JsonEncoder<PlayerData>());
        // persistence = new Persistence<PlayerData>(new XmlEncoder<PlayerData>());
        persistence = new Persistence<PlayerData>(new BinaryEncoder<PlayerData>());
        
        var data = persistence.Load();

        if (data is not null)
        {
            playerData = data;
            Debug.Log("Loaded PlayerData: " + playerData);
        }
        else
        {
            playerData = new PlayerData
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
            Debug.Log("Couldn't load player data, used default: " + playerData);
        }
    }

    void OnClick()
    {
        persistence.Save(playerData);
        Debug.Log("Saved PlayerData");
    }

    void OnRightClick()
    {
        playerData = null;
        var data = persistence.Load();
        playerData = data;
        Debug.Log("Loaded PlayerData: " + data);
    }

    void OnMiddleClick()
    {
        playerData.level += 1;
        playerData.health -= 10;
        Debug.Log("Edited Level: " + playerData.level + " Health: " + playerData.health);
    }
}
