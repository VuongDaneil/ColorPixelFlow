using static PaintingSharedAttributes;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class LevelCollectorsConfigSetup : MonoBehaviour
{
    #region PROPERTIES
    [Header("Configuration")]
    [ReadOnly] public LevelColorCollectorsConfig configAsset; // The config asset to set up
    [ReadOnly] public PaintingConfig paintingConfig;

    [Header("Setup Parameters")]
    public int NumberOfColumns = 3; // Number of columns to arrange collectors in
    public int MaxBulletPerCollector = 20; // Maximum bullets per collector as per design document

    [Header("Default States")]
    public bool defaultLocked = false;
    public bool defaultHidden = false;

    [Header("Preview")]
    public LevelCollectorsSystem previewSystem; // Use LevelCollectorsSystem gameobject to preview the config file

    [Header("Tool")]
    public bool ToolActive = false;
    public LayerMask CollectorObjectLayerMask;
    public float CollisionRadius;
    [ShowIf("ToolActive")] public List<Vector3> OriginalCollectorPosition = new List<Vector3>();
    public int NumberOfWorkingPixels = 0;
    public int TotalBulletsCount = 0;
    public int NumberOfLockedCollector = 0;
    public Dictionary<string, int> colorSetCounters = new Dictionary<string, int>();
    public Dictionary<string, int> collectorSetCounters = new Dictionary<string, int>();

    [Header("TOOL MODULE(s)")]
    public MoveCollector MoveModule;
    public SwapCollectors SwapModule;
    public SplitCollector SplitModule;
    public CombinesCollector CombineModule;
    public ConnectCollectors ConnectModule;
    #endregion

    #region MAIN

    public void LoadConfigAsset(LevelColorCollectorsConfig sourceConfig)
    {
        if (sourceConfig == null)
        {
            Debug.LogError("Source config is null!");
            return;
        }

        configAsset = sourceConfig;
        Debug.Log($"Loaded config asset: {configAsset.name}");

        // Preview the loaded config if preview system is available
        if (previewSystem != null)
        {
            previewSystem.CurrentLevelCollectorsConfig = configAsset;
            previewSystem.SetupCollectors();
        }

#if UNITY_EDITOR
        BakeCollectorsPositionInTool();
#endif
    }

    // Import from a LevelCollectorsSystem's CurrentCollectors (runtime data)
    public void ImportCollectorsFromScene()
    {
        if (previewSystem == null || previewSystem.CurrentCollectors == null || previewSystem.CurrentCollectors.Count == 0)
        {
            Debug.LogError("Source system or its current collectors is null/empty!");
            return;
        }

        if (configAsset == null)
        {
            Debug.LogError("Target config is null! Please assign a config asset to import to.");
            return;
        }

        // Clear the target config's existing setups
        configAsset.CollectorColumns.Clear();

        // Import collector data from the system's current collectors
        foreach (CollectorColumn col in previewSystem.CollectorColumns)
        {
            ColumnOfCollectorConfig newColumn = new ColumnOfCollectorConfig();
            foreach (ColorPixelsCollectorObject collector in col.CollectorsInColumn)
            {
                SingleColorCollectorConfig newCollector = new SingleColorCollectorConfig
                {
                    ID = collector.ID,
                    ColorCode = collector.CollectorColor, // Use the color code the collector can destroy
                    Bullets = collector.BulletCapacity, // Use remaining bullets
                    Locked = collector.IsLocked,
                    Hidden = collector.IsHidden,
                    ConnectedCollectorsIDs = new List<int>(collector.ConnectedCollectorsIDs) // Default to no connections
                };
                newColumn.Collectors.Add(newCollector);
            }
            if (newColumn.Collectors.Count > 0) configAsset.CollectorColumns.Add(newColumn);
        }

        // Update the number of columns (might need to set this manually or estimate)
        // For now, we'll keep the existing number of columns or default to 1 if not set
        EnsureBidirectionalConnections();
        Debug.Log($"Imported {previewSystem.CurrentCollectors.Count} collector data from LevelCollectorsSystem to {configAsset.name}");
    }

    // Ensure bidirectional connections in the config
    public void EnsureBidirectionalConnections()
    {
        if (configAsset == null || configAsset.CollectorColumns == null || configAsset.CollectorColumns.Count <= 0)
        {
            Debug.LogError("Config asset or collector setups is null!");
            return;
        }

        List<SingleColorCollectorConfig> allCollectorConfig = new List<SingleColorCollectorConfig>();

        foreach (ColumnOfCollectorConfig column in configAsset.CollectorColumns)
        {
            allCollectorConfig.AddRange(column.Collectors);
        }

        if (allCollectorConfig.Count <= 0) return;

        // Iterate through each collector in the config
        for (int i = 0; i < allCollectorConfig.Count; i++)
        {
            SingleColorCollectorConfig collector = allCollectorConfig[i];
            
            // For each connection in this collector, ensure the reverse connection exists
            foreach (int connectedID in collector.ConnectedCollectorsIDs)
            {
                if (connectedID == collector.ID) continue;
                if (connectedID >= 0)
                {
                    SingleColorCollectorConfig targetCollector = GetCollectorConfigByID(allCollectorConfig, connectedID);
                    if (targetCollector == null || targetCollector.ID == collector.ID) return;
                    
                    // If the target collector doesn't have this collector in its connections, add it
                    if (!targetCollector.ConnectedCollectorsIDs.Contains(collector.ID))
                    {
                        targetCollector.ConnectedCollectorsIDs.Add(collector.ID);
                        Debug.Log($"Added reverse connection: Collector {connectedID} now connected to ID {collector.ID}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Invalid connection index {connectedID} in collector {i}");
                }
            }
        }
        
        Debug.Log($"Ensured bidirectional connections for {allCollectorConfig.Count} collectors in {allCollectorConfig}");
    }
    
    // Generate collector configurations from painting config
    public void GenerateCollectorsFromPaintingConfig()
    {
        if (paintingConfig == null)
        {
            Debug.LogError("PaintingConfig is null! Please assign a painting config to generate collectors from.");
            return;
        }

        if (configAsset == null)
        {
            Debug.LogError("Config asset is null! Please assign a LevelColorCollectorsConfig asset to populate.");
            return;
        }

        // Clear existing collector setups
        if (configAsset.CollectorColumns == null)
        {
            configAsset.CollectorColumns = new List<ColumnOfCollectorConfig>();
        }
        configAsset.CollectorColumns.Clear();

        // Collect all pixels from painting config (PaintingConfig.Pixels) and from pipe setups (PixelCovered)
        List<PaintingPixelConfig> allWorkingPixels = paintingConfig.GetAllWorkingPixels();
        List<PaintingPixel> allPixels = new List<PaintingPixel>();

        for (int i = 0; i < allWorkingPixels.Count; i++)
        {
            PaintingPixelConfig pConfig = allWorkingPixels[i];
            PaintingPixel pixel = new PaintingPixel
            {
                column = pConfig.column,
                row = pConfig.row,
                color = pConfig.color,
                colorCode = pConfig.colorCode,
                Hearts = 1, // Default to 1 heart if not specified in config
                Hidden = pConfig.Hidden
            };
            allPixels.Add(pixel);
        }

        if (false)
        {
            // Add pixels from PaintingConfig.Pixels (convert from PaintingPixelConfig to PaintingPixel)
            foreach (var pixelConfig in paintingConfig.Pixels)
            {
                if (pixelConfig.Hidden) continue;
                PaintingPixel pixel = new PaintingPixel
                {
                    column = pixelConfig.column,
                    row = pixelConfig.row,
                    color = pixelConfig.color,
                    colorCode = pixelConfig.colorCode,
                    Hearts = 1, // Default to 1 heart if not specified in config
                    Hidden = pixelConfig.Hidden
                };
                allPixels.Add(pixel);
            }

            // Add pixels from PipeSetups.PixelCovered
            foreach (var pipeSetup in paintingConfig.PipeSetups)
            {
                foreach (var pixel in pipeSetup.PixelCovered)
                {
                    PaintingPixel _new = new PaintingPixel(pixel);
                    if (allPixels.Any(x => (x.column == _new.column && x.row == _new.row)))
                    {
                        allPixels.Remove(allPixels.First(x => (x.column == _new.column && x.row == _new.row)));
                    }
                    allPixels.Add(_new);
                }
            }

            // Add pixels from PipeSetups.WallSetups
            foreach (var wallSetup in paintingConfig.WallSetups)
            {
                int pixelCoveredCount = wallSetup.PixelCovered.Count;

                foreach (PaintingPixelConfig _p in wallSetup.PixelCovered)
                {
                    if (allPixels.Any(x => (x.column == _p.column && x.row == _p.row)))
                    {
                        allPixels.Remove(allPixels.First(x => (x.column == _p.column && x.row == _p.row)));
                    }
                }

                for (int i = 0; i < wallSetup.Hearts; i++)
                {
                    PaintingPixel _new = new PaintingPixel(wallSetup.PixelCovered[i % (pixelCoveredCount - 1)]);
                    _new.colorCode = wallSetup.ColorCode;
                    allPixels.Add(_new);
                }
            }

            // Add pixels from PipeSetups.KeySetups
            foreach (var keySetup in paintingConfig.KeySetups)
            {
                int pixelCoveredCount = keySetup.PixelCovered.Count;

                foreach (PaintingPixelConfig _p in keySetup.PixelCovered)
                {
                    if (allPixels.Any(x => (x.column == _p.column && x.row == _p.row)))
                    {
                        allPixels.Remove(allPixels.First(x => (x.column == _p.column && x.row == _p.row)));
                    }
                }

                PaintingPixel _new = new PaintingPixel(keySetup.PixelCovered[Random.Range(0, pixelCoveredCount)]);
                allPixels.Add(_new);
            }
        }

        // Group pixels by outline based on painting size
        List<List<PaintingPixel>> outlines = ExtractOutlinesByDepth(allPixels, paintingConfig.PaintingSize);

        // Process each outline to create collectors
        int outlineCount = outlines.Count;
        List<SingleColorCollectorConfig> allCollectorConfigs = new List<SingleColorCollectorConfig>();

        int id = 0;
        for (int i = 0; i < outlineCount; i++)
        {
            var outline = outlines[i];

            // Categorize pixels by color in this outline
            Dictionary<string, int> colorCounts = new Dictionary<string, int>();
            foreach (var pixel in outline)
            {
                if (pixel.Hidden) continue; // Skip hidden pixels
                if (colorCounts.ContainsKey(pixel.colorCode))
                {
                    colorCounts[pixel.colorCode] += pixel.Hearts; // Count hearts as multiples
                }
                else
                {
                    colorCounts[pixel.colorCode] = pixel.Hearts;
                }
            }

            // Create collectors based on color counts
            int colorSetCount = colorCounts.Count;
            for (int j = 0; j < colorSetCount; j++)
            {
                var colorCount = colorCounts.ElementAt(j);
                string colorCode = colorCount.Key;
                int totalPixels = colorCount.Value;
                
                // Create as many collectors as needed to handle the total pixel count
                int remainingPixels = totalPixels;
                
                while (remainingPixels > 0)
                {
                    int bulletsForThisCollector = Mathf.Min(remainingPixels, MaxBulletPerCollector);
                    
                    SingleColorCollectorConfig collector = new SingleColorCollectorConfig
                    {
                        ID = id,
                        ColorCode = colorCode,
                        Bullets = bulletsForThisCollector,
                        Locked = defaultLocked,
                        Hidden = defaultHidden,
                        ConnectedCollectorsIDs = new List<int>() // Empty by default
                    };

                    allCollectorConfigs.Add(collector);
                    
                    remainingPixels -= bulletsForThisCollector;
                    id++;
                }
            }
        }

        int collectorCount = allCollectorConfigs.Count;
        List<ColumnOfCollectorConfig> columnsConfig = new List<ColumnOfCollectorConfig>();

        for (int colIdx = 0; colIdx < NumberOfColumns; colIdx++)
        {
            ColumnOfCollectorConfig column = new ColumnOfCollectorConfig();
            // Add collectors to this column (every nth collector where n is the number of columns)
            // In row-major order, collectors in the same column are at indices: colIdx, colIdx+numberOfColumns, colIdx+2*numberOfColumns, etc.
            for (int idx = colIdx; idx < collectorCount; idx += NumberOfColumns)
            {
                if (idx < allCollectorConfigs.Count)
                {
                    column.Collectors.Add(allCollectorConfigs[idx]);
                }
            }
            columnsConfig.Add(column);
        }
        configAsset.CollectorColumns = columnsConfig;

        Debug.Log($"Generated {configAsset.CollectorColumns.Count} collector setups from painting config '{paintingConfig.name}'");
    }
    
    // Extract outline pixels from outermost to innermost
    private List<List<PaintingPixel>> ExtractOutlinesByDepth(List<PaintingPixel> allPixels, Vector2 paintingSize)
    {
        List<List<PaintingPixel>> outlines = new List<List<PaintingPixel>>();
        if (allPixels == null || allPixels.Count == 0)
            return outlines;

        // Clone the list to avoid modifying the original
        List<PaintingPixel> workingPixels = new List<PaintingPixel>(allPixels);

        // Keep looping while there are still pixels
        while (workingPixels.Count > 0)
        {
            // Determine current bounds (outermost rectangle of remaining pixels)
            int minCol = int.MaxValue, maxCol = int.MinValue;
            int minRow = int.MaxValue, maxRow = int.MinValue;

            foreach (var pixel in workingPixels)
            {
                if (pixel.column < minCol) minCol = pixel.column;
                if (pixel.column > maxCol) maxCol = pixel.column;
                if (pixel.row < minRow) minRow = pixel.row;
                if (pixel.row > maxRow) maxRow = pixel.row;
            }

            List<PaintingPixel> currentOutline = new List<PaintingPixel>();

            // Top edge
            for (int col = minCol; col <= maxCol; col++)
            {
                var p = FindPixelsAt(workingPixels, col, minRow);
                if (p != null)
                {
                    currentOutline.AddRange(p);
                }
            }

            // Right edge
            for (int row = minRow + 1; row <= maxRow - 1; row++)
            {
                var p = FindPixelsAt(workingPixels, maxCol, row);
                if (p != null)
                {
                    currentOutline.AddRange(p);
                }
            }

            // Bottom edge
            for (int col = maxCol; col >= minCol; col--)
            {
                var p = FindPixelsAt(workingPixels, col, maxRow);
                if (p != null)
                {
                    currentOutline.AddRange(p);
                }
            }

            // Left edge
            for (int row = maxRow - 1; row >= minRow + 1; row--)
            {
                var p = FindPixelsAt(workingPixels, minCol, row);
                if (p != null)
                {
                    currentOutline.AddRange(p);
                }
            }

            // Remove found pixels from working list
            foreach (var p in currentOutline)
            {
                workingPixels.Remove(p);
            }

            // Add to results if we found any
            if (currentOutline.Count > 0)
            {
                outlines.Add(currentOutline);
            }
            else
            {
                // No outline found — break to avoid infinite loop
                break;
            }
        }

        return outlines;
    }
    
    // Helper method to find a pixel at specific coordinates
    private List<PaintingPixel> FindPixelsAt(List<PaintingPixel> pixels, int column, int row)
    {
        List<PaintingPixel> rs = new List<PaintingPixel>();
        for (int i = 0; i < pixels.Count; i++)
        {
            if (pixels[i].column == column && pixels[i].row == row)
            {
                rs.Add(pixels[i]);
            }
        }
        return rs;
    }
    #endregion

    #region SUPPORTIVE
    public void StartUpTool()
    {
        ToolActive = !ToolActive;
        if (ToolActive)
        {
            List<PaintingPixelConfig> allWorkingPixels = paintingConfig.GetAllWorkingPixels();
            NumberOfWorkingPixels = allWorkingPixels.Count;
            colorSetCounters.Clear();
            foreach (var pixel in allWorkingPixels)
            {
                if (pixel.Hidden) continue; // Skip hidden pixels
                if (colorSetCounters.ContainsKey(pixel.colorCode))
                {
                    colorSetCounters[pixel.colorCode]++;
                }
                else
                {
                    colorSetCounters[pixel.colorCode] = 1;
                }
            }

            ReCountCollectors();

            LoadConfigAsset(configAsset);
        }
    }
    public void BakeCollectorsPositionInTool()
    {
        OriginalCollectorPosition.Clear();
        for (int i = 0; i < previewSystem.CurrentCollectors.Count; i++)
        {
            OriginalCollectorPosition.Add(previewSystem.CurrentCollectors[i].transform.position);
        }
    }
    public void ReApplyCollectorsPosition()
    {
        for (int i = 0; i < previewSystem.CurrentCollectors.Count; i++)
        {
            previewSystem.CurrentCollectors[i].transform.position = OriginalCollectorPosition[i];
        }
    }
    public void ReCountCollectors()
    {
        TotalBulletsCount = 0;
        NumberOfLockedCollector = 0;
        collectorSetCounters.Clear();
        foreach (var collector in previewSystem.CurrentCollectors)
        {
            if (collectorSetCounters.ContainsKey(collector.CollectorColor))
            {
                collectorSetCounters[collector.CollectorColor]++;
            }
            else
            {
                collectorSetCounters[collector.CollectorColor] = 1;
            }
            TotalBulletsCount += collector.BulletCapacity;
            if (collector.IsLocked) NumberOfLockedCollector++;
        }
    }

#if UNITY_EDITOR
    public LevelColorCollectorsConfig CreateConfigAsset(string configName, string path = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = CollectorsConfigPath;
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
        LevelColorCollectorsConfig newConfig = ScriptableObject.CreateInstance<LevelColorCollectorsConfig>();
        AssetDatabase.CreateAsset(newConfig, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created new LevelColorCollectorsConfig asset at {assetPath}");
        return newConfig;
    }
#endif
    #endregion

    #region TOOL MODULES

    #region _move
    public void InsertAmongOtherCollector(ColorPixelsCollectorObject itemToInsert, ColorPixelsCollectorObject originItem, bool higher)
    {
        bool sameColumn = false;
        CollectorColumn targetColumnToMoveTo = null;
        CollectorColumn originColumn = null;
        foreach (CollectorColumn column in previewSystem.CollectorColumns)
        {
            if (column.CollectorsInColumn.Contains(originItem))
            {
                targetColumnToMoveTo = column;
                if (column.CollectorsInColumn.Contains(itemToInsert))
                {
                    sameColumn = true;
                    break;
                }
            }

            if (column.CollectorsInColumn.Contains(itemToInsert)) originColumn = column;
        }

        if (sameColumn)
        {
            MoveRelative(targetColumnToMoveTo.CollectorsInColumn, itemToInsert, originItem, higher);
        }
        else
        {
            originColumn.CollectorsInColumn.Remove(itemToInsert);
            InsertRelative(targetColumnToMoveTo.CollectorsInColumn, itemToInsert, originItem, higher);
        }
        previewSystem.ReArrangePosition();
        previewSystem.SetupConnectedCollectors();
    }

    public void InsertNewToOtherCollector(ColorPixelsCollectorObject itemToInsert, ColorPixelsCollectorObject originItem, bool higher)
    {
        bool sameColumn = false;
        CollectorColumn targetColumnToMoveTo = null;
        CollectorColumn originColumn = null;
        foreach (CollectorColumn column in previewSystem.CollectorColumns)
        {
            if (column.CollectorsInColumn.Contains(originItem))
            {
                targetColumnToMoveTo = column;
                if (column.CollectorsInColumn.Contains(itemToInsert))
                {
                    sameColumn = true;
                    break;
                }
            }

            if (column.CollectorsInColumn.Contains(originItem)) originColumn = column;
        }

        if (sameColumn)
        {
            MoveRelative(targetColumnToMoveTo.CollectorsInColumn, itemToInsert, originItem, higher);
        }
        else
        {
            originColumn.CollectorsInColumn.Remove(itemToInsert);
            InsertRelative(targetColumnToMoveTo.CollectorsInColumn, itemToInsert, originItem, higher);
        }
        previewSystem.ReArrangePosition();
        previewSystem.SetupConnectedCollectors();
    }
    #endregion

    #region _split
    public void SplitACollector(ColorPixelsCollectorObject originItem)
    {
        int maxBullets = originItem.BulletCapacity;
        if (maxBullets <= 1) return;
        int originObjBullets = maxBullets / 2;
        int cloneObjBullets = maxBullets - originObjBullets;

        ColorPixelsCollectorObject newCollector = previewSystem.CloneNewFromCollector(originItem);

        originItem.BulletCapacity = originObjBullets;
        newCollector.BulletCapacity = cloneObjBullets;

        InsertNewToOtherCollector(newCollector, originItem, higher: false);

        previewSystem.ReArrangePosition();
        previewSystem.SetupConnectedCollectors();
    }
    #endregion

    #endregion
}