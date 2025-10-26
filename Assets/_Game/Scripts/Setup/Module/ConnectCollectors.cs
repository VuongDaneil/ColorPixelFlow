using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectCollectors : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int ConnectIndexOne;
    public int ConnectIndexTwo;
    [ReadOnly] public ColorPixelsCollectorObject Gunner0ne;
    [ReadOnly] public ColorPixelsCollectorObject GunnerTwo;

    private void OnValidate()
    {
        if (LevelCollectorsSetup == null) return;
        if (LevelCollectorsSetup.previewSystem.CurrentCollectors.Count <= 0) return;
        if (ConnectIndexOne < 0 || ConnectIndexOne >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        if (ConnectIndexTwo < 0 || ConnectIndexTwo >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        Gunner0ne = LevelCollectorsSetup.previewSystem.CurrentCollectors[ConnectIndexOne];
        GunnerTwo = LevelCollectorsSetup.previewSystem.CurrentCollectors[ConnectIndexTwo];
    }

    [Button("CONNECT")]
    public void Connect()
    {
        if (Gunner0ne == null || GunnerTwo == null) return;

        if (Gunner0ne.ConnectedCollectorsIndex.Contains())

        Gunner0ne.VisualHandler.RefreshColor();
        GunnerTwo.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }

    public void Connect(ColorPixelsCollectorObject first, ColorPixelsCollectorObject second)
    {
        if (first == null || second == null) return;


        first.VisualHandler.RefreshColor();
        second.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }
}
