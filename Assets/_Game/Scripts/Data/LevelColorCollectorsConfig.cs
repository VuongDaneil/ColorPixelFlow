using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelColorCollectorsConfig", menuName = "ColorPixelFlow/Level Color Collectors Config", order = 1)]
public class LevelColorCollectorsConfig : ScriptableObject
{
    [Header("Collector Squad Configuration")]
    public List<SingleColorCollectorObject> CollectorSetups; // list of SingleColorCollectorObject in a level

    [Header("Formation Settings")]
    public int NumberOfColumns; // this squad's number of column
}