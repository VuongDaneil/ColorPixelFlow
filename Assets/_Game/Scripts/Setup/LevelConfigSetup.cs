using static PaintingSharedAttributes;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelConfigSetup : MonoBehaviour
{
    #region PROPERTIES
    [Header("LEVEL DATA(s)")]
    public LevelConfig CurrentLevel;
    public PaintingConfig CurrentLevelPaintingConfig;
    public LevelColorCollectorsConfig CurrentLevelCollectorConfig;
    public Sprite CurrentPainting;

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
        if (CurrentLevel == null) return;

        CurrentLevelCollectorConfig = CurrentLevel.CollectorsConfig;
        CurrentLevelPaintingConfig = CurrentLevel.BlocksPaintingConfig;

        if (PaintingSetup)
        {
            PaintingSetup.CurrentGridObject = CurrentGridObject;
            PaintingSetup.ResultPaintingConfig = CurrentLevelPaintingConfig;
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
        if (CurrentPainting == null) return;

        PaintingSetup.TargetPainting = CurrentPainting;
        PaintingSetup.SamplePaintingToGrid(CurrentPainting);

        string newCollectorConfigName = CurrentPainting.name + "_CollectorsConfig";
        var collectorConfig = LevelCollectorsSetup.CreateConfigAsset(newCollectorConfigName);

        var newLvl = CreateConfigAsset(CurrentPainting.name + "LevelConfig", PaintingSetup.ResultPaintingConfig, collectorConfig);
        CurrentLevel = newLvl;
        OnValidate();
    }

    public LevelConfig CreateConfigAsset(string configName, PaintingConfig paintingConfig, LevelColorCollectorsConfig collectorConfig, string path = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = LevelConfigPath;
        }
        if (string.IsNullOrEmpty(configName))
        {
            Debug.LogError("Config name cannot be empty!");
            return null;
        }

        // Ensure the path ends with a slash
        if (!path.EndsWith("/"))
        {
            path += "/";
        }

        // Create the directory if it doesn't exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string assetPath = path + configName + ".asset";
        LevelConfig newConfig = ScriptableObject.CreateInstance<LevelConfig>();
        newConfig.BlocksPaintingConfig = paintingConfig;
        newConfig.CollectorsConfig = collectorConfig;
        AssetDatabase.CreateAsset(newConfig, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created new LevelColorCollectorsConfig asset at {assetPath}");
        return newConfig;
    }
#endif

    #endregion
}