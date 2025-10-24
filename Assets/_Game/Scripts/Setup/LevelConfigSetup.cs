using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class LevelConfigSetup : MonoBehaviour
{
    #region PROPERTIES
    [Header("LEVEL")]
    public PaintingConfig CurrentLevelPaintingConfig;
    public LevelColorCollectorsConfig CurrentLevelCollectorConfig;

    [Header("CONTROLLER(s)")]
    public PaintingGridObject CurrentGridObject;
    public PaintingConfigSetup PaintingSetup;
    public PipeObjectConfigSetup PipeObjectSetup;
    
    [Space]
    public WallObjectConfigSetup WallObjectSetup;

    [Space]
    public LevelCollectorsSystem LevelCollectorsManager;
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    #endregion

    #region UNITY CORE
    [Button("Refresh")]
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (CurrentLevelPaintingConfig != null)
        {
            CurrentGridObject.paintingConfig = CurrentLevelPaintingConfig;
            PaintingSetup.resultPaintingConfig = CurrentLevelPaintingConfig;
            PipeObjectSetup.CurrentLevelObjectSetups = CurrentLevelPaintingConfig.PipeSetups;
        }

        if (CurrentLevelCollectorConfig != null)
        {
            LevelCollectorsSetup.configAsset = CurrentLevelCollectorConfig;
            LevelCollectorsSetup.paintingConfig = CurrentLevelPaintingConfig;
            LevelCollectorsManager.CurrentLevelCollectorsConfig = CurrentLevelCollectorConfig;
        }

        if (WallObjectSetup.gridObject != null)
        {
            WallObjectSetup.CurrentLevelWallObjectSetups = CurrentLevelPaintingConfig.WallSetups;
        }
    }
    #endregion

    #region MAIN
    [Button("LOAD LEVEL")]
    public void SetupLevel()
    {
        LevelCollectorsManager.SetupCollectors();
        CurrentGridObject.ApplyPaintingConfig();
    }

    [Button("CLEAR LEVEL")]
    public void ClearLevel()
    {
        LevelCollectorsManager.ClearExistingCollectors();
        CurrentGridObject.ClearAllPipe();
        CurrentGridObject.ClearAllWall();
        CurrentGridObject.ClearToWhite();
    }
    #endregion
}