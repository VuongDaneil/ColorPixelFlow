using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class CombinesCollector : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int CombineIndexOne;
    public int CombineIndexTwo;
    [ReadOnly] public ColorPixelsCollectorObject Gunner0ne;
    [ReadOnly] public ColorPixelsCollectorObject GunnerTwo;

    private void OnValidate()
    {
        if (LevelCollectorsSetup == null) return;
        if (LevelCollectorsSetup.previewSystem.CurrentCollectors.Count <= 0) return;
        if (CombineIndexOne < 0 || CombineIndexOne >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        if (CombineIndexTwo < 0 || CombineIndexTwo >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        Gunner0ne = LevelCollectorsSetup.previewSystem.CurrentCollectors[CombineIndexOne];
        GunnerTwo = LevelCollectorsSetup.previewSystem.CurrentCollectors[CombineIndexTwo];
    }

    [Button("COMBINE")]
    public void Combine()
    {
        if (Gunner0ne == null || GunnerTwo == null) return;
        if (Gunner0ne.CollectorColor != GunnerTwo.CollectorColor) return;

        GunnerTwo.BulletCapacity += Gunner0ne.BulletCapacity;

        Gunner0ne.VisualHandler.RefreshColor();
        GunnerTwo.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }

    public void Combine(ColorPixelsCollectorObject first, ColorPixelsCollectorObject second)
    {
        if (first == null || second == null) return;
        if (first.CollectorColor != second.CollectorColor) return;

        second.BulletCapacity += first.BulletCapacity;

        LevelCollectorsSetup.previewSystem.RemoveCollector(first);
        LevelCollectorsSetup.ImportCollectorsFromScene();
        LevelCollectorsSetup.BakeCollectorsPositionInTool();
    }
}
