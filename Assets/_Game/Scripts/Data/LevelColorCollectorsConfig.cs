using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelColorCollectorsConfig", menuName = "ColorPixelFlow/Level Color Collectors Config", order = 1)]
public class LevelColorCollectorsConfig : ScriptableObject
{
    [Header("Collector Squad Configuration")]
    public List<ColumnOfCollectorConfig> CollectorColumns; // list of SingleColorCollectorObject in a level

    public int NumberOfColumns()
    {
        return CollectorColumns.Count;
    }

    public int NumberOfCollectors()
    {
        int num = 0;
        foreach (var col in CollectorColumns)
        {
            num += col.Collectors.Count;
        }
        return num;
    }

    public int NumberOfLocks()
    {
        int num = 0;
        foreach (var col in CollectorColumns)
        {
            num += col.Locks.Count;
        }
        return num;
    }

    public List<SingleColorCollectorConfig> GetAllCollectorConfigs()
    {
        List<SingleColorCollectorConfig> rs = new List<SingleColorCollectorConfig>();
        foreach (var col in CollectorColumns)
        {
            rs.AddRange(col.Collectors);
        }

        return rs;
    }
}

[Serializable]
public class ColumnOfCollectorConfig
{
    public List<SingleColorCollectorConfig> Collectors;
    public List<LockObjectConfig> Locks;

    public ColumnOfCollectorConfig()
    {
        Collectors = new List<SingleColorCollectorConfig>();
        Locks = new List<LockObjectConfig>();
    }
}