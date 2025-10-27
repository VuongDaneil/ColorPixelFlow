using System.Collections.Generic;
using NaughtyAttributes;
using System.Linq;
using UnityEngine;

public class WallObjectConfigSetup : MonoBehaviour
{
    [Header("Wall Configuration")]
    public List<WallObjectSetup> CurrentLevelWallObjectSetups; // To store all the Wall that being setup
    public List<WallObjectSetup> wallObjectSetups; // To store all the Wall that being setup

    [Header("References")]
    [ReadOnly] public PaintingGridObject gridObject; // GridObject (PaintingGridObject.cs)

    [Header("Prefabs")]
    public GameObject WallPrefab;

    [Header("Wall Properties")]
    public string ColorCode = "Default"; // Color of the Wall
    public int WallHearts = 1;      // Hearts of the Wall
    public ColorPalleteData colorPalette; // The color source that Wall will get from

    [Header("Wall Positioning")]
    public int WallSpaceFromGrid = 1;     // Space from grid for placing Wall outside the grid

    [Header("Wall Setup")]
    public List<PaintingPixelComponent> WallPixelComponents;

    private void Awake()
    {
        if (wallObjectSetups == null) wallObjectSetups = new List<WallObjectSetup>();
    }

    /// <summary>
    /// Add a wall setup to the list
    /// </summary>
    /// <param name="wallSetup">The wall setup to add</param>
    public void AddWallSetup(WallObjectSetup wallSetup)
    {
        if (wallObjectSetups == null) wallObjectSetups = new List<WallObjectSetup>();
        if (wallSetup != null && !wallObjectSetups.Contains(wallSetup))
        {
            wallObjectSetups.Add(wallSetup);
        }
    }

    /// <summary>
    /// Remove a wall setup from the list
    /// </summary>
    /// <param name="wallSetup">The wall setup to remove</param>
    public void RemoveWallSetup(WallObjectSetup wallSetup)
    {
        if (wallSetup != null)
        {
            wallObjectSetups.Remove(wallSetup);
        }
    }

    /// <summary>
    /// Clear all wall setups
    /// </summary>
    public void ClearWallSetups()
    {
        wallObjectSetups.Clear();
    }

    /// <summary>
    /// Create a wall between StartPixel and EndPixel based on current settings
    /// </summary>
    public void CreateWall()
    {
        if (WallPixelComponents == null || WallPixelComponents.Count <= 1)
        {
            Debug.LogWarning("WallPixelComponents is not valid. Cannot create wall.");
            return;
        }

        // Validate that the wall should be straight (horizontal or vertical)
        if (!IsValidWallOrientation(WallPixelComponents))
        {
            Debug.LogWarning("Wall must be either horizontal (same row) or vertical (same column). Cannot create wall.");
            return;
        }

        // Create and setup the wall in the scene - this will also create the wall pixels
        List<PaintingPixelConfig> wallPixelConfigs = new List<PaintingPixelConfig>();
        foreach (var pixelComponent in WallPixelComponents)
        {
            wallPixelConfigs.Add( new PaintingPixelConfig( pixelComponent.PixelData));
        }
        WallObjectSetup wallSetup = new WallObjectSetup(wallPixelConfigs, ColorCode, WallHearts);

        var newWallObject = SetupNewWallInScene(wallSetup);

        if (newWallObject != null)
        {
            AddWallSetup(wallSetup);
        }
    }



    /// <summary>
    /// Set up the actual wall object in the scene
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <param name="colorCode">Color code for the wall</param>
    /// <returns>Tuple with the created WallObject component and list of new wall pixels</returns>
    private WallObject SetupNewWallInScene(WallObjectSetup setup)
    {
        WallObject wallObject = gridObject.CreateWallObject(setup);

        return wallObject;
    }

    /// <summary>
    /// Validates if the wall orientation is valid (horizontal or vertical only)
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <returns>True if wall orientation is valid, false otherwise</returns>
    public bool IsValidWallOrientation(List<PaintingPixelComponent> _wallPixels)
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
    /// Import all wall configurations from this setup to a PaintingConfig asset
    /// </summary>
    /// <param name="paintingConfig">The PaintingConfig to import to</param>
    public void ImportWallsToPaintingConfig(PaintingConfig paintingConfig)
    {
        if (paintingConfig == null)
        {
            Debug.LogError("PaintingConfig is null. Cannot import walls.");
            return;
        }

        // Clear existing wall setups in the config
        if (paintingConfig.WallSetups == null)
            paintingConfig.WallSetups = new List<WallObjectSetup>();
        else
            paintingConfig.WallSetups.Clear();

        // Copy all wall setups from this component to the config
        foreach (WallObjectSetup wallSetup in wallObjectSetups)
        {
            if (wallSetup != null)
            {
                // Add the wall setup to the painting config
                paintingConfig.WallSetups.Add(wallSetup);
            }
        }

        paintingConfig.HidePixelsUnderPipes();

        Debug.Log($"Imported {wallObjectSetups.Count} wall setups to PaintingConfig.");
    }

    public void Reload()
    {
        gridObject.ClearAllWalls();
        CurrentLevelWallObjectSetups = gridObject.paintingConfig.WallSetups;
        wallObjectSetups = new List<WallObjectSetup>(CurrentLevelWallObjectSetups);
        //return;
        foreach (var wall in CurrentLevelWallObjectSetups)
        {
            gridObject.CreateWallObject(wall);
        }
    }
}