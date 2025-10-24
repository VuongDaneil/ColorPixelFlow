using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class CombineCollector : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int SwapIndexOne;
    public int SwapIndexTwo;
    [ReadOnly] public ColorPixelsCollector Gunner0ne;
    [ReadOnly] public ColorPixelsCollector GunnerTwo;

    private void OnValidate()
    {
        if (LevelCollectorsSetup == null) return;
        if (LevelCollectorsSetup.previewSystem.CurrentCollectors.Count <= 0) return;
        if (SwapIndexOne < 0 || SwapIndexOne >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        if (SwapIndexTwo < 0 || SwapIndexTwo >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        Gunner0ne = LevelCollectorsSetup.previewSystem.CurrentCollectors[SwapIndexOne];
        GunnerTwo = LevelCollectorsSetup.previewSystem.CurrentCollectors[SwapIndexTwo];
    }

    [Button("SWAP")]
    public void Swap()
    {
        if (Gunner0ne == null || GunnerTwo == null) return;

        bool gunnerOneLocked = Gunner0ne.IsLocked;
        bool gunnerOneHidden = Gunner0ne.IsHidden;
        int gunnerOneIndex = Gunner0ne.BulletCapacity;
        string gunnerOneColor = Gunner0ne.CollectorColor;
        Color gunnerOnecCurrentColor = Gunner0ne.VisualHandler.CurrentColor;
        List<int> connectedCollectorOne = new List<int>(Gunner0ne.ConnectedCollectorsIndex);

        bool gunnerTwoLocked = GunnerTwo.IsLocked;
        bool gunnerTwoHidden = GunnerTwo.IsHidden;
        int gunnerTwoIndex = GunnerTwo.BulletCapacity;
        string gunnerTwoColor = GunnerTwo.CollectorColor;
        Color gunnerTwocCurrentColor = GunnerTwo.VisualHandler.CurrentColor;
        List<int> connectedCollectorTwo = new List<int>(GunnerTwo.ConnectedCollectorsIndex);

        Gunner0ne.BulletCapacity = gunnerTwoIndex;
        Gunner0ne.CollectorColor = gunnerTwoColor;
        Gunner0ne.IsLocked = gunnerTwoLocked;
        Gunner0ne.IsHidden = gunnerTwoHidden;
        Gunner0ne.VisualHandler.CurrentColor = gunnerTwocCurrentColor;

        GunnerTwo.BulletCapacity = gunnerOneIndex;
        GunnerTwo.CollectorColor = gunnerOneColor;
        GunnerTwo.IsLocked = gunnerOneLocked;
        GunnerTwo.IsHidden = gunnerOneHidden;
        GunnerTwo.VisualHandler.CurrentColor = gunnerOnecCurrentColor;

        Gunner0ne.VisualHandler.RefreshColor();
        GunnerTwo.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }
}
