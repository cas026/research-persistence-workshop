using System;

[Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public float health;
    public PetData pet;

    public override string ToString()
    {
        return "PlayerData: " + playerName + " (level " + level + ", " + health + "hp, has pet " + pet + ")";
    }
}