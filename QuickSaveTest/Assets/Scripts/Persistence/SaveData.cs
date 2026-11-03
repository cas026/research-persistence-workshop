using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public string Name = "Speler";
    public float Health = 100f;
    public int Coins;

    // Geen Transform: QuickSave kan die wel schrijven maar niet teruglezen.
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
