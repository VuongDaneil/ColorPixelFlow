using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitCollector : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int SplitIndex;
    [ReadOnly] public ColorPixelsCollectorObject TargetGunner;

    private void OnValidate()
    {
        if (LevelCollectorsSetup == null) return;
        if (LevelCollectorsSetup.previewSystem.CurrentCollectors.Count <= 0) return;
        if (SplitIndex < 0 || SplitIndex >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        TargetGunner = LevelCollectorsSetup.previewSystem.CurrentCollectors[SplitIndex];
    }

    [Button("SPLIT")]
    public void Split()
    {
        if (TargetGunner == null) return;

        LevelCollectorsSetup.SplitACollector(TargetGunner);

        TargetGunner.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
        LevelCollectorsSetup.BakeCollectorsPositionInTool();
    }

    public void Split(ColorPixelsCollectorObject target)
    {
        if (target == null) return;

        LevelCollectorsSetup.SplitACollector(target);

        target.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
        LevelCollectorsSetup.BakeCollectorsPositionInTool();
    }

    public bool IsInFrontOf(Transform first, Transform second)
    {
        Vector3 dirToFirst = first.position - second.position;
        float dot = Vector3.Dot(second.forward, dirToFirst);
        return dot > 0f;
    }
}
