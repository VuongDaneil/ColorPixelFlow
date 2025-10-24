using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CollectorColumn
{
    public List<ColorPixelsCollector> CollectorsInColumn;
    
    public CollectorColumn()
    {
        CollectorsInColumn = new List<ColorPixelsCollector>();
    }
}