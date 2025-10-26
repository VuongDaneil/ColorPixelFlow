using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SingleColorCollectorConfig
{
    [Header("Color and Ammunition")]
    public string ColorCode; // Color code that this collector can collect
    public int Bullets; // Number of bullets, each of them can collect one color

    [Header("Collector State")]
    public bool Locked; // Locked or not
    public bool Hidden; // Hide information about color and bullet from player

    [Header("Connections")]
    public List<int> ConnectedCollectorsIndex; // Original index of other collectors that connected to this collector

    public SingleColorCollectorConfig()
    {
        ConnectedCollectorsIndex = new List<int>();
    }

    public SingleColorCollectorConfig(string colorCode, int bullets, bool locked = false, bool hidden = false)
    {
        ColorCode = colorCode;
        Bullets = bullets;
        Locked = locked;
        Hidden = hidden;
        ConnectedCollectorsIndex = new List<int>();
    }
}