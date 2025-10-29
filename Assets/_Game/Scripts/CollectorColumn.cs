using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CollectorColumn
{
    public List<CollectorMachanicObjectBase> CollectorsInColumn;
    
    public CollectorColumn()
    {
        CollectorsInColumn = new List<CollectorMachanicObjectBase>();
    }
}