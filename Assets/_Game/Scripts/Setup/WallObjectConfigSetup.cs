using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallObjectConfigSetup : MonoBehaviour
{
    [Header("Pipe Configuration")]
    public List<WallObjectSetup> CurrentLevelWallObjectSetups; // To store all the pipe that being setup
    public List<WallObjectSetup> wallObjectSetups; // To store all the pipe that being setup

    [Header("References")]
    public PaintingGridObject gridObject; // GridObject (PaintingGridObject.cs)

    [Header("Prefabs")]
    public GameObject WallPrefab;

    [Header("Wall Properties")]
    public string ColorCode = "Default"; // Color of the pipe
    public int WallHearts = 1;      // Hearts of the pipe
    public ColorPalleteData colorPalette; // The color source that pipe will get from

    [Header("Pipe Positioning")]
    public int PipeSpaceFromGrid = 1;     // Space from grid for placing pipes outside the grid

    [Header("Pipe Setup")]
    public List<PaintingPixelComponent> WallPixelComponents;

    private void Awake()
    {
        if (wallObjectSetups == null) wallObjectSetups = new List<WallObjectSetup>();
    }

    /// <summary>
    /// Add a pipe setup to the list
    /// </summary>
    /// <param name="wallSetup">The pipe setup to add</param>
    public void AddWallSetup(WallObjectSetup wallSetup)
    {
        if (wallObjectSetups == null) wallObjectSetups = new List<WallObjectSetup>();
        if (wallSetup != null && !wallObjectSetups.Contains(wallSetup))
        {
            wallObjectSetups.Add(wallSetup);
        }
    }

    /// <summary>
    /// Remove a pipe setup from the list
    /// </summary>
    /// <param name="pipeSetup">The pipe setup to remove</param>
    public void RemovePipeSetup(WallObjectSetup pipeSetup)
    {
        if (pipeSetup != null)
        {
            wallObjectSetups.Remove(pipeSetup);
        }
    }

    /// <summary>
    /// Clear all pipe setups
    /// </summary>
    public void ClearWallSetups()
    {
        wallObjectSetups.Clear();
    }

    /// <summary>
    /// Create a pipe between StartPixel and EndPixel based on current settings
    /// </summary>
    public void CreateWall()
    {
        if (WallPixelComponents == null || WallPixelComponents.Count <= 1)
        {
            Debug.LogWarning("WallPixelComponents is not valid. Cannot create wall.");
            return;
        }

        // Validate that the pipe should be straight (horizontal or vertical)
        if (!IsValidWallOrientation(WallPixelComponents))
        {
            Debug.LogWarning("Wall must be either horizontal (same row) or vertical (same column). Cannot create pipe.");
            return;
        }

        // Create and setup the pipe in the scene - this will also create the pipe pixels
        List<PaintingPixelConfig> wallPixelConfigs = new List<PaintingPixelConfig>();
        foreach (var pixelComponent in WallPixelComponents)
        {
            wallPixelConfigs.Add( new PaintingPixelConfig( pixelComponent.PixelData));
        }
        WallObjectSetup wallSetup = new WallObjectSetup(wallPixelConfigs, ColorCode, WallHearts);

        var newWallObject = SetupNewWallInSceneWithPixels(wallSetup);

        if (newWallObject != null)
        {
            AddWallSetup(wallSetup);
        }
    }



    /// <summary>
    /// Set up the actual pipe object in the scene
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <param name="colorCode">Color code for the pipe</param>
    /// <returns>Tuple with the created PipeObject component and list of new pipe pixels</returns>
    private WallObject SetupNewWallInSceneWithPixels(WallObjectSetup setup)
    {
        WallObject wallObject = gridObject.CreateWallObject(setup);

        return wallObject;
    }

    /// <summary>
    /// Create a new PaintingPixel and PaintingPixelComponent for a pipe part
    /// </summary>
    /// <param name="column">Column index in the grid that this pipe part corresponds to</param>
    /// <param name="row">Row index in the grid that this pipe part corresponds to</param>
    /// <param name="worldPos">World position for the pipe part</param>
    /// <param name="isHorizontal">Whether the pipe is horizontal</param>
    /// <param name="isTail">Whether this is the tail part of the pipe</param>
    /// <param name="colorCode">Color code for the pipe part</param>
    /// <returns>New PaintingPixel object for the pipe part</returns>
    private PaintingPixel CreatePipePixel(int column, int row, Vector3 worldPos, string colorCode, bool hidden)
    {
        // Get the color from the color palette using the color code
        Color pipeColor = Color.white; // default
        if (colorPalette != null && !string.IsNullOrEmpty(colorCode))
        {
            pipeColor = colorPalette.GetColorByCode(colorCode);
        }

        // Create a GameObject for the pipe pixel
        GameObject pipePixelGO = new GameObject($"PipePixel ({column}, {row})");
        pipePixelGO.transform.SetParent(gridObject.transform);
        pipePixelGO.transform.localPosition = worldPos;

        // Add a PaintingPixelComponent to the GameObject
        PaintingPixelComponent pipePixelComponent = pipePixelGO.AddComponent<PaintingPixelComponent>();

        // Create the PaintingPixel object
        PaintingPixel pipePixel = new PaintingPixel(column, row, pipeColor, worldPos, 1, hidden: hidden, pipePixelGO);
        pipePixel.SetUp(pipeColor, colorCode, hidden); // Set both color and color code

        // Set the pixel data for the component
        pipePixelComponent.SetUp(pipePixel);

        // Add the pipe pixel to the grid object's list of pipe pixels
        if (gridObject.wallObjectsPixels == null)
            gridObject.wallObjectsPixels = new List<PaintingPixel>();
        gridObject.wallObjectsPixels.Add(pipePixel);

        return pipePixel;
    }

    /// <summary>
    /// Validates if the pipe orientation is valid (horizontal or vertical only)
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    private bool IsValidWallOrientation(List<PaintingPixelComponent> _wallPixels)
    {
        if (_wallPixels == null || _wallPixels.Count == 0)
            return false;

        int minRow = _wallPixels.Min(p => p.PixelData.row);
        int maxRow = _wallPixels.Max(p => p.PixelData.row);
        int minCol = _wallPixels.Min(p => p.PixelData.column);
        int maxCol = _wallPixels.Max(p => p.PixelData.column);

        int width = maxCol - minCol + 1;
        int height = maxRow - minRow + 1;
        int expectedCount = width * height;

        if (_wallPixels.Count != expectedCount)
            return false;

        HashSet<(int, int)> pointSet = _wallPixels
            .Select(p => (p.PixelData.row, p.PixelData.column))
            .ToHashSet();

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                if (!pointSet.Contains((r, c)))
                    return false;
            }
        }

        return true;
    }

    private Vector3 GetCenterByBoundingBox(List<PaintingPixelComponent> points)
    {
        if (points == null || points.Count == 0)
            return Vector3.zero;

        float minX = points.Min(p => p.PixelData.worldPos.x);
        float maxX = points.Max(p => p.PixelData.worldPos.x);
        float minY = points.Min(p => p.PixelData.worldPos.y);
        float maxY = points.Max(p => p.PixelData.worldPos.y);
        float minZ = points.Min(p => p.PixelData.worldPos.z);
        float maxZ = points.Max(p => p.PixelData.worldPos.z);

        // trung tâm bounding box
        return new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f
        );
    }

    /// <summary>
    /// Helper method to validate pipe orientation using PaintingPixelComponents
    /// </summary>
    /// <param name="startPixelComponent">Start pixel component</param>
    /// <param name="endPixelComponent">End pixel component</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    //private bool IsValidPipeOrientation(PaintingPixelComponent startPixelComponent, PaintingPixelComponent endPixelComponent)
    //{
    //    if (startPixelComponent == null || endPixelComponent == null)
    //        return false;

    //    PaintingPixel startPixel = startPixelComponent.PixelData;
    //    PaintingPixel endPixel = endPixelComponent.PixelData;

    //    if (startPixel == null || endPixel == null)
    //        return false;

    //    return (startPixel.row == endPixel.row) || (startPixel.column == endPixel.column);
    //}

    /// <summary>
    /// Import all pipe configurations from this setup to a PaintingConfig asset
    /// </summary>
    /// <param name="paintingConfig">The PaintingConfig to import to</param>
    public void ImportPipesToPaintingConfig(PaintingConfig paintingConfig)
    {
        if (paintingConfig == null)
        {
            Debug.LogError("PaintingConfig is null. Cannot import pipes.");
            return;
        }

        // Clear existing pipe setups in the config
        if (paintingConfig.WallSetups == null)
            paintingConfig.WallSetups = new List<WallObjectSetup>();
        else
            paintingConfig.WallSetups.Clear();

        // Copy all pipe setups from this component to the config
        foreach (WallObjectSetup pipeSetup in wallObjectSetups)
        {
            if (pipeSetup != null)
            {
                // Add the pipe setup to the painting config
                paintingConfig.WallSetups.Add(pipeSetup);
            }
        }

        paintingConfig.HidePixelsUnderPipes();

        Debug.Log($"Imported {wallObjectSetups.Count} pipe setups to PaintingConfig.");
    }

    /// <summary>
    /// Apply all pipe configurations from a PaintingConfig asset to this setup
    /// </summary>
    /// <param name="paintingConfig">The PaintingConfig to import from</param>
    public void ApplyPaintingConfigPipes(PaintingConfig paintingConfig)
    {
        if (paintingConfig == null)
        {
            Debug.LogError("PaintingConfig is null. Cannot apply pipes.");
            return;
        }

        // Clear existing pipe setups in this component
        wallObjectSetups.Clear();

        // Copy all pipe setups from the config to this component
        if (paintingConfig.WallSetups != null)
        {
            foreach (WallObjectSetup pipeSetup in paintingConfig.WallSetups)
            {
                if (pipeSetup != null)
                {
                    wallObjectSetups.Add(pipeSetup);
                }
            }
        }
        int tmp = paintingConfig.PipeSetups != null ? paintingConfig.PipeSetups.Count : 0;
        Debug.Log($"Applied {tmp} pipe setups from PaintingConfig.");
    }

    public void Reload()
    {
        CurrentLevelWallObjectSetups = gridObject.paintingConfig.WallSetups;
        wallObjectSetups = new List<WallObjectSetup>(CurrentLevelWallObjectSetups);
        //return;
        foreach (var pipe in CurrentLevelWallObjectSetups)
        {
            gridObject.CreateWallObject(pipe);
        }
    }
}