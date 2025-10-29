using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class SwapCollectors : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int SwapIndexOne;
    public int SwapIndexTwo;
    [ReadOnly] public ColorPixelsCollectorObject Gunner0ne;
    [ReadOnly] public ColorPixelsCollectorObject GunnerTwo;

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
        int gunnerOneBullets = Gunner0ne.BulletCapacity;
        string gunnerOneColor = Gunner0ne.CollectorColor;
        Color gunnerOnecCurrentColor = Gunner0ne.VisualHandler.CurrentColor;

        bool gunnerTwoLocked = GunnerTwo.IsLocked;
        bool gunnerTwoHidden = GunnerTwo.IsHidden;
        int gunnerTwoBullets = GunnerTwo.BulletCapacity;
        string gunnerTwoColor = GunnerTwo.CollectorColor;
        Color gunnerTwocCurrentColor = GunnerTwo.VisualHandler.CurrentColor;

        Gunner0ne.IsLocked = gunnerTwoLocked;
        Gunner0ne.IsHidden = gunnerTwoHidden;
        Gunner0ne.CollectorColor = gunnerTwoColor;
        Gunner0ne.BulletCapacity = gunnerTwoBullets;
        Gunner0ne.VisualHandler.CurrentColor = gunnerTwocCurrentColor;

        GunnerTwo.IsLocked = gunnerOneLocked;
        GunnerTwo.IsHidden = gunnerOneHidden;
        GunnerTwo.CollectorColor = gunnerOneColor;
        GunnerTwo.BulletCapacity = gunnerOneBullets;
        GunnerTwo.VisualHandler.CurrentColor = gunnerOnecCurrentColor;

        Gunner0ne.VisualHandler.RefreshColor();
        GunnerTwo.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }

    public void Swap(CollectorMachanicObjectBase first, CollectorMachanicObjectBase second)
    {
        //if (first == null || second == null) return;

        //bool gunnerOneLocked = first.IsLocked;
        //bool gunnerOneHidden = first.IsHidden;
        //int gunnerOneIndex = first.BulletCapacity;
        //string gunnerOneColor = first.CollectorColor;
        //Color gunnerOnecCurrentColor = first.VisualHandler.CurrentColor;

        //bool gunnerTwoLocked = second.IsLocked;
        //bool gunnerTwoHidden = second.IsHidden;
        //int gunnerTwoIndex = second.BulletCapacity;
        //string gunnerTwoColor = second.CollectorColor;
        //Color gunnerTwocCurrentColor = second.VisualHandler.CurrentColor;

        //first.BulletCapacity = gunnerTwoIndex;
        //first.CollectorColor = gunnerTwoColor;
        //first.IsLocked = gunnerTwoLocked;
        //first.IsHidden = gunnerTwoHidden;
        //first.VisualHandler.CurrentColor = gunnerTwocCurrentColor;

        //second.BulletCapacity = gunnerOneIndex;
        //second.CollectorColor = gunnerOneColor;
        //second.IsLocked = gunnerOneLocked;
        //second.IsHidden = gunnerOneHidden;
        //second.VisualHandler.CurrentColor = gunnerOnecCurrentColor;

        //first.VisualHandler.RefreshColor();
        //second.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
    }
}