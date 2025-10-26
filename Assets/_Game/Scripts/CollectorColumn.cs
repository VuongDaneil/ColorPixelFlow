using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CollectorColumn
{
    public List<ColorPixelsCollectorObject> CollectorsInColumn;
    
    public CollectorColumn()
    {
        CollectorsInColumn = new List<ColorPixelsCollectorObject>();
    }
}