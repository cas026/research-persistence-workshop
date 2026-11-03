using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public string Name = "Player";
    public float Health = 100f;
    public int Coins;

    // No Transform: QuickSave can write it but cannot read it back.
    public Vector3 Position;
    public Quaternion Rotation = Quaternion.identity;
}

public class InventoryItem
{
    public string Name;
    public int Amount;
}

public class SlotSummary
{
    public string PlayerName;
    public int Coins;
    public DateTime SavedAt;
}

public class SaveGame
{
    public PlayerData Player = new PlayerData();
    public List<InventoryItem> Inventory = new List<InventoryItem>();
    public List<string> CollectedIds = new List<string>();
}
