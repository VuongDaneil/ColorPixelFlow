using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using NaughtyAttributes;

[System.Serializable]
public class IntPixelListPair
{
    public int key;
    public List<PaintingPixel> pixels;
    
    public IntPixelListPair(int key, List<PaintingPixel> pixels)
    {
        this.key = key;
        this.pixels = pixels;
    }
}

public class PaintingGridObject : MonoBehaviour
{
    #region PROPERTIES
    [Header("Grid Properties")]
    public Vector2 gridSize;
    public Transform Center;
    public List<PaintingPixel> paintingPixels;
    public ColorPalleteData colorPallete;

    // Lists for faster lookup by row and column (serializable)
    public List<IntPixelListPair> pixelsByRow;
    public List<IntPixelListPair> pixelsByColumn;

    public List<List<PaintingPixel>> Rows = new List<List<PaintingPixel>>();

    [Header("Pipes")]
    public List<PipeObject> pipeObjects;
    
    [Header("Pipe Object Pixels")]
    public List<PaintingPixel> pipeObjectsPixels;  // New list for pipe pixels that are outside the grid

    [Header("Walls")]
    public List<WallObject> wallObjects;

    [Header("Wall Object Pixels")]
    public List<PaintingPixel> wallObjectsPixels;

    [Header("Keys")]
    public List<KeyObject> keyObjects;

    [Header("Key Object Pixels")]
    public List<PaintingPixel> keyObjectsPixels;

    [Header("Grid Settings")]
    public float pixelArrangeSpace = 1.0f;
    public float pixelScaleFactor = 1.0f;
    public GameObject pixelPrefab;
    
    [Header("Painting Configuration")]
    [ReadOnly] public PaintingConfig paintingConfig;
    
    [Header("Color Variation")]
    [Range(0.0f, 1.0f)]
    public float colorVariationAmount = 0.0f;
    
    [Header("Default Prefabs")]
    public GameObject DefaultPipeHeadPrefab;
    public GameObject DefaultPipeBodyPrefab;
    public GameObject DefaultPipeTailPrefab;
    public GameObject WallObjectPrefab;
    public GameObject KeyObjectPrefab;
    #endregion

    #region UNITY CORE
    private void Awake()
    {
        RegisterEvent();

        ApplyPaintingConfig();
        // Initialize the mapping lists
        pixelsByRow = new List<IntPixelListPair>();
        pixelsByColumn = new List<IntPixelListPair>();

        // Initialize mappings from existing pixels if they exist
        InitializePixelMappings();

        // Initialize pipe objects list if it's null
        if (pipeObjects == null) pipeObjects = new List<PipeObject>();

        // Initialize pipe objects pixels list if it's null
        if (pipeObjectsPixels == null) pipeObjectsPixels = new List<PaintingPixel>();
    }

    private void OnDestroy()
    {
        UnregisterEvent();
    }
    #endregion

    #region MAIN

    #region _initialize
    // Method to apply painting configuration to the grid
    public void ApplyPaintingConfig()
    {
        if (paintingConfig == null)
        {
            Debug.LogWarning("No PaintingConfig assigned to apply.");
            return;
        }

        // Iterate through all pixel configs in the painting config
        foreach (PaintingPixelConfig pixelConfig in paintingConfig.Pixels)
        {
            // Apply color variation if enabled
            Color variedColor = ApplyColorVariation(pixelConfig.color, colorVariationAmount);

            // Update the pixel color and color code in the grid
            SetupPixelObject(pixelConfig.column, pixelConfig.row, variedColor, pixelConfig.colorCode, pixelConfig.Hidden);
        }

        // Apply wall configurations as well
        ApplyWallConfigurations();

        // Apply pipe configurations as well
        ApplyPipeConfigurations();

        // Apply key configurations as well
        ApplyKeyConfigurations();

    }
    #endregion

    #region _actions
    public void ShootPixel(PaintingPixel pixel)
    {
        pixel.DestroyPixel();

        // Trigger event to notify that grid pixels have changed
        GameplayEventsManager.OnGridPixelsChanged?.Invoke(this);
    }

    public void DestroyAllPixelsObjects()
    {
        for (int i = 0; i < paintingPixels.Count; i++)
        {
            if (paintingPixels[i] != null)
            {
                paintingPixels[i].DestroyObject();
            }
        }
        paintingPixels.Clear();

        // Destroy all pipe objects and clear the list
        for (int i = 0; i < pipeObjects.Count; i++)
        {
            if (pipeObjects[i] != null)
            {
                GameObject.Destroy(pipeObjects[i].gameObject);
            }
        }
        pipeObjects.Clear();

        // Clear pipe object pixels and destroy their GameObjects as well
        for (int i = 0; i < pipeObjectsPixels.Count; i++)
        {
            if (pipeObjectsPixels[i] != null && pipeObjectsPixels[i].pixelObject != null)
            {
                if (Application.isPlaying) GameObject.Destroy(pipeObjectsPixels[i].pixelObject);
                else GameObject.DestroyImmediate(pipeObjectsPixels[i].pixelObject);
            }
        }
        pipeObjectsPixels.Clear();

        // Clear the row and column mappings as well
        pixelsByRow.Clear();
        pixelsByColumn.Clear();
    }

    public List<PaintingPixel> SelectOutlinePixels()
    {
        List<PaintingPixel> outlinePixels = new List<PaintingPixel>();
        HashSet<PaintingPixel> addedPixels = new HashSet<PaintingPixel>();

        // First, check the pixels in each row to find min/max columns
        if (pixelsByRow != null)
        {
            foreach (var rowPair in pixelsByRow)
            {
                var rowPixels = rowPair.pixels;
                if (rowPixels != null && rowPixels.Count > 0)
                {
                    // Find min and max column for this row among non-destroyed pixels
                    int minCol = int.MaxValue;
                    int maxCol = int.MinValue;

                    foreach (var pixel in rowPixels)
                    {
                        if (pixel != null && !pixel.destroyed && !pixel.Hidden)
                        {
                            minCol = Mathf.Min(minCol, pixel.column);
                            maxCol = Mathf.Max(maxCol, pixel.column);
                        }
                    }

                    // Add the leftmost and rightmost pixels of this row to outline
                    if (minCol != int.MaxValue) // Check if we found any non-destroyed pixels
                    {
                        foreach (var pixel in rowPixels)
                        {
                            if (pixel != null && !pixel.destroyed && !pixel.Hidden && (pixel.column == minCol || pixel.column == maxCol))
                            {
                                if (addedPixels.Add(pixel)) // Add returns true if pixel was not already in the set
                                {
                                    outlinePixels.Add(pixel);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Next, check the pixels in each column to find min/max rows
        if (pixelsByColumn != null)
        {
            foreach (var colPair in pixelsByColumn)
            {
                var colPixels = colPair.pixels;
                if (colPixels != null && colPixels.Count > 0)
                {
                    // Find min and max row for this column among non-destroyed pixels
                    int minRow = int.MaxValue;
                    int maxRow = int.MinValue;

                    foreach (var pixel in colPixels)
                    {
                        if (pixel != null && !pixel.destroyed && !pixel.Hidden)
                        {
                            minRow = Mathf.Min(minRow, pixel.row);
                            maxRow = Mathf.Max(maxRow, pixel.row);
                        }
                    }

                    // Add the topmost and bottommost pixels of this column to outline
                    if (minRow != int.MaxValue) // Check if we found any non-destroyed pixels
                    {
                        foreach (var pixel in colPixels)
                        {
                            if (pixel != null && !pixel.destroyed && !pixel.Hidden && (pixel.row == minRow || pixel.row == maxRow))
                            {
                                if (addedPixels.Add(pixel)) // Add returns true if pixel was not already in the set
                                {
                                    outlinePixels.Add(pixel);
                                }
                            }
                        }
                    }
                }
            }
        }

        return outlinePixels;
    }

    public List<PaintingPixel> SelectOutlinePixelsWithColor(string colorCode)
    {
        List<PaintingPixel> outlinePixels = SelectOutlinePixels();

        if (paintingPixels == null || paintingPixels.Count == 0)
        {
            return outlinePixels;
        }
        var rs = outlinePixels.FindAll(x => (x.colorCode == colorCode || x.colorCode.Equals(PaintingSharedAttributes.KeyColorDefine))).ToList();
        return rs;
    }
    #endregion

    #region _grid
    // Generate the grid of pixels
    public void GenerateGrid(Transform centerPoint, float yoffset = 0)
    {
        Center = centerPoint;
        Vector3 centerPos = centerPoint.position;

        DestroyAllPixelsObjects();

        // Generate pixels
        for (int col = 0; col < (int)gridSize.x; col++) // Swapped row and col loops
        {
            for (int row = 0; row < (int)gridSize.y; row++) // Swapped row and col loops
            {
                // Calculate grid coordinates with center at [0,0] as per design document:
                // For a grid of 20 rows x 40 columns (example from design doc):
                // - Center point is [0, 0] 
                // - Far left point is [0, -20] (when rows=20, cols=40)
                // - Far right point is [0, 20] 
                // - Top most point is [10, 0]
                // - Lowest point is [-9, 0]
                // 
                // From this example, we can deduce:
                // For 20 rows: indices are from -10 to +9 (centered at 0)
                // For 40 columns: indices are from -20 to +19 (centered at 0)

                // Calculate actual grid coordinates based on design document specification
                // For any grid size, indices should be centered around [0, 0]
                int halfCols = (int)(gridSize.x / 2); // Swapped halfRows and halfCols
                int halfRows = (int)(gridSize.y / 2); // Swapped halfRows and halfCols

                // Handle both even and odd dimensions
                int gridCol = halfCols - col;  // Start from right (positive) to left (negative) - Swapped gridRow and gridCol
                if ((int)gridSize.x % 2 == 0)
                {
                    gridCol -= 1; // Adjust for even number of columns
                }

                int gridRow = row - halfRows;  // Start from bottom (negative) to top (positive) - Swapped gridRow and gridCol
                if ((int)gridSize.y % 2 == 0)
                {
                    // No adjustment needed for even number of rows in this implementation
                }

                // Calculate world position based on grid coordinates
                float xPos = centerPos.x + gridCol * pixelArrangeSpace;
                float zPos = centerPos.z + gridRow * pixelArrangeSpace; // Positive rows go "up" in z-axis
                Vector3 worldPos = CalculatePixelPosition(gridCol, gridRow, yoffset);

                // Instantiate pixel prefab in the scene as child of this object
                GameObject pixelGO = null;
                if (pixelPrefab != null)
                {
                    pixelGO = Instantiate(pixelPrefab, worldPos, Quaternion.identity, this.transform);
                    pixelGO.transform.localScale = pixelGO.transform.localScale * this.pixelScaleFactor;
                    // Set the GameObject name with the format "Pixel (column, row)"
                    pixelGO.name = $"Pixel ({gridCol}, {gridRow})";
                }

                // Create PaintingPixel with reference to its GameObject
                PaintingPixel pixel = new PaintingPixel(gridCol, gridRow, Color.white, worldPos, 1, hidden: false, pixelGO); // Swapped gridRow and gridCol parameters

                // Add to grid
                paintingPixels.Add(pixel);
                AddPixelToMappings(pixel);

                // Store reference to the pixel in the pixel gameobject if needed
                if (pixelGO != null)
                {
                    var pixelComponent = pixelGO.GetComponent<PaintingPixelComponent>();
                    if (pixelComponent != null)
                    {
                        pixelComponent.SetUp(pixel);
                    }
                }
            }
        }
    }
    // Initialize the row and column mapping lists from the existing paintingPixels
    private void InitializePixelMappings()
    {
        // Initialize the lists if they're null
        if (pixelsByRow == null)
        {
            pixelsByRow = new List<IntPixelListPair>();
        }
        else
        {
            pixelsByRow.Clear();
        }

        if (pixelsByColumn == null)
        {
            pixelsByColumn = new List<IntPixelListPair>();
        }
        else
        {
            pixelsByColumn.Clear();
        }

        // Populate the mappings with current painting pixels
        if (paintingPixels != null)
        {
            foreach (PaintingPixel pixel in paintingPixels)
            {
                if (pixel != null)
                {
                    // Add to row mapping
                    AddPixelToRowMapping(pixel.row, pixel);

                    // Add to column mapping
                    AddPixelToColumnMapping(pixel.column, pixel);
                }
            }
        }

        // Populate the mappings with pipe object pixels
        if (pipeObjectsPixels != null)
        {
            foreach (PaintingPixel pixel in pipeObjectsPixels)
            {
                if (pixel != null)
                {
                    // Add to row mapping
                    AddPixelToRowMapping(pixel.row, pixel);

                    // Add to column mapping
                    AddPixelToColumnMapping(pixel.column, pixel);
                }
            }
        }
    }
    public void InitializeGrid(Vector2 size, float arrangeSpace, GameObject prefab, float scaleFactor = 1.0f)
    {
        this.gridSize = size;
        this.pixelArrangeSpace = arrangeSpace;
        this.pixelPrefab = prefab;
        this.pixelScaleFactor = scaleFactor;

        if (paintingPixels == null) paintingPixels = new List<PaintingPixel>();
        else paintingPixels.Clear();

        if (pipeObjects == null) pipeObjects = new List<PipeObject>();
        else pipeObjects.Clear();
    }

    // Method to update a pixel's color and color code at a specific position using MaterialPropertyBlock
    public void SetupPixelObject(int column, int row, Color newColor, string colorCode, bool hidden)
    {
        PaintingPixel pixel = GetPixelAt(column, row);
        if (pixel != null)
        {
            pixel.SetUp(newColor, colorCode, hidden);
        }
    }

    // Helper method to add a pixel to row mapping
    private void AddPixelToRowMapping(int row, PaintingPixel pixel)
    {
        // Initialize the list if it's null
        if (pixelsByRow == null)
        {
            pixelsByRow = new List<IntPixelListPair>();
        }

        // Find if there's already a list for this row
        IntPixelListPair rowPair = pixelsByRow.Find(pair => pair.key == row);

        if (rowPair == null)
        {
            // Create a new list for this row
            List<PaintingPixel> newList = new List<PaintingPixel>();
            newList.Add(pixel);
            pixelsByRow.Add(new IntPixelListPair(row, newList));
        }
        else
        {
            // Add to existing list
            if (!rowPair.pixels.Contains(pixel))
            {
                rowPair.pixels.Add(pixel);
            }
        }
    }

    // Helper method to add a pixel to column mapping
    private void AddPixelToColumnMapping(int column, PaintingPixel pixel)
    {
        // Initialize the list if it's null
        if (pixelsByColumn == null)
        {
            pixelsByColumn = new List<IntPixelListPair>();
        }

        // Find if there's already a list for this column
        IntPixelListPair columnPair = pixelsByColumn.Find(pair => pair.key == column);

        if (columnPair == null)
        {
            // Create a new list for this column
            List<PaintingPixel> newList = new List<PaintingPixel>();
            newList.Add(pixel);
            pixelsByColumn.Add(new IntPixelListPair(column, newList));
        }
        else
        {
            // Add to existing list
            if (!columnPair.pixels.Contains(pixel))
            {
                columnPair.pixels.Add(pixel);
            }
        }
    }

    // Update the row and column mappings when a pixel is added
    private void AddPixelToMappings(PaintingPixel pixel)
    {
        if (pixel == null) return;

        // Add to row mapping
        AddPixelToRowMapping(pixel.row, pixel);

        // Add to column mapping
        AddPixelToColumnMapping(pixel.column, pixel);
    }
    #endregion

    #region _events
    private void RegisterEvent()
    {
        GameplayEventsManager.OnAPixelDestroyed += OnAPixelDestroyed;
    }

    private void UnregisterEvent()
    {
        GameplayEventsManager.OnAPixelDestroyed -= OnAPixelDestroyed;
    }

    public void OnAPixelDestroyed(PaintingPixel _pixel)
    {
        if (_pixel == null) return;

        foreach (PipeObject pipe in pipeObjects)
        {
            if (pipe.Destroyed || _pixel.colorCode != pipe.ColorCode) continue;
            if (pipe.PaintingPixelsCovered.Contains(_pixel))
            {
                pipe.OnAPixelDestroyed();
                return;
            }
        }

        foreach (WallObject wall in wallObjects)
        {
            if (wall.Destroyed || _pixel.colorCode != wall.ColorCode) continue;
            if (wall.PaintingPixelsCovered.Contains(_pixel))
            {
                wall.OnAPixelDestroyed();
                return;
            }
        }

        foreach (KeyObject key in keyObjects)
        {
            if (key.Collected || _pixel.colorCode != PaintingSharedAttributes.KeyColorDefine) continue;
            if (key.PaintingPixelsCovered.Contains(_pixel))
            {
                key.OnAPixelDestroyed();
                return;
            }
        }
    }
    #endregion

    #region _pipe objects
    // Method to apply pipe configurations to the grid
    public void ApplyPipeConfigurations()
    {
        if (paintingConfig == null || paintingConfig.PipeSetups == null)
        {
            Debug.LogWarning("No PaintingConfig or PipeSetups assigned to apply.");
            return;
        }

        // Clear existing pipe objects
        ClearAllPipes();

        // Create pipe objects based on the configurations in the painting config
        foreach (var pipeSetup in paintingConfig.PipeSetups)
        {
            if (pipeSetup != null && pipeSetup.PixelCovered != null && pipeSetup.PixelCovered.Count > 0)
            {
                // Create a new pipe object based on the setup
                CreatePipeObject(pipeSetup);
            }
        }
    }
    public void ClearAllPipes()
    {
        if (pipeObjects == null) pipeObjects = new List<PipeObject>();
        else
        {
            List<PipeObject> tmp = new List<PipeObject>(pipeObjects);
            foreach (var pipeObj in tmp)
            {
                RemovePipeObject(pipeObj);
            }
        }
        pipeObjects.Clear();
        pipeObjectsPixels.Clear();
    }

    // Helper method to create a pipe object from a pipe setup configuration
    public PipeObject CreatePipeObject(PipeObjectSetup pipeSetup)
    {
        if (pipeSetup.PixelCovered == null || pipeSetup.PixelCovered.Count < 2)
        {
            Debug.LogWarning("Pipe setup has less than 2 pixels. Cannot create pipe.");
            return null;
        }

        List<PaintingPixel> respectedPixels = new List<PaintingPixel>();
        Color pipeColor = colorPallete.GetColorByCode(pipeSetup.ColorCode);

        for (int i = 0; i < pipeSetup.PixelCovered.Count; i++)
        {
            PaintingPixelConfig pixelConfig = pipeSetup.PixelCovered[i];
            PaintingPixel respectedPixel = GetPixelAtGridPosition(pixelConfig.column, pixelConfig.row);
            if (respectedPixel == null)
            {
                PaintingPixel additionPixel = CreateNewPaintingPixel(pixelConfig, true);
                additionPixel.color = pipeColor;
                additionPixel.colorCode = pipeSetup.ColorCode;
                if (additionPixel != null) respectedPixels.Add(additionPixel);
            }
            else
            {
                respectedPixels.Add(respectedPixel);
            }
        }

        // Determine if the pipe is horizontal or vertical based on the first and last pixels
        // Get the head and tail positions from the first and last pixels
        PaintingPixel headPixel = respectedPixels[0];
        PaintingPixel tailPixel = respectedPixels[^1];
        bool isHorizontal = headPixel.row == tailPixel.row;

        // Create the pipe game object with head transform as parent
        GameObject pipeGO = new GameObject($"PIPE_OBJECT_{pipeSetup.ColorCode}");
        pipeGO.transform.SetParent(this.transform);
        pipeGO.transform.localPosition = Vector3.zero;

        // Create the head at the first pixel position
        GameObject headGO = Instantiate(DefaultPipeHeadPrefab, headPixel.worldPos, Quaternion.identity, pipeGO.transform);
        headGO.name = "PipeHead";

        // Apply direct scale to the head
        headGO.transform.localScale = pipeSetup.Scale;
        headGO.transform.localPosition = headPixel.worldPos;

        Transform headTransform = headGO.transform;

        // Get the PipeObject component from the head
        PipeObject pipeObject = headTransform.GetComponent<PipeObject>();
        if (pipeObject == null)
        {
            pipeObject = headGO.AddComponent<PipeObject>();
        }

        // Change the color of the head part using PipePartVisualHandle
        PipePartVisualHandle headVisualHandle = headGO.GetComponent<PipePartVisualHandle>();
        if (headVisualHandle == null)
            headVisualHandle = headGO.GetComponentInChildren<PipePartVisualHandle>();
        if (headVisualHandle != null)
            headVisualHandle.SetColor(pipeColor);

        // Initialize the list for body parts
        List<Transform> bodyParts = new List<Transform>
        {
            headGO.transform
        };

        // Create body parts for the middle pixels
        for (int i = 1; i < pipeSetup.PixelCovered.Count - 1; i++)
        {
            GameObject bodyGO = Instantiate(DefaultPipeBodyPrefab, respectedPixels[i].worldPos, Quaternion.identity, pipeGO.transform);
            bodyGO.name = "PipeBody";
            // Apply direct scale to the body part
            bodyGO.transform.localScale = pipeSetup.Scale;
            bodyGO.transform.localPosition = respectedPixels[i].worldPos;
            bodyParts.Add(bodyGO.transform);

            // Change the color of the body part using PipePartVisualHandle
            PipePartVisualHandle visualHandle = bodyGO.GetComponent<PipePartVisualHandle>();
            if (visualHandle == null)
                visualHandle = bodyGO.GetComponentInChildren<PipePartVisualHandle>();
            if (visualHandle != null)
                visualHandle.SetColor(pipeColor);
        }

        // Create tail at the last pixel position
        GameObject tailGO = Instantiate(DefaultPipeTailPrefab, tailPixel.worldPos, Quaternion.identity, pipeGO.transform);
        tailGO.name = "PipeTail";
        // Apply direct scale to the tail
        tailGO.transform.localScale = pipeSetup.Scale;
        tailGO.transform.localPosition = tailPixel.worldPos;
        bodyParts.Add(tailGO.transform);

        // Change the color of the tail part using PipePartVisualHandle
        PipePartVisualHandle tailVisualHandle = tailGO.GetComponent<PipePartVisualHandle>();
        if (tailVisualHandle == null)
            tailVisualHandle = tailGO.GetComponentInChildren<PipePartVisualHandle>();
        if (tailVisualHandle != null)
            tailVisualHandle.SetColor(pipeColor);

        // Add the pipe setup pixels to the pipeObjectsPixels list if they're not already there
        if (pipeObjectsPixels == null) pipeObjectsPixels = new List<PaintingPixel>();
        List<PaintingPixel> newpipePixels = new List<PaintingPixel>();

        foreach (PaintingPixel pixel in respectedPixels)
        {
            PaintingPixel tmp = CreatePipePixel(pixel);
            if (tmp != null)
            {
                newpipePixels.Add(tmp);
                tmp.PixelComponent?.ApplyPosition();
            }
        }

        //foreach (var pipePart in newpipePixels) pipePart.PixelComponent?.ApplyPosition();

        pipeObject.Initialize(headTransform, bodyParts, newpipePixels, pipeSetup.ColorCode, isHorizontal);
        pipeObject.ApplyOrientationRotation();
        pipeObjects.Add(pipeObject);
        return pipeObject;
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
    private PaintingPixel CreatePipePixel(PaintingPixel stock)
    {
        // Create a GameObject for the pipe pixel
        GameObject pipePixelGO = new GameObject($"PipePixel ({stock.column}, {stock.row})");
        pipePixelGO.transform.SetParent(transform);
        pipePixelGO.transform.position = stock.worldPos;

        // Add a PaintingPixelComponent to the GameObject
        PaintingPixelComponent pipePixelComponent = pipePixelGO.AddComponent<PaintingPixelComponent>();

        // Create the PaintingPixel object
        PaintingPixel pipePixel = new PaintingPixel(stock.column, stock.row, stock.color, stock.worldPos, 1, hidden: false, pipePixelGO);
        pipePixel.SetUp(stock.color, stock.colorCode, false); // Set both color and color code

        // Set the pixel data for the component
        pipePixelComponent.SetUp(pipePixel);

        // Add the pipe pixel to the grid object's list of pipe pixels
        if (!pipeObjectsPixels.Contains(pipePixel))
        {
            pipeObjectsPixels.Add(pipePixel);
            return pipePixel;
        }
        return null;
    }

    public void RemovePipeObject(PipeObject _pipe)
    {
        if (_pipe == null) return;
        if (pipeObjects.Contains(_pipe))
        {
            foreach (var coverPixel in _pipe.PaintingPixelsCovered)
            {
                if (pipeObjectsPixels.Contains(coverPixel)) pipeObjectsPixels.Remove(coverPixel);
            }
            _pipe.SelfDestroy();
            pipeObjects.Remove(_pipe);
        }
    }
    #endregion

    #region _walls
    public void ApplyWallConfigurations()
    {
        if (paintingConfig == null || paintingConfig.WallSetups == null)
        {
            Debug.LogWarning("No PaintingConfig or WallSetups assigned to apply.");
            return;
        }

        // Clear existing pipe objects
        ClearAllWalls();

        // Create pipe objects based on the configurations in the painting config
        foreach (var wallSetup in paintingConfig.WallSetups)
        {
            if (wallSetup != null)
            {
                // Create a new pipe object based on the setup
                CreateWallObject(wallSetup);
            }
        }
    }
    public WallObject CreateWallObject(WallObjectSetup wallSetup)
    {
        if (wallSetup.PixelCovered == null || wallSetup.PixelCovered.Count <= 1)
        {
            Debug.LogWarning("Cannot create wall.");
            return null;
        }

        List<PaintingPixel> wallPixels = new List<PaintingPixel>();

        foreach (PaintingPixelConfig pixelConfig in wallSetup.PixelCovered)
        {
            PaintingPixel respectedPixel = GetPixelAtGridPosition(pixelConfig.column, pixelConfig.row);
            if (respectedPixel != null)
            {
                respectedPixel.colorCode = wallSetup.ColorCode;
                wallPixels.Add(respectedPixel);
            }
        }

        if (wallPixels.Count != wallSetup.PixelCovered.Count) return null;

        Vector3 wallPosition = GetCenterByBoundingBox(wallPixels.Select(p => p.PixelComponent).ToList());

        // Create the pipe game object with head transform as parent
        GameObject wallGO = Instantiate(WallObjectPrefab, wallPosition, Quaternion.identity, transform);
        wallGO.name = $"WALL_OBJECT_{wallSetup.ColorCode}";

        Color wallColor = colorPallete.GetColorByCode(wallSetup.ColorCode);

        // Get the PipeObject component from the head
        WallObject wallObject = wallGO.GetComponent<WallObject>();
        if (wallObject == null)
        {
            wallObject = wallGO.AddComponent<WallObject>();
        }
        (int height, int width) = GetShapeSize(wallPixels);
        Vector3 defaultPixelScale = wallPixels[0].pixelObject.transform.localScale;
        Vector3 wallScale = new Vector3(defaultPixelScale.x * width, defaultPixelScale.y, defaultPixelScale.z * height);
        wallGO.transform.localScale = wallScale;
        // Hide all pixel that covered by wall
        for (int i = 0; i < wallPixels.Count; i++)
        {
            wallPixels[i].PixelComponent?.HideVisualOnly();
        }

        // Add the pipe setup pixels to the pipeObjectsPixels list if they're not already there
        if (wallObjectsPixels == null) wallObjectsPixels = new List<PaintingPixel>();
        foreach (PaintingPixel pixel in wallPixels)
        {
            if (!wallObjectsPixels.Contains(pixel))
            {
                wallObjectsPixels.Add(pixel);
            }
        }

        wallObject.Initialize(wallPixels, wallSetup.Hearts, wallColor, wallSetup.ColorCode);
        wallObjects.Add(wallObject);
        return wallObject;
    }

    public void ClearAllWalls()
    {
        if (wallObjects == null) wallObjects = new List<WallObject>();
        else
        {
            List<WallObject> tmp = new List<WallObject>(wallObjects);
            foreach (var wallObj in tmp)
            {
                RemoveWallObject(wallObj);
            }
        }
        wallObjects.Clear();
        wallObjectsPixels.Clear();
    }

    public void RemoveWallObject(WallObject _wall)
    {
        if (_wall == null) return;
        if (wallObjects.Contains(_wall))
        {
            foreach (var coverPixel in _wall.PaintingPixelsCovered)
            {
                if (wallObjectsPixels.Contains(coverPixel))
                {
                    coverPixel.PixelComponent?.ShowVisualOnly();
                    wallObjectsPixels.Remove(coverPixel);
                }
            }
            _wall.SelfDestroy();
            wallObjects.Remove(_wall);
        }
    }
    #endregion

    #region _keys
    public void ApplyKeyConfigurations()
    {
        if (paintingConfig == null || paintingConfig.KeySetups == null)
        {
            Debug.LogWarning("No PaintingConfig or KeySetups assigned to apply.");
            return;
        }

        // Clear existing key objects
        ClearAllKeys();

        // Create key objects based on the configurations in the painting config
        foreach (var KeySetup in paintingConfig.KeySetups)
        {
            if (KeySetup != null)
            {
                // Create a new key object based on the setup
                CreateKeyObject(KeySetup);
            }
        }
    }

    public KeyObject CreateKeyObject(KeyObjectSetup keySetup)
    {
        if (keySetup.PixelCovered == null || keySetup.PixelCovered.Count <= 0)
        {
            Debug.LogWarning("Cannot create wall.");
            return null;
        }

        List<PaintingPixel> keyPixels = new List<PaintingPixel>();

        foreach (PaintingPixelConfig pixelConfig in keySetup.PixelCovered)
        {
            PaintingPixel respectedPixel = GetPixelAtGridPosition(pixelConfig.column, pixelConfig.row);
            if (respectedPixel != null)
            {
                respectedPixel.colorCode = keySetup.ColorCode;
                keyPixels.Add(respectedPixel);
            }
        }

        if (keyPixels.Count != keySetup.PixelCovered.Count) return null;

        Vector3 keyPosition = GetCenterByBoundingBox(keyPixels.Select(p => p.PixelComponent).ToList());

        // Create the pipe game object with head transform as parent
        GameObject keyGO = Instantiate(KeyObjectPrefab, keyPosition, Quaternion.identity, transform);
        keyGO.name = $"KEY_OBJECT";

        // Get the PipeObject component from the head
        KeyObject keyObject = keyGO.GetComponent<KeyObject>();
        if (keyObject == null)
        {
            keyObject = keyGO.AddComponent<KeyObject>();
        }

        // Hide all pixel that covered by key
        for (int i = 0; i < keyPixels.Count; i++)
        {
            keyPixels[i].PixelComponent?.HideVisualOnly();
        }

        // Add the pipe setup pixels to the pipeObjectsPixels list if they're not already there
        if (keyObjectsPixels == null) keyObjectsPixels = new List<PaintingPixel>();
        foreach (PaintingPixel pixel in keyPixels)
        {
            if (!keyObjectsPixels.Contains(pixel))
            {
                keyObjectsPixels.Add(pixel);
            }
        }

        keyObject.Initialize(keyPixels);
        keyObjects.Add(keyObject);
        return keyObject;
    }

    public void ClearAllKeys()
    {
        if (keyObjects == null) keyObjects = new List<KeyObject>();
        else
        {
            List<KeyObject> tmp = new List<KeyObject>(keyObjects);
            foreach (var keyObj in tmp)
            {
                RemoveKeyObject(keyObj);
            }
        }
        keyObjects.Clear();
        keyObjectsPixels.Clear();
    }

    public void RemoveKeyObject(KeyObject _key)
    {
        if (_key == null) return;
        if (keyObjects.Contains(_key))
        {
            foreach (var coverPixel in _key.PaintingPixelsCovered)
            {
                if (keyObjectsPixels.Contains(coverPixel))
                {
                    coverPixel.PixelComponent?.ShowVisualOnly();
                    keyObjectsPixels.Remove(coverPixel);
                }
            }
            _key.SelfDestroy();
            keyObjects.Remove(_key);
        }
    }
    #endregion

    #endregion

    #region SUPPORTIVE
    private Vector3 GetCenterByBoundingBox(List<PaintingPixelComponent> points)
    {
        if (points == null || points.Count == 0)
            return Vector3.zero;

        if (points.Count == 1) return points[0].PixelData.worldPos;

        float minX = points.Min(p => p.PixelData.worldPos.x);
        float maxX = points.Max(p => p.PixelData.worldPos.x);
        float minY = points.Min(p => p.PixelData.worldPos.y);
        float maxY = points.Max(p => p.PixelData.worldPos.y);
        float minZ = points.Min(p => p.PixelData.worldPos.z);
        float maxZ = points.Max(p => p.PixelData.worldPos.z);

        // trung tâm bounding box
        return new Vector3(
            (minX + maxX) * 0.5f + transform.position.x,
            (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f + transform.position.z
        );
    }
    public (int rowCount, int columnCount) GetShapeSize(List<PaintingPixel> pixels)
    {
        if (pixels == null || pixels.Count == 0)
            return (0, 0);

        if (pixels.Count == 1) return (1, 1);

        int minRow = pixels.Min(p => p.row);
        int maxRow = pixels.Max(p => p.row);
        int minCol = pixels.Min(p => p.column);
        int maxCol = pixels.Max(p => p.column);

        int rowCount = maxRow - minRow + 1;
        int columnCount = maxCol - minCol + 1;

        return (rowCount, columnCount);
    }
    public PaintingPixel GetPixelAtGridPosition(int column, int row)
    {
        foreach (PaintingPixel pixel in paintingPixels)
        {
            if (pixel.column == column && pixel.row == row)
            {
                return pixel;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a specific pixel from a row by its index in the row's pixel list
    /// </summary>
    /// <param name="row">The row index to search in</param>
    /// <param name="pixelIndexInRow">The index of the pixel within the row's list</param>
    /// <returns>The pixel at the specified position, or null if not found</returns>
    public PaintingPixel GetRespectedPixelFromRows(int row, int pixelIndexInRow)
    {
        if (pixelsByRow == null)
        {
            return null; // Return null if mappings not initialized
        }

        IntPixelListPair rowPair = pixelsByRow.Find(pair => pair.key == row);
        if (rowPair != null && pixelIndexInRow >= 0 && pixelIndexInRow < rowPair.pixels.Count)
        {
            return rowPair.pixels[pixelIndexInRow];
        }
        return null;
    }

    /// <summary>
    /// Gets a specific pixel from a column by its index in the column's pixel list
    /// </summary>
    /// <param name="column">The column index to search in</param>
    /// <param name="pixelIndexInColumn">The index of the pixel within the column's list</param>
    /// <returns>The pixel at the specified position, or null if not found</returns>
    public PaintingPixel GetRespectedPixelFromColumns(int column, int pixelIndexInColumn)
    {
        if (pixelsByColumn == null)
        {
            return null; // Return null if mappings not initialized
        }

        IntPixelListPair columnPair = pixelsByColumn.Find(pair => pair.key == column);
        if (columnPair != null && pixelIndexInColumn >= 0 && pixelIndexInColumn < columnPair.pixels.Count)
        {
            return columnPair.pixels[pixelIndexInColumn];
        }
        return null;
    }
    /// <summary>
    /// Gets all pixels in a specific row
    /// </summary>
    /// <param name="row">The row index to get pixels from</param>
    /// <returns>List of pixels in the specified row, or empty list if row doesn't exist</returns>
    public List<PaintingPixel> GetPixelsInRow(int row)
    {
        if (pixelsByRow == null)
        {
            return new List<PaintingPixel>(); // Return empty list if mappings not initialized
        }

        IntPixelListPair rowPair = pixelsByRow.Find(pair => pair.key == row);
        if (rowPair == null)
        {
            return new List<PaintingPixel>(); // Return empty list if row doesn't exist
        }
        return new List<PaintingPixel>(rowPair.pixels);
    }

    /// <summary>
    /// Gets all pixels in a specific column
    /// </summary>
    /// <param name="column">The column index to get pixels from</param>
    /// <returns>List of pixels in the specified column, or empty list if column doesn't exist</returns>
    public List<PaintingPixel> GetPixelsInColumn(int column)
    {
        if (pixelsByColumn == null)
        {
            return new List<PaintingPixel>(); // Return empty list if mappings not initialized
        }

        IntPixelListPair columnPair = pixelsByColumn.Find(pair => pair.key == column);
        if (columnPair == null)
        {
            return new List<PaintingPixel>(); // Return empty list if column doesn't exist
        }
        return new List<PaintingPixel>(columnPair.pixels);
    }

    // Helper method to apply random variation to a color
    private Color ApplyColorVariation(Color originalColor, float variationAmount)
    {
        if (variationAmount <= 0f)
        {
            return originalColor;
        }

        // Generate random variations within the specified range for each color component
        float rVariation = Random.Range(-variationAmount, variationAmount);
        float gVariation = Random.Range(-variationAmount, variationAmount);
        float bVariation = Random.Range(-variationAmount, variationAmount);

        // Apply the variations to each color component, ensuring values stay within [0, 1] range
        float newR = Mathf.Clamp01(originalColor.r + rVariation);
        float newG = Mathf.Clamp01(originalColor.g + gVariation);
        float newB = Mathf.Clamp01(originalColor.b + bVariation);

        // Return the new color with applied variation (keeping the original alpha)
        return new Color(newR, newG, newB, originalColor.a);
    }

    public void AddPixel(PaintingPixel pixel)
    {
        if (pixel != null)
        {
            paintingPixels.Add(pixel);
            AddPixelToMappings(pixel);
        }
    }

    public PaintingPixel GetPixelAt(int column, int row)
    {
        foreach (PaintingPixel pixel in paintingPixels)
        {
            if (pixel.column == column && pixel.row == row)
            {
                return pixel;
            }
        }
        return null;
    }
    public int GetTotalPixels()
    {
        return paintingPixels.Count;
    }

    public int GetRemainingPixels()
    {
        int count = 0;
        foreach (PaintingPixel pixel in paintingPixels)
        {
            if (pixel != null && !pixel.destroyed && !pixel.Hidden)
            {
                count++;
            }
        }
        return count;
    }
    public void ClearToWhite()
    {
        foreach (PaintingPixel pixel in paintingPixels)
        {
            if (pixel != null)
            {
                pixel.PixelComponent?.SetColor(Color.white);
                pixel.ShowPixelObject();
            }
        }
    }
    
    // Calculate world position based on grid coordinates
    public Vector3 CalculatePixelPosition(int col, int row, float yOffset = 0)
    {
        float xPos = Center.position.x + col * pixelArrangeSpace;
        float zPos = Center.position.z + row * pixelArrangeSpace; // Positive rows go "up" in z-axis
        xPos = xPos - transform.position.x; //with parent offset
        return new Vector3(xPos, Center.position.y + yOffset, zPos);
    }
    public PaintingPixel CreateNewPaintingPixel(PaintingPixelConfig pixelConfig, bool calculatePositon = false)
    {
        PaintingPixel pixel = new PaintingPixel
        {
            column = pixelConfig.column,
            row = pixelConfig.row,
            color = pixelConfig.color,
            colorCode = pixelConfig.colorCode,
            Hidden = pixelConfig.Hidden
        };
        if (calculatePositon)
        {
            pixel.worldPos = CalculatePixelPosition(pixel.column, pixel.row);
        }

        return pixel;
    }
    #endregion
}