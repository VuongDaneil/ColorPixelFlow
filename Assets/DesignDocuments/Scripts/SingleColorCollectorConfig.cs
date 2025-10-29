using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SingleColorCollectorConfig
{
    public int ID = -1;

    [Header("Color and Ammunition")]
    public string ColorCode; // Color code that this collector can collect
    public int Bullets; // Number of bullets, each of them can collect one color

    [Header("Collector State")]
    public bool Locked; // Locked or not
    public bool Hidden; // Hide information about color and bullet from player

    [Header("Connections")]
    public List<int> ConnectedCollectorsIDs; // Original index of other collectors that connected to this collector

    public SingleColorCollectorConfig()
    {
        ConnectedCollectorsIDs = new List<int>();
    }

    public SingleColorCollectorConfig(string colorCode, int bullets, bool locked = false, bool hidden = false)
    {
        ID = -1;
        ColorCode = colorCode;
        Bullets = bullets;
        Locked = locked;
        Hidden = hidden;
        ConnectedCollectorsIDs = new List<int>();
    }
}

[Serializable]
public class LockObjectConfig
{
    public int ID = -1;
    public int Row = -1;

    public LockObjectConfig()
    {
        ID = -1;
        Row = -1;
    }
}