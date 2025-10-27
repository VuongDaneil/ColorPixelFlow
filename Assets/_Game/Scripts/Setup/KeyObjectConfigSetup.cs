using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KeyObjectConfigSetup : MonoBehaviour
{
    [Header("Key Configuration")]
    public List<KeyObjectSetup> CurrentLevelKeyObjectSetups; // To store all the key that being setup
    public List<KeyObjectSetup> keyObjectSetups; // To store all the pipe that being setup

    [Header("References")]
    public PaintingGridObject gridObject; // GridObject (PaintingGridObject.cs)

    [Header("Prefabs")]
    public GameObject KeyPrefab;

    [Header("Key Properties")]
    public ColorPalleteData colorPalette; // The color source that pipe will get from

    [Header("Pipe Setup")]
    public List<PaintingPixelComponent> KeyPixelComponents;

    private void Awake()
    {
        if (keyObjectSetups == null) keyObjectSetups = new List<KeyObjectSetup>();
    }

    /// <summary>
    /// Add a pipe setup to the list
    /// </summary>
    /// <param name="keySetup">The pipe setup to add</param>
    public void AddKeySetup(KeyObjectSetup keySetup)
    {
        if (keyObjectSetups == null) keyObjectSetups = new List<KeyObjectSetup>();
        if (keySetup != null && !keyObjectSetups.Contains(keySetup))
        {
            keyObjectSetups.Add(keySetup);
        }
    }

    /// <summary>
    /// Remove a pipe setup from the list
    /// </summary>
    /// <param name="keySetup">The key setup to remove</param>
    public void RemoveKeySetup(KeyObjectSetup keySetup)
    {
        if (keySetup != null)
        {
            keyObjectSetups.Remove(keySetup);
        }
    }

    /// <summary>
    /// Clear all key setups
    /// </summary>
    public void ClearKeySetups()
    {
        keyObjectSetups.Clear();
    }

    /// <summary>
    /// Create a key between StartPixel and EndPixel based on current settings
    /// </summary>
    public void CreateKey()
    {
        if (KeyPixelComponents == null || KeyPixelComponents.Count <= 0)
        {
            Debug.LogWarning("KeyPixelComponents is not valid. Cannot create key.");
            return;
        }

        // Validate that the key should be straight (horizontal or vertical)
        if (!IsValidKeyOrientation(KeyPixelComponents))
        {
            Debug.LogWarning("Key must be either horizontal (same row) or vertical (same column). Cannot create key.");
            return;
        }

        // Create and setup the key in the scene - this will also create the key pixels
        List<PaintingPixelConfig> keyPixelConfigs = new List<PaintingPixelConfig>();
        foreach (var pixelComponent in KeyPixelComponents)
        {
            keyPixelConfigs.Add(new PaintingPixelConfig(pixelComponent.PixelData));
        }
        KeyObjectSetup keySetup = new KeyObjectSetup(keyPixelConfigs);

        var newKeyObject = SetupNewKeyInScene(keySetup);

        if (newKeyObject != null)
        {
            AddKeySetup(keySetup);
        }
    }

    public void CreateKey(List<PaintingPixelComponent> _keyPixels)
    {
        if (_keyPixels == null || _keyPixels.Count <= 0)
        {
            Debug.LogWarning("_keyPixels is not valid. Cannot create key.");
            return;
        }

        // Validate that the key should be straight (horizontal or vertical)
        if (!IsValidKeyOrientation(_keyPixels))
        {
            Debug.LogWarning("Key must be either horizontal (same row) or vertical (same column). Cannot create key.");
            return;
        }

        // Create and setup the key in the scene - this will also create the key pixels
        List<PaintingPixelConfig> keyPixelConfigs = new List<PaintingPixelConfig>();
        foreach (var pixelComponent in _keyPixels)
        {
            keyPixelConfigs.Add(new PaintingPixelConfig(pixelComponent.PixelData));
        }
        KeyObjectSetup keySetup = new KeyObjectSetup(keyPixelConfigs);

        var newKeyObject = SetupNewKeyInScene(keySetup);

        if (newKeyObject != null)
        {
            AddKeySetup(keySetup);
        }
    }

    /// <summary>
    /// Set up the actual key object in the scene
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <param name="colorCode">Color code for the key</param>
    /// <returns>Tuple with the created PipeObject component and list of new key pixels</returns>
    private KeyObject SetupNewKeyInScene(KeyObjectSetup setup)
    {
        KeyObject keyObject = gridObject.CreateKeyObject(setup);

        return keyObject;
    }

    /// <summary>
    /// Validates if the pipe orientation is valid (horizontal or vertical only)
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    public bool IsValidKeyOrientation(List<PaintingPixelComponent> _keyPixels)
    {
        if (_keyPixels == null || _keyPixels.Count == 0)
            return false;

        if (_keyPixels.Count == 1) return true; // A single pixel is always valid

        int minRow = _keyPixels.Min(p => p.PixelData.row);
        int maxRow = _keyPixels.Max(p => p.PixelData.row);
        int minCol = _keyPixels.Min(p => p.PixelData.column);
        int maxCol = _keyPixels.Max(p => p.PixelData.column);

        int width = maxCol - minCol + 1;
        int height = maxRow - minRow + 1;
        int expectedCount = width * height;

        if (_keyPixels.Count != expectedCount)
            return false;

        HashSet<(int, int)> pointSet = _keyPixels
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
    public void ImportKeysToPaintingConfig(PaintingConfig paintingConfig)
    {
        if (paintingConfig == null)
        {
            Debug.LogError("PaintingConfig is null. Cannot import pipes.");
            return;
        }

        // Clear existing pipe setups in the config
        if (paintingConfig.KeySetups == null)
            paintingConfig.KeySetups = new List<KeyObjectSetup>();
        else
            paintingConfig.KeySetups.Clear();

        // Copy all pipe setups from this component to the config
        foreach (KeyObjectSetup pipeSetup in keyObjectSetups)
        {
            if (pipeSetup != null)
            {
                // Add the pipe setup to the painting config
                paintingConfig.KeySetups.Add(pipeSetup);
            }
        }

        paintingConfig.HidePixelsUnderPipes();

        Debug.Log($"Imported {keyObjectSetups.Count} key setups to PaintingConfig.");
    }

    public void Reload()
    {
        gridObject.ClearAllKeys();
        CurrentLevelKeyObjectSetups = gridObject.paintingConfig.KeySetups;
        keyObjectSetups = new List<KeyObjectSetup>(CurrentLevelKeyObjectSetups);
        //return;
        foreach (KeyObjectSetup pipe in CurrentLevelKeyObjectSetups)
        {
            gridObject.CreateKeyObject(pipe);
        }
    }
}
