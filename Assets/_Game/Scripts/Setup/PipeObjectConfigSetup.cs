using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class PipeObjectConfigSetup : MonoBehaviour
{
    [Header("Pipe Configuration")]
    public List<PipeObjectSetup> CurrentLevelObjectSetups; // To store all the pipe that being setup
    public List<PipeObjectSetup> pipeObjectSetups; // To store all the pipe that being setup
    
    [Header("References")]
    [ReadOnly] public PaintingGridObject gridObject; // GridObject (PaintingGridObject.cs)
    
    [Header("Prefabs")]
    public GameObject PipeHeadPrefab;     // Head of pipe, spawn at head pixel position
    public GameObject PipeBodyPrefab;     // Body part of pipe, spawn between head and tail
    public GameObject PipeTailPrefab;     // Tail of pipe, spawn at tail pixel position
    
    [Header("Pipe Properties")]
    public string ColorCode = "Default"; // Color of the pipe
    public ColorPalleteData colorPalette; // The color source that pipe will get from
    
    [Header("Pipe Scale")]
    public Vector3 Scale = Vector3.one;   // Scale applied to pipe parts (direct scale values)
    
    [Header("Pipe Positioning")]
    public int PipeSpaceFromGrid = 1;     // Space from grid for placing pipes outside the grid
    
    [Header("Pipe Setup")]
    public PaintingPixelComponent StartPixelComponent;      // Pixel component in grid that start this pipe (head)
    public PaintingPixelComponent EndPixelComponent;        // Pixel component in grid that end this pipe (tail)
    
    private void Awake()
    {
        if (pipeObjectSetups == null)
            pipeObjectSetups = new List<PipeObjectSetup>();
    }

    /// <summary>
    /// Add a pipe setup to the list
    /// </summary>
    /// <param name="pipeSetup">The pipe setup to add</param>
    public void AddPipeSetup(PipeObjectSetup pipeSetup)
    {
        if (pipeSetup != null && !pipeObjectSetups.Contains(pipeSetup))
        {
            pipeObjectSetups.Add(pipeSetup);
        }
    }
    
    /// <summary>
    /// Remove a pipe setup from the list
    /// </summary>
    /// <param name="pipeSetup">The pipe setup to remove</param>
    public void RemovePipeSetup(PipeObjectSetup pipeSetup)
    {
        if (pipeSetup != null)
        {
            pipeObjectSetups.Remove(pipeSetup);
        }
    }
    
    /// <summary>
    /// Clear all pipe setups
    /// </summary>
    public void ClearPipeSetups()
    {
        pipeObjectSetups.Clear();
    }

    public void ClearAllPipeSetups()
    {
        ClearPipeSetups();

        // Also clear pipe objects from the grid if they exist
        if (gridObject != null && gridObject.pipeObjects != null)
        {
            // Destroy the pipe gameobjects
            List<PipeObject> currentPipes = new List<PipeObject>(gridObject.pipeObjects);
            foreach (var pipeObj in currentPipes)
            {
                gridObject.RemovePipeObject(pipeObj);
            }
            gridObject.pipeObjects.Clear();
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (gameObject != null)
            EditorUtility.SetDirty(gameObject);
#endif
    }
    
    /// <summary>
    /// Create a pipe between StartPixel and EndPixel based on current settings
    /// </summary>
    public void CreatePipe()
    {
        if (StartPixelComponent == null || EndPixelComponent == null)
        {
            Debug.LogWarning("StartPixelComponent or EndPixelComponent is null. Cannot create pipe.");
            return;
        }
        
        // Get the PaintingPixel from the components
        PaintingPixel startPixel = StartPixelComponent.PixelData;
        PaintingPixel endPixel = EndPixelComponent.PixelData;
        
        if (startPixel == null || endPixel == null)
        {
            Debug.LogWarning("Could not get PaintingPixel data from components. Cannot create pipe.");
            return;
        }
        
        if (gridObject == null)
        {
            Debug.LogWarning("GridObject reference is null. Cannot create pipe.");
            return;
        }
        
        // Validate that the pipe should be straight (horizontal or vertical)
        if (!IsValidPipeOrientation(startPixel, endPixel))
        {
            Debug.LogWarning("Pipe must be either horizontal (same row) or vertical (same column). Cannot create pipe.");
            return;
        }

        // Create and setup the pipe in the scene - this will also create the pipe pixels
        List<PaintingPixelConfig> pipePixelConfigs = GetPaintingPixelsInBetweenWithSpace(startPixel, endPixel);
        PipeObjectSetup wallSetup = new PipeObjectSetup(pipePixelConfigs, ColorCode, Scale);

        var newWallObject = SetupNewPipeInScene(wallSetup);

        if (newWallObject != null)
        {
            AddPipeSetup(wallSetup);
        }
    }


    public void CreatePipe(PaintingPixel startPixel, PaintingPixel endPixel)
    {
        if (startPixel == null || endPixel == null)
        {
            Debug.LogWarning("StartPixelComponent or EndPixelComponent is null. Cannot create pipe.");
            return;
        }

        if (startPixel == null || endPixel == null)
        {
            Debug.LogWarning("Could not get PaintingPixel data from components. Cannot create pipe.");
            return;
        }

        if (gridObject == null)
        {
            Debug.LogWarning("GridObject reference is null. Cannot create pipe.");
            return;
        }

        // Validate that the pipe should be straight (horizontal or vertical)
        if (!IsValidPipeOrientation(startPixel, endPixel))
        {
            Debug.LogWarning("Pipe must be either horizontal (same row) or vertical (same column). Cannot create pipe.");
            return;
        }

        // Create and setup the pipe in the scene - this will also create the pipe pixels
        List<PaintingPixelConfig> pipePixelConfigs = GetPaintingPixelsInBetween(startPixel, endPixel);
        PipeObjectSetup wallSetup = new PipeObjectSetup(pipePixelConfigs, ColorCode, Scale);

        var newWallObject = SetupNewPipeInScene(wallSetup);

        if (newWallObject != null)
        {
            AddPipeSetup(wallSetup);
        }
    }

    public List<PaintingPixelConfig> GetPaintingPixelsInBetweenWithSpace(PaintingPixel startPixel, PaintingPixel endPixel)
    {
        List<PaintingPixelConfig> pixelsInBetween = new List<PaintingPixelConfig>();

        // Determine if pipe is horizontal or vertical
        bool isHorizontal = startPixel.row == endPixel.row;
        bool isVertical = startPixel.column == endPixel.column;

        // Create new PaintingPixel and PaintingPixelComponent for the head of the pipe (outside the grid)
        // First get the actual grid pixel for head to make sure we have the correct world position
        PaintingPixelConfig actualGridHeadPixel = new PaintingPixelConfig(gridObject.GetPixelAt(startPixel.column, startPixel.row));
        int currentPixelColumn = startPixel.column;
        int currentPixelRow = startPixel.row;

        if (isHorizontal)
        {
            if (startPixel.row < 0) currentPixelRow -= PipeSpaceFromGrid;
            else currentPixelRow += PipeSpaceFromGrid;
        }
        else
        {
            if (startPixel.column < 0) currentPixelColumn -= PipeSpaceFromGrid;
            else currentPixelColumn += PipeSpaceFromGrid;
        }

        PaintingPixelConfig headPipePixel = new PaintingPixelConfig()
        {
            column = currentPixelColumn,
            row = currentPixelRow,
            color = colorPalette.GetColorByCode(ColorCode),
            colorCode = ColorCode,
            Hidden = false,
        };
        pixelsInBetween.Add(headPipePixel); // Add the head pipe pixel first

        if (isHorizontal)
        {
            int row = startPixel.row;
            int startCol = Mathf.Min(startPixel.column, endPixel.column);
            int endCol = Mathf.Max(startPixel.column, endPixel.column);

            // Create PaintingPixel objects for all body parts between start and end
            for (int col = startCol + 1; col < endCol; col++)
            {
                // Get the original grid pixel to use its world position for reference
                PaintingPixelConfig gridPixel = new PaintingPixelConfig(gridObject.GetPixelAt(col, row));
                if (gridPixel != null)
                {
                    currentPixelRow = row;
                    currentPixelColumn = col;
                    if (startPixel.row < 0) currentPixelRow -= PipeSpaceFromGrid;
                    else currentPixelRow += PipeSpaceFromGrid;
                    PaintingPixelConfig bodyPipePixel = new PaintingPixelConfig()
                    {
                        column = currentPixelColumn,
                        row = currentPixelRow,
                        color = colorPalette.GetColorByCode(ColorCode),
                        colorCode = ColorCode,
                        Hidden = false,
                    };
                    pixelsInBetween.Add(bodyPipePixel);
                }
            }
        }
        else if (isVertical)
        {
            int column = startPixel.column;
            int startRow = Mathf.Min(startPixel.row, endPixel.row);
            int endRow = Mathf.Max(startPixel.row, endPixel.row);

            // Create PaintingPixel objects for all body parts between start and end
            for (int row = startRow + 1; row < endRow; row++)
            {
                // Get the original grid pixel to use its world position for reference
                PaintingPixelConfig gridPixel = new PaintingPixelConfig(gridObject.GetPixelAt(column, row));
                if (gridPixel != null)
                {
                    currentPixelRow = row;
                    currentPixelColumn = column;
                    if (startPixel.column < 0) currentPixelColumn -= PipeSpaceFromGrid;
                    else currentPixelColumn += PipeSpaceFromGrid;
                    PaintingPixelConfig bodyPipePixel = new PaintingPixelConfig()
                    {
                        column = currentPixelColumn,
                        row = currentPixelRow,
                        color = colorPalette.GetColorByCode(ColorCode),
                        colorCode = ColorCode,
                        Hidden = false,
                    };
                    pixelsInBetween.Add(bodyPipePixel);
                }
            }
        }

        // Create new PaintingPixel and PaintingPixelComponent for the tail of the pipe (outside the grid)
        // First get the actual grid pixel for tail to make sure we have the correct world position
        PaintingPixelConfig actualGridTailPixel = new PaintingPixelConfig(gridObject.GetPixelAt(endPixel.column, endPixel.row));
        currentPixelRow = endPixel.row;
        currentPixelColumn = endPixel.column;
        if (isHorizontal)
        {
            if (startPixel.row < 0) currentPixelRow -= PipeSpaceFromGrid;
            else currentPixelRow += PipeSpaceFromGrid;
        }
        else
        {
            if (startPixel.column < 0) currentPixelColumn -= PipeSpaceFromGrid;
            else currentPixelColumn += PipeSpaceFromGrid;
        }
        PaintingPixelConfig tailPipePixel = new PaintingPixelConfig()
        {
            column = currentPixelColumn,
            row = currentPixelRow,
            color = colorPalette.GetColorByCode(ColorCode),
            colorCode = ColorCode,
            Hidden = false,
        };
        pixelsInBetween.Add(tailPipePixel);

        // Sort pipe pixels from head to tail based on original grid position
        if (pixelsInBetween.Count > 1)
        {
            if (isHorizontal)
            {
                if (startPixel.column < endPixel.column)
                    pixelsInBetween.Sort((p1, p2) => p1.column.CompareTo(p2.column));
                else
                    pixelsInBetween.Sort((p1, p2) => p2.column.CompareTo(p1.column));
            }
            else if (isVertical)
            {
                if (startPixel.row < endPixel.row)
                    pixelsInBetween.Sort((p1, p2) => p1.row.CompareTo(p2.row));
                else
                    pixelsInBetween.Sort((p1, p2) => p2.row.CompareTo(p1.row));
            }
        }

        return pixelsInBetween;
    }

    public List<PaintingPixelConfig> GetPaintingPixelsInBetween(PaintingPixel startPixel, PaintingPixel endPixel)
    {
        List<PaintingPixelConfig> pixelsInBetween = new List<PaintingPixelConfig>();

        // Determine if pipe is horizontal or vertical
        bool isHorizontal = startPixel.row == endPixel.row;
        bool isVertical = startPixel.column == endPixel.column;

        if (isHorizontal)
        {
            // Create PaintingPixel objects for all body parts between start and end
            int direction = startPixel.column <= endPixel.column ? 1 : -1;
            for (int c = startPixel.column; c != endPixel.column + direction; c += direction)
            {
                PaintingPixelConfig bodyPipePixel = new PaintingPixelConfig()
                {
                    column = c,
                    row = startPixel.row,
                    color = colorPalette.GetColorByCode(ColorCode),
                    colorCode = ColorCode,
                    Hidden = false,
                };
                pixelsInBetween.Add(bodyPipePixel);
            }
        }
        else if (isVertical)
        {
            int direction = startPixel.row <= endPixel.row ? 1 : -1;
            for (int r = startPixel.row; r != endPixel.row + direction; r += direction)
            {
                PaintingPixelConfig bodyPipePixel = new PaintingPixelConfig()
                {
                    column = startPixel.column,
                    row = r,
                    color = colorPalette.GetColorByCode(ColorCode),
                    colorCode = ColorCode,
                    Hidden = false,
                };
                pixelsInBetween.Add(bodyPipePixel);
            }
        }

        // Sort pipe pixels from head to tail based on original grid position
        if (pixelsInBetween.Count > 1)
        {
            if (isHorizontal)
            {
                if (startPixel.column < endPixel.column)
                    pixelsInBetween.Sort((p1, p2) => p1.column.CompareTo(p2.column));
                else
                    pixelsInBetween.Sort((p1, p2) => p2.column.CompareTo(p1.column));
            }
            else if (isVertical)
            {
                if (startPixel.row < endPixel.row)
                    pixelsInBetween.Sort((p1, p2) => p1.row.CompareTo(p2.row));
                else
                    pixelsInBetween.Sort((p1, p2) => p2.row.CompareTo(p1.row));
            }
        }

        return pixelsInBetween;
    }

    /// <summary>
    /// Set up the actual pipe object in the scene
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <param name="colorCode">Color code for the pipe</param>
    /// <returns>Tuple with the created PipeObject component and list of new pipe pixels</returns>
    private PipeObject SetupNewPipeInScene(PipeObjectSetup setup)
    {
        PipeObject pipeObject = gridObject.CreatePipeObject(setup);
        return pipeObject;
    }
    
    /// <summary>
    /// Validates if the pipe orientation is valid (horizontal or vertical only)
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    public bool IsValidPipeOrientation(PaintingPixel startPixel, PaintingPixel endPixel)
    {
        return (startPixel.row == endPixel.row) || (startPixel.column == endPixel.column);
    }
    
    /// <summary>
    /// Helper method to validate pipe orientation using PaintingPixelComponents
    /// </summary>
    /// <param name="startPixelComponent">Start pixel component</param>
    /// <param name="endPixelComponent">End pixel component</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    private bool IsValidPipeOrientation(PaintingPixelComponent startPixelComponent, PaintingPixelComponent endPixelComponent)
    {
        if (startPixelComponent == null || endPixelComponent == null)
            return false;
            
        PaintingPixel startPixel = startPixelComponent.PixelData;
        PaintingPixel endPixel = endPixelComponent.PixelData;
        
        if (startPixel == null || endPixel == null)
            return false;
            
        return (startPixel.row == endPixel.row) || (startPixel.column == endPixel.column);
    }
    
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
        if (paintingConfig.PipeSetups == null)
            paintingConfig.PipeSetups = new List<PipeObjectSetup>();
        else
            paintingConfig.PipeSetups.Clear();
        
        // Copy all pipe setups from this component to the config
        foreach (PipeObjectSetup pipeSetup in pipeObjectSetups)
        {
            if (pipeSetup != null)
            {
                // Add the pipe setup to the painting config
                paintingConfig.PipeSetups.Add(pipeSetup);
            }
        }

        paintingConfig.HidePixelsUnderPipes();

        Debug.Log($"Imported {pipeObjectSetups.Count} pipe setups to PaintingConfig.");
    }

    public void Reload()
    {
        gridObject.ClearAllPipes();
        pipeObjectSetups = new List<PipeObjectSetup>(CurrentLevelObjectSetups);
        //return;
        foreach (var pipe in CurrentLevelObjectSetups)
        {
            gridObject.CreatePipeObject(pipe);
        }
    }

    public void Save()
    {
        Undo.RecordObject(gridObject.paintingConfig, "Save pipe configs");
        ImportPipesToPaintingConfig(gridObject.paintingConfig);
        EditorUtility.SetDirty(gridObject.paintingConfig);
    }
}