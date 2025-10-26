using NaughtyAttributes;
using UnityEngine;

public class MoveCollector : MonoBehaviour
{
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    public int MoveIndexOne;
    public int MoveIndexTwo;
    [ReadOnly] public ColorPixelsCollectorObject Gunner0ne;
    [ReadOnly] public ColorPixelsCollectorObject GunnerTwo;

    private void OnValidate()
    {
        if (LevelCollectorsSetup == null) return;
        if (LevelCollectorsSetup.previewSystem.CurrentCollectors.Count <= 0) return;
        if (MoveIndexOne < 0 || MoveIndexOne >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        if (MoveIndexTwo < 0 || MoveIndexTwo >= LevelCollectorsSetup.previewSystem.CurrentCollectors.Count) return;
        Gunner0ne = LevelCollectorsSetup.previewSystem.CurrentCollectors[MoveIndexOne];
        GunnerTwo = LevelCollectorsSetup.previewSystem.CurrentCollectors[MoveIndexTwo];
    }

    [Button("MOVE")]
    public void Move()
    {
        if (Gunner0ne == null || GunnerTwo == null) return;


        LevelCollectorsSetup.InsertAmongOtherCollector(Gunner0ne, GunnerTwo, IsInFrontOf(Gunner0ne.transform, GunnerTwo.transform));

        Gunner0ne.VisualHandler.RefreshColor();
        GunnerTwo.VisualHandler.RefreshColor();

        LevelCollectorsSetup.ImportCollectorsFromScene();
        LevelCollectorsSetup.BakeCollectorsPositionInTool();
    }

    public void Move(ColorPixelsCollectorObject first, ColorPixelsCollectorObject second)
    {
        if (first == null || second == null) return;

        LevelCollectorsSetup.InsertAmongOtherCollector(first, second, IsInFrontOf(first.transform, second.transform));

        first.VisualHandler.RefreshColor();
        second.VisualHandler.RefreshColor();

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
