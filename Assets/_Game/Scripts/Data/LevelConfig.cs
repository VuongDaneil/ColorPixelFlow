using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig", order = 1)]
public class LevelConfig : ScriptableObject
{
    public PaintingConfig BlocksPaintingConfig;
    public LevelColorCollectorsConfig CollectorsConfig;

    public List<string> ColorsUsed = new List<string>();
}