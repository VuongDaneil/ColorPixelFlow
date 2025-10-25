using System.Collections.Generic;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class LevelCollectorsConfigSetup : MonoBehaviour
{
    [Header("Configuration")]
    public LevelColorCollectorsConfig configAsset; // The config asset to set up
    public PaintingConfig paintingConfig;

    [Header("Limits")]
    public int MaxBulletPerCollector = 20; // Maximum bullets per collector as per design document

    [Header("Setup Parameters")]
    public int numberOfCollectors = 6; // Total number of collectors to create
    public int numberOfColumns = 3; // Number of columns to arrange collectors in
    public int bulletsPerCollector = 10; // Number of bullets for each collector
    public string baseColorCode = "Color"; // Base name for color codes (will be suffixed with index)

    [Header("Default States")]
    public bool defaultLocked = false;
    public bool defaultHidden = false;

    [Header("Preview")]
    public LevelCollectorsSystem previewSystem; // Use LevelCollectorsSystem gameobject to preview the config file

    public void GenerateDefaultConfig()
    {
        if (configAsset == null)
        {
            Debug.LogError("Please assign a LevelColorCollectorsConfig asset to set up!");
            return;
        }

        // Initialize the collector setups list
        if (configAsset.CollectorSetups == null)
        {
            configAsset.CollectorSetups = new List<SingleColorCollectorObject>();
        }
        
        configAsset.CollectorSetups.Clear();
        configAsset.NumberOfColumns = numberOfColumns;

        // Create collector objects based on parameters
        for (int i = 0; i < numberOfCollectors; i++)
        {
            SingleColorCollectorObject collector = new SingleColorCollectorObject
            {
                OriginalIndex = i,
                ColorCode = baseColorCode,
                Bullets = bulletsPerCollector,
                Locked = defaultLocked,
                Hidden = defaultHidden,
                ConnectedCollectorsIndex = new List<int>() // Empty by default
            };

            configAsset.CollectorSetups.Add(collector);
        }

        EnsureBidirectionalConnections();

        Debug.Log($"Generated {numberOfCollectors} collector setups in {numberOfColumns} columns for {configAsset.name}");
    }
    
    #if UNITY_EDITOR
    public LevelColorCollectorsConfig CreateConfigAsset(string configName, string path = "Assets/Resources/")
    {
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
        configAsset.CollectorSetups.Clear();
        
        // Import collector data from the system's current collectors
        for (int i = 0; i < previewSystem.CurrentCollectors.Count; i++)
        {
            ColorPixelsCollector collector = previewSystem.CurrentCollectors[i];
            
            SingleColorCollectorObject newCollector = new SingleColorCollectorObject
            {
                OriginalIndex = collector.OriginalIndex,
                ColorCode = collector.CollectorColor, // Use the color code the collector can destroy
                Bullets = collector.BulletLeft, // Use remaining bullets
                Locked = collector.IsLocked,
                Hidden = collector.IsHidden, // Default to not hidden
                ConnectedCollectorsIndex = new List<int>(collector.ConnectedCollectorsIndex) // Default to no connections
            };
            
            configAsset.CollectorSetups.Add(newCollector);
        }
        
        // Update the number of columns (might need to set this manually or estimate)
        // For now, we'll keep the existing number of columns or default to 1 if not set
        if (configAsset.NumberOfColumns <= 0) configAsset.NumberOfColumns = 1;
        EnsureBidirectionalConnections();
        Debug.Log($"Imported {previewSystem.CurrentCollectors.Count} collector data from LevelCollectorsSystem to {configAsset.name}");
    }
    
    // Ensure bidirectional connections in the config
    public void EnsureBidirectionalConnections()
    {
        if (configAsset == null || configAsset.CollectorSetups == null)
        {
            Debug.LogError("Config asset or collector setups is null!");
            return;
        }

        // Iterate through each collector in the config
        for (int i = 0; i < configAsset.CollectorSetups.Count; i++)
        {
            SingleColorCollectorObject collector = configAsset.CollectorSetups[i];
            
            // For each connection in this collector, ensure the reverse connection exists
            foreach (int connectedIndex in collector.ConnectedCollectorsIndex)
            {
                if (connectedIndex >= 0 && connectedIndex < configAsset.CollectorSetups.Count)
                {
                    SingleColorCollectorObject targetCollector = configAsset.CollectorSetups[connectedIndex];
                    
                    // If the target collector doesn't have this collector in its connections, add it
                    if (!targetCollector.ConnectedCollectorsIndex.Contains(i))
                    {
                        targetCollector.ConnectedCollectorsIndex.Add(i);
                        Debug.Log($"Added reverse connection: Collector {connectedIndex} now connected to {i}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Invalid connection index {connectedIndex} in collector {i}");
                }
            }
        }
        
        Debug.Log($"Ensured bidirectional connections for {configAsset.CollectorSetups.Count} collectors in {configAsset.name}");
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
        if (configAsset.CollectorSetups == null)
        {
            configAsset.CollectorSetups = new List<SingleColorCollectorObject>();
        }
        configAsset.CollectorSetups.Clear();

        // Collect all pixels from painting config (PaintingConfig.Pixels) and from pipe setups (PixelCovered)
        List<PaintingPixel> allPixels = new List<PaintingPixel>();

        // Add pixels from PaintingConfig.Pixels (convert from PaintingPixelConfig to PaintingPixel)
        foreach (var pixelConfig in paintingConfig.Pixels)
        {
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
            foreach(var pixel in pipeSetup.PixelCovered)
            {
                allPixels.Add(new PaintingPixel(pixel));
            }
        }

        // Group pixels by outline based on painting size
        List<List<PaintingPixel>> outlines = ExtractOutlinesByDepth(allPixels, paintingConfig.PaintingSize);

        // Process each outline to create collectors
        int originalIndex = 0;
        int outlineCount = outlines.Count;
        for (int i = outlineCount - 1; i >= 0; i--)
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
            for (int j = colorSetCount - 1; j >= 0; j--)
            {
                var colorCount = colorCounts.ElementAt(j);
                string colorCode = colorCount.Key;
                int totalPixels = colorCount.Value;
                
                // Create as many collectors as needed to handle the total pixel count
                int remainingPixels = totalPixels;
                
                while (remainingPixels > 0)
                {
                    int bulletsForThisCollector = Mathf.Min(remainingPixels, MaxBulletPerCollector);
                    
                    SingleColorCollectorObject collector = new SingleColorCollectorObject
                    {
                        OriginalIndex = originalIndex++,
                        ColorCode = colorCode,
                        Bullets = bulletsForThisCollector,
                        Locked = defaultLocked,
                        Hidden = defaultHidden,
                        ConnectedCollectorsIndex = new List<int>() // Empty by default
                    };
                    
                    configAsset.CollectorSetups.Add(collector);
                    
                    remainingPixels -= bulletsForThisCollector;
                }
            }
        }

        Debug.Log($"Generated {configAsset.CollectorSetups.Count} collector setups from painting config '{paintingConfig.name}'");
    }
    
    // Extract outline pixels from outermost to innermost
    private List<List<PaintingPixel>> ExtractOutlinesByDepth(List<PaintingPixel> allPixels, Vector2 paintingSize)
    {
        List<List<PaintingPixel>> outlines = new List<List<PaintingPixel>>();
        
        // Create a copy of pixels to work with
        List<PaintingPixel> workingPixels = new List<PaintingPixel>(allPixels);
        
        // Calculate bounds based on painting size
        int halfWidth = Mathf.RoundToInt(paintingSize.x / 2f);
        int halfHeight = Mathf.RoundToInt(paintingSize.y / 2f);
        
        // We'll extract outlines from the outermost to the innermost
        int minCol = -halfWidth;
        int maxCol = halfWidth - 1;
        int minRow = -halfHeight;
        int maxRow = halfHeight - 1;
        
        while (minCol <= maxCol && minRow <= maxRow)
        {
            // Extract current outline
            List<PaintingPixel> currentOutline = new List<PaintingPixel>();
            
            // Add pixels from the top edge (minRow)
            for (int col = minCol; col <= maxCol; col++)
            {
                var pixel = FindPixelAt(workingPixels, col, minRow);
                if (pixel != null)
                {
                    currentOutline.Add(pixel);
                    workingPixels.Remove(pixel); // Remove from working list to avoid duplicates
                }
            }
            
            // Add pixels from the right edge (maxCol), excluding corners already added
            for (int row = minRow + 1; row <= maxRow - 1; row++)
            {
                var pixel = FindPixelAt(workingPixels, maxCol, row);
                if (pixel != null)
                {
                    currentOutline.Add(pixel);
                    workingPixels.Remove(pixel); // Remove from working list to avoid duplicates
                }
            }
            
            // Add pixels from the bottom edge (maxRow), in reverse order
            for (int col = maxCol; col >= minCol; col--)
            {
                var pixel = FindPixelAt(workingPixels, col, maxRow);
                if (pixel != null)
                {
                    currentOutline.Add(pixel);
                    workingPixels.Remove(pixel); // Remove from working list to avoid duplicates
                }
            }
            
            // Add pixels from the left edge (minCol), excluding corners already added
            for (int row = maxRow - 1; row >= minRow + 1; row--)
            {
                var pixel = FindPixelAt(workingPixels, minCol, row);
                if (pixel != null)
                {
                    currentOutline.Add(pixel);
                    workingPixels.Remove(pixel); // Remove from working list to avoid duplicates
                }
            }
            
            // If we found any pixels in this outline, add it to our list
            if (currentOutline.Count > 0)
            {
                outlines.Add(currentOutline);
            }
            
            // Move to the next inner outline by shrinking the boundaries
            minCol++;
            maxCol--;
            minRow++;
            maxRow--;
        }
        
        return outlines;
    }
    
    // Helper method to find a pixel at specific coordinates
    private PaintingPixel FindPixelAt(List<PaintingPixel> pixels, int column, int row)
    {
        for (int i = 0; i < pixels.Count; i++)
        {
            if (pixels[i].column == column && pixels[i].row == row)
            {
                return pixels[i];
            }
        }
        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelCollectorsConfigSetup))]
public class LevelCollectorsConfigSetupEditor : Editor
{
    private string newConfigName = "CollectorsConfig";
    private string newConfigPath = "Assets/_Game/Data/GunnerConfig/";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelCollectorsConfigSetup setup = (LevelCollectorsConfigSetup)target;

        if (GUILayout.Button("Generate Default Config"))
        {
            setup.GenerateDefaultConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Config Asset:", EditorStyles.boldLabel);
        newConfigPath = EditorGUILayout.TextField("Path", newConfigPath);

        if (GUILayout.Button("Create Config Asset"))
        {
            if (setup != null)
            {
                if (setup.paintingConfig != null) newConfigName = setup.paintingConfig.name.Replace("_PaintingConfig", "") + "_CollectorsConfig";
                LevelColorCollectorsConfig newConfig = setup.CreateConfigAsset(newConfigName, newConfigPath);
                if (newConfig != null)
                {
                    setup.configAsset = newConfig;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load & Preview Config:", EditorStyles.boldLabel);
        if (GUILayout.Button("Load & Preview Config"))
        {
            if (setup.configAsset != null)
            {
                setup.LoadConfigAsset(setup.configAsset);
            }
            else
            {
                Debug.LogWarning("Please assign a config asset to load!");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import Config:", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh"))
        {
            foreach (var collector in setup.previewSystem.CurrentCollectors)
            {
                collector.VisualHandler.SetColor(collector.CollectorColor);
            }
        }
        if (GUILayout.Button("Import from Scene"))
        {
            if (EditorUtility.DisplayDialog("Confirm Generation",
                    "This will replace all existing collector setups in the config asset with new ones based on the one in scene. Are you sure?",
                    "Yes", "Cancel"))
            {
                if (setup.previewSystem != null && setup.configAsset != null)
                {
                    setup.ImportCollectorsFromScene();
                }
                else
                {
                    Debug.LogWarning("Please assign both source system and target config asset!");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connections:", EditorStyles.boldLabel);
        if (GUILayout.Button("Ensure Bidirectional Connections"))
        {
            setup.EnsureBidirectionalConnections();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import from PaintingConfig:", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate from PaintingConfig"))
        {
            if (setup.paintingConfig != null && setup.configAsset != null)
            {
                if (EditorUtility.DisplayDialog("Confirm Generation", 
                    "This will replace all existing collector setups in the config asset with new ones based on the painting config. Are you sure?", 
                    "Yes", "Cancel"))
                {
                    setup.GenerateCollectorsFromPaintingConfig();
                }
            }
            else
            {
                Debug.LogWarning("Please assign both paintingConfig and configAsset!");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Clear Setup:", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Setup in Scene"))
        {
            setup.previewSystem.ClearExistingCollectors();
        }
    }
}
#endif