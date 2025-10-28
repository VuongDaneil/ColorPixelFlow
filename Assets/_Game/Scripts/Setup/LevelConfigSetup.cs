using static PaintingSharedAttributes;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class LevelConfigSetup : MonoBehaviour
{
    #region PROPERTIES
    [Space]
    [Header("INPUT(s)")]
    public Sprite NewTargetPainting;
    public List<string> ColorCodesUsed = new List<string>();

    [Header("LEVEL DATA(s)")]
    public LevelConfig CurrentLevel;
    [ReadOnly] public PaintingConfig CurrentLevelPaintingConfig;
    [ReadOnly] public LevelColorCollectorsConfig CurrentLevelCollectorConfig;
    [ReadOnly] public Sprite CurrentLevelPainting;
    [ReadOnly] public List<string> CurrentLevelColorCodes = new List<string>();

    [Header("CONTROLLER(s)")]
    public PaintingGridObject CurrentGridObject;
    public PaintingConfigSetup PaintingSetup;
    public PaintingAdvancedSetup PaintingAdvancedSetup;
    public PipeObjectConfigSetup PipeObjectSetup;
    
    [Space]
    public WallObjectConfigSetup WallObjectSetup;

    [Space]
    public KeyObjectConfigSetup KeyObjectSetup;

    [Space]
    public LevelCollectorsSystem LevelCollectorsManager;
    public LevelCollectorsConfigSetup LevelCollectorsSetup;
    #endregion

    #region UNITY CORE
    [Button("Refresh")]
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        SetUpComponents();
    }

    private void Awake()
    {
        SetUpComponents();
    }

    private void Start()
    {
        LoadLevel();
    }
    #endregion

    #region MAIN
    [Button("LOAD LEVEL")]
    public void LoadLevel()
    {
        ClearLevel();
        LevelCollectorsManager.SetupCollectors();
        CurrentGridObject.InitializeLevel();
    }

    [Button("CLEAR LEVEL")]
    public void ClearLevel()
    {
        LevelCollectorsManager.ClearExistingCollectors();
        CurrentGridObject.ClearAllKeys();
        CurrentGridObject.ClearAllPipes();
        CurrentGridObject.ClearAllWalls();
        CurrentGridObject.ClearToWhite();
    }

    /// <summary>
    /// Dont mind this, leave it alone
    /// </summary>
    public void SetUpComponents()
    {
        if (CurrentLevel == null)
        {
            CurrentLevelPaintingConfig = null;
            CurrentLevelCollectorConfig = null;
            CurrentLevelPainting = null;
            return;
        }

        CurrentLevelColorCodes = new List<string>(CurrentLevel.ColorsUsed);
        CurrentLevelCollectorConfig = CurrentLevel.CollectorsConfig;
        CurrentLevelPaintingConfig = CurrentLevel.BlocksPaintingConfig;

        if (CurrentLevelPaintingConfig != null) CurrentLevelPainting = CurrentLevelPaintingConfig.Sprite;

        if (PaintingSetup)
        {
            PaintingSetup.CurrentGridObject = CurrentGridObject;
            PaintingSetup.CurrentPaintingConfig = CurrentLevelPaintingConfig;
        }

        if (PaintingAdvancedSetup)
        {
            PaintingAdvancedSetup.CurrentLevelPaintingConfig = CurrentLevelPaintingConfig;
        }

        if (PipeObjectSetup)
        {
            PipeObjectSetup.CurrentLevelObjectSetups = CurrentLevelPaintingConfig.PipeSetups;
        }

        if (CurrentGridObject)
        {
            CurrentGridObject.paintingConfig = CurrentLevelPaintingConfig;
        }

        if (WallObjectSetup)
        {
            WallObjectSetup.CurrentLevelWallObjectSetups = CurrentLevelPaintingConfig.WallSetups;
        }

        if (LevelCollectorsManager)
        {
            LevelCollectorsManager.CurrentLevelCollectorsConfig = CurrentLevelCollectorConfig;
        }

        if (LevelCollectorsSetup)
        {
            LevelCollectorsSetup.configAsset = CurrentLevelCollectorConfig;
            LevelCollectorsSetup.paintingConfig = CurrentLevelPaintingConfig;
        }

        if (KeyObjectSetup)
        {
            KeyObjectSetup.CurrentLevelKeyObjectSetups = CurrentLevelPaintingConfig.KeySetups;
        }
    }

#if UNITY_EDITOR
    [Button("CREATE NEW LEVEL")]
    public void CreateNewLevel()
    {
        if (NewTargetPainting == null) return;

        var existingConfig = GetLevelConfig(NewTargetPainting);

        if (existingConfig != null)
        {
            CurrentLevel = existingConfig;
            PaintingSetup.ColorCodeInUse = new List<string>(existingConfig.ColorsUsed);
            SetUpComponents();
            return;
        }

        PaintingSetup.ColorCodeInUse = new List<string>(ColorCodesUsed);
        PaintingSetup.TargetPainting = NewTargetPainting;
        PaintingSetup.SamplePaintingToGrid(NewTargetPainting);

        string newCollectorConfigName = NewTargetPainting.name;
        var collectorConfig = LevelCollectorsSetup.CreateConfigAsset(newCollectorConfigName);

        var newLvl = CreateConfigAsset(NewTargetPainting.name + "_LevelConfig", PaintingSetup.CurrentPaintingConfig, collectorConfig);
        CurrentLevel = newLvl;
        SetUpComponents();
    }

    public LevelConfig CreateConfigAsset(string configName, PaintingConfig paintingConfig, LevelColorCollectorsConfig collectorConfig)
    {
        if (string.IsNullOrEmpty(configName))
        {
            Debug.LogError("Config name cannot be empty!");
            return null;
        }

        // Ensure the path ends with a slash
        if (!LevelConfigPath.EndsWith("/"))
        {
            LevelConfigPath += "/";
        }

        // Create the directory if it doesn't exist
        if (!Directory.Exists(LevelConfigPath))
        {
            Directory.CreateDirectory(LevelConfigPath);
        }

        string assetPath = LevelConfigPath + configName + ".asset";

        LevelConfig newConfig = ScriptableObject.CreateInstance<LevelConfig>();
        newConfig.BlocksPaintingConfig = paintingConfig;
        newConfig.CollectorsConfig = collectorConfig;
        newConfig.ColorsUsed = new List<string>(ColorCodesUsed);
        AssetDatabase.CreateAsset(newConfig, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created new LevelColorCollectorsConfig asset at {assetPath}");
        return newConfig;
    }
#endif

    #endregion
}