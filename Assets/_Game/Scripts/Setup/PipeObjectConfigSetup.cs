using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PipeObjectConfigSetup : MonoBehaviour
{
    [Header("Pipe Configuration")]
    public List<PipeObjectSetup> CurrentLevelObjectSetups; // To store all the pipe that being setup
    public List<PipeObjectSetup> pipeObjectSetups; // To store all the pipe that being setup
    
    [Header("References")]
    public PaintingGridObject gridObject; // GridObject (PaintingGridObject.cs)
    
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
        var (newPipeObject, newPipePixels) = SetupNewPipeInSceneWithPixels(startPixel, endPixel, ColorCode);
        
        if (newPipeObject != null && newPipePixels != null)
        {
            // Create the PipeObjectSetup configuration using the newly created pipe pixels
            PipeObjectSetup pipeSetup = new PipeObjectSetup(newPipePixels, ColorCode, Scale);
            
            // Add to the list of pipe setups
            AddPipeSetup(pipeSetup);
            
            // Add the created pipe to the grid object's pipe list
            if (gridObject.pipeObjects == null)
                gridObject.pipeObjects = new List<PipeObject>();
                
            gridObject.pipeObjects.Add(newPipeObject);
        }
    }



    /// <summary>
    /// Set up the actual pipe object in the scene
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <param name="colorCode">Color code for the pipe</param>
    /// <returns>Tuple with the created PipeObject component and list of new pipe pixels</returns>
    private (PipeObject pipeObject, List<PaintingPixel> pipePixels) SetupNewPipeInSceneWithPixels(PaintingPixel startPixel, PaintingPixel endPixel, string colorCode)
    {
        if (PipeHeadPrefab == null || PipeBodyPrefab == null || PipeTailPrefab == null)
        {
            Debug.LogError("Pipe prefabs are not assigned. Cannot create pipe.");
            return (null, null);
        }
        
        // Create the pipe game object with head transform as parent
        GameObject pipeGO = new GameObject($"Pipe ({startPixel.column},{startPixel.row}) to ({endPixel.column},{endPixel.row})");
        pipeGO.transform.SetParent(gridObject.transform);

        // Determine if pipe is horizontal or vertical
        bool isHorizontal = startPixel.row == endPixel.row;
        bool isVertical = startPixel.column == endPixel.column;
        
        // Create new PaintingPixel and PaintingPixelComponent for the head of the pipe (outside the grid)
        // First get the actual grid pixel for head to make sure we have the correct world position
        PaintingPixel actualGridHeadPixel = gridObject.GetPixelAt(startPixel.column, startPixel.row);
        Vector3 headWorldPos = CalculatePipePosition(actualGridHeadPixel ?? startPixel, isHorizontal);
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
        PaintingPixel headPipePixel = CreatePipePixel(currentPixelColumn, currentPixelRow, headWorldPos, colorCode, hidden: false); // false means tail
        
        // Create the head at the calculated position
        GameObject headGO = Instantiate(PipeHeadPrefab, headWorldPos, Quaternion.identity, pipeGO.transform);
        headGO.name = "PipeHead";
        
        // Apply direct scale to the head
        headGO.transform.localScale = Scale;
        headGO.transform.localPosition = headWorldPos;
        
        Transform headTransform = headGO.transform;
        
        // Get the PipeObject component from the head (the head prefab already has PipeObject.cs)
        PipeObject pipeObject = headTransform.GetComponent<PipeObject>();
        if (pipeObject == null)
        {
            Debug.LogWarning("PipeHeadPrefab does not have PipeObject component. Please ensure the head prefab has PipeObject.cs attached.");
            return (null, null);
        }
        
        // Change the color of the head part using PipePartVisualHandle
        PipePartVisualHandle headVisualHandle = headGO.GetComponent<PipePartVisualHandle>();
        if (headVisualHandle == null)
            headVisualHandle = headGO.GetComponentInChildren<PipePartVisualHandle>();
        if (headVisualHandle != null)
            headVisualHandle.SetColor(headPipePixel.color);
        
        // Initialize the list for body parts
        List<Transform> bodyParts = new List<Transform>();
        
        // Create new PaintingPixel and PaintingPixelComponent for body parts and add them to grid object's pipe pixels list
        List<PaintingPixel> pipePixels = new List<PaintingPixel>();
        pipePixels.Add(headPipePixel); // Add the head pipe pixel first
        bodyParts.Add(headGO.transform);


        if (isHorizontal)
        {
            int row = startPixel.row;
            int startCol = Mathf.Min(startPixel.column, endPixel.column);
            int endCol = Mathf.Max(startPixel.column, endPixel.column);
            
            // Create PaintingPixel objects for all body parts between start and end
            for (int col = startCol + 1; col < endCol; col++)
            {
                // Get the original grid pixel to use its world position for reference
                PaintingPixel gridPixel = gridObject.GetPixelAt(col, row);
                if (gridPixel != null)
                {
                    Vector3 bodyPos = CalculatePipePosition(gridPixel, isHorizontal);
                    currentPixelRow = row;
                    currentPixelColumn = col;
                    if (startPixel.row < 0) currentPixelRow -= PipeSpaceFromGrid;
                    else currentPixelRow += PipeSpaceFromGrid;
                    PaintingPixel bodyPipePixel = CreatePipePixel(currentPixelColumn, currentPixelRow, bodyPos, colorCode, hidden: false);
                    pipePixels.Add(bodyPipePixel);
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
                PaintingPixel gridPixel = gridObject.GetPixelAt(column, row);
                if (gridPixel != null)
                {
                    Vector3 bodyPos = CalculatePipePosition(gridPixel, isHorizontal);
                    currentPixelRow = row;
                    currentPixelColumn = column;
                    if (startPixel.column < 0) currentPixelColumn -= PipeSpaceFromGrid;
                    else currentPixelColumn += PipeSpaceFromGrid;
                    PaintingPixel bodyPipePixel = CreatePipePixel(currentPixelColumn, currentPixelRow, bodyPos, colorCode, hidden: false);
                    pipePixels.Add(bodyPipePixel);
                }
            }
        }
        
        // Create body parts at their calculated positions
        foreach (var pipePixel in pipePixels)
        {
            // Skip the head since it's already created
            if (pipePixel != headPipePixel)
            {
                GameObject bodyGO = Instantiate(PipeBodyPrefab, pipePixel.worldPos, Quaternion.identity, pipeGO.transform);
                bodyGO.name = "PipeBody";
                // Apply direct scale to the body part
                bodyGO.transform.localScale = Scale;
                bodyGO.transform.localPosition = pipePixel.worldPos;
                bodyParts.Add(bodyGO.transform);
                
                // Change the color of the pipe part using PipePartVisualHandle
                PipePartVisualHandle visualHandle = bodyGO.GetComponent<PipePartVisualHandle>();
                if (visualHandle == null)
                    visualHandle = bodyGO.GetComponentInChildren<PipePartVisualHandle>();
                if (visualHandle != null)
                    visualHandle.SetColor(pipePixel.color);
            }
        }
        
        // Create new PaintingPixel and PaintingPixelComponent for the tail of the pipe (outside the grid)
        // First get the actual grid pixel for tail to make sure we have the correct world position
        PaintingPixel actualGridTailPixel = gridObject.GetPixelAt(endPixel.column, endPixel.row);
        Vector3 tailWorldPos = CalculatePipePosition(actualGridTailPixel ?? endPixel, isHorizontal); // true means tail
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
        PaintingPixel tailPipePixel = CreatePipePixel(currentPixelColumn, currentPixelRow, tailWorldPos, colorCode, hidden: false); // true means tail
        pipePixels.Add(tailPipePixel); // Add the tail pipe pixel last
        
        // Create tail at the calculated position
        GameObject tailGO = Instantiate(PipeTailPrefab, tailWorldPos, Quaternion.identity, pipeGO.transform);
        tailGO.name = "PipeTail";
        // Apply direct scale to the tail
        tailGO.transform.localScale = Scale;
        tailGO.transform.localPosition = tailWorldPos;
        
        // Change the color of the tail part using PipePartVisualHandle
        PipePartVisualHandle tailVisualHandle = tailGO.GetComponent<PipePartVisualHandle>();
        if (tailVisualHandle == null)
            tailVisualHandle = tailGO.GetComponentInChildren<PipePartVisualHandle>();
        if (tailVisualHandle != null)
            tailVisualHandle.SetColor(tailPipePixel.color);
        
        bodyParts.Add(tailGO.transform); // Add tail as the last body part

        // Sort pipe pixels from head to tail based on original grid position
        if (pipePixels.Count > 1)
        {
            if (isHorizontal)
            {
                if (startPixel.column < endPixel.column)
                    pipePixels.Sort((p1, p2) => p1.column.CompareTo(p2.column));
                else
                    pipePixels.Sort((p1, p2) => p2.column.CompareTo(p1.column));
            }
            else if (isVertical)
            {
                if (startPixel.row < endPixel.row)
                    pipePixels.Sort((p1, p2) => p1.row.CompareTo(p2.row));
                else
                    pipePixels.Sort((p1, p2) => p2.row.CompareTo(p1.row));
            }
        }

        // Initialize the pipe object with head and body parts, and orientation
        pipeObject.Initialize(headTransform, bodyParts, pipePixels, isHorizontal);

        // Apply orientation rotation if needed (this is handled by the PipeObject itself)
        pipeObject.ApplyOrientationRotation();

        pipeGO.transform.localPosition = Vector3.zero;
        for (int i = 0; i < pipePixels.Count; i++)
        {
            bodyParts[i].localPosition = pipePixels[i].worldPos;
        }

        return (pipeObject, pipePixels);
    }

    private (PipeObject pipeObject, List<PaintingPixel> pipePixels) SpawnAPipeInScene(PipeObjectSetup setup)
    {
        if (PipeHeadPrefab == null || PipeBodyPrefab == null || PipeTailPrefab == null)
        {
            Debug.LogError("Pipe prefabs are not assigned. Cannot create pipe.");
            return (null, null);
        }

        var startPixel = setup.PixelCovered[0];
        var endPixel = setup.PixelCovered[^1];

        // Create the pipe game object with head transform as parent
        GameObject pipeGO = new GameObject($"Pipe ({startPixel.column},{startPixel.row}) to ({endPixel.column},{endPixel.row})");
        pipeGO.transform.SetParent(gridObject.transform);

        // Determine if pipe is horizontal or vertical
        bool isHorizontal = startPixel.row == endPixel.row;
        bool isVertical = startPixel.column == endPixel.column;

        // Create new PaintingPixel and PaintingPixelComponent for the head of the pipe (outside the grid)

        PaintingPixel headPipePixel = CreatePipePixel(startPixel.column, startPixel.row, startPixel.worldPos, setup.ColorCode, hidden: false); // false means tail

        // Create the head at the calculated position
        GameObject headGO = Instantiate(PipeHeadPrefab, startPixel.worldPos, Quaternion.identity, pipeGO.transform);
        headGO.name = "PipeHead";

        // Apply direct scale to the head
        headGO.transform.localScale = setup.Scale;

        Transform headTransform = headGO.transform;

        // Get the PipeObject component from the head (the head prefab already has PipeObject.cs)
        PipeObject pipeObject = headTransform.GetComponent<PipeObject>();
        if (pipeObject == null)
        {
            Debug.LogWarning("PipeHeadPrefab does not have PipeObject component. Please ensure the head prefab has PipeObject.cs attached.");
            return (null, null);
        }

        // Change the color of the head part using PipePartVisualHandle
        PipePartVisualHandle headVisualHandle = headGO.GetComponent<PipePartVisualHandle>();
        if (headVisualHandle == null)
            headVisualHandle = headGO.GetComponentInChildren<PipePartVisualHandle>();
        if (headVisualHandle != null)
            headVisualHandle.SetColor(headPipePixel.color);

        // Initialize the list for body parts
        List<Transform> bodyParts = new List<Transform>();

        // Create new PaintingPixel and PaintingPixelComponent for body parts and add them to grid object's pipe pixels list
        List<PaintingPixel> pipePixels = new List<PaintingPixel>();
        pipePixels.Add(headPipePixel); // Add the head pipe pixel first
        bodyParts.Add(headGO.transform);
        if (isHorizontal)
        {
            // Create PaintingPixel objects for all body parts between start and end
            for (int i = 1; i < setup.PixelCovered.Count - 1; i++) //except for head and tail
            {
                Vector3 bodyPos = setup.PixelCovered[i].worldPos;

                PaintingPixel bodyPipePixel = CreatePipePixel(setup.PixelCovered[i].column, setup.PixelCovered[i].row, bodyPos, setup.PixelCovered[i].colorCode, hidden: false);
                pipePixels.Add(bodyPipePixel);
            }
        }
        else if (isVertical)
        {
            for (int i = 1; i < setup.PixelCovered.Count - 1; i++) //except for head and tail
            {
                Vector3 bodyPos = setup.PixelCovered[i].worldPos;

                PaintingPixel bodyPipePixel = CreatePipePixel(setup.PixelCovered[i].column, setup.PixelCovered[i].row, bodyPos, setup.PixelCovered[i].colorCode, hidden: false);
                pipePixels.Add(bodyPipePixel);
            }
        }

        // Create body parts at their calculated positions
        foreach (var pipePixel in pipePixels)
        {
            // Skip the head since it's already created
            if (pipePixel != headPipePixel)
            {
                GameObject bodyGO = Instantiate(PipeBodyPrefab, pipePixel.worldPos, Quaternion.identity, pipeGO.transform);
                bodyGO.name = "PipeBody";
                // Apply direct scale to the body part
                bodyGO.transform.localScale = setup.Scale;
                bodyParts.Add(bodyGO.transform);

                // Change the color of the pipe part using PipePartVisualHandle
                PipePartVisualHandle visualHandle = bodyGO.GetComponent<PipePartVisualHandle>();
                if (visualHandle == null)
                    visualHandle = bodyGO.GetComponentInChildren<PipePartVisualHandle>();
                if (visualHandle != null)
                    visualHandle.SetColor(pipePixel.color);
            }
        }

        // Create new PaintingPixel and PaintingPixelComponent for the tail of the pipe (outside the grid)
        PaintingPixel tailPipePixel = CreatePipePixel(endPixel.column, endPixel.row, endPixel.worldPos, endPixel.colorCode, hidden: false); // true means tail
        pipePixels.Add(tailPipePixel); // Add the tail pipe pixel last

        // Create tail at the calculated position
        GameObject tailGO = Instantiate(PipeTailPrefab, endPixel.worldPos, Quaternion.identity, pipeGO.transform);
        tailGO.name = "PipeTail";
        // Apply direct scale to the tail
        tailGO.transform.localScale = setup.Scale;

        // Change the color of the tail part using PipePartVisualHandle
        PipePartVisualHandle tailVisualHandle = tailGO.GetComponent<PipePartVisualHandle>();
        if (tailVisualHandle == null)
            tailVisualHandle = tailGO.GetComponentInChildren<PipePartVisualHandle>();
        if (tailVisualHandle != null)
            tailVisualHandle.SetColor(tailPipePixel.color);

        bodyParts.Add(tailGO.transform); // Add tail as the last body part

        // Sort pipe pixels from head to tail based on original grid position
        if (pipePixels.Count > 1)
        {
            if (isHorizontal)
            {
                if (startPixel.column < endPixel.column)
                    pipePixels.Sort((p1, p2) => p1.column.CompareTo(p2.column));
                else
                    pipePixels.Sort((p1, p2) => p2.column.CompareTo(p1.column));
            }
            else if (isVertical)
            {
                if (startPixel.row < endPixel.row)
                    pipePixels.Sort((p1, p2) => p1.row.CompareTo(p2.row));
                else
                    pipePixels.Sort((p1, p2) => p2.row.CompareTo(p1.row));
            }
        }

        // Initialize the pipe object with head and body parts, and orientation
        pipeObject.Initialize(headTransform, bodyParts, pipePixels, isHorizontal);

        // Apply orientation rotation if needed (this is handled by the PipeObject itself)
        pipeObject.ApplyOrientationRotation();

        pipeGO.transform.localPosition = Vector3.zero;
        for (int i = 0; i < setup.PixelCovered.Count; i++)
        {
            bodyParts[i].localPosition = setup.PixelCovered[i].worldPos;
        }

        return (pipeObject, pipePixels);
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
        if (gridObject.pipeObjectsPixels == null)
            gridObject.pipeObjectsPixels = new List<PaintingPixel>();
        gridObject.pipeObjectsPixels.Add(pipePixel);
        
        return pipePixel;
    }
    
    /// <summary>
    /// Calculate the world position for a pipe part based on its grid position and the offset
    /// </summary>
    /// <param name="gridPixel">The grid pixel that the pipe part corresponds to</param>
    /// <param name="isHorizontal">Whether the pipe is horizontal</param>
    /// <param name="isTail">Whether this is the tail part of the pipe</param>
    /// <returns>The calculated world position for the pipe part</returns>
    private Vector3 CalculatePipePosition(PaintingPixel gridPixel, bool isHorizontal)
    {
        // Calculate the offset from the grid based on the pipe direction and PipeSpaceFromGrid
        // For horizontal pipes (in same row), the pipe is placed in a row parallel to the grid row
        // For vertical pipes (in same column), the pipe is placed in a column parallel to the grid column
        
        Vector3 gridPosition = gridPixel.worldPos;
        
        // Calculate the offset based on grid direction and PipeSpaceFromGrid
        if (isHorizontal)
        {
            // For horizontal pipes (same row), offset perpendicular to the row direction (in the grid's X direction)
            // Using positive Z-axis as offset for horizontal pipes (above the grid)
            float dirr = gridPixel.row > 0 ? 1 : -1;
            float offset = (dirr * PipeSpaceFromGrid) * gridObject.pixelArrangeSpace;
            Vector3 offsetDirection = Vector3.forward; // Positive Z direction (up from grid perspective)
            return gridPosition + offsetDirection * offset;
        }
        else // Vertical pipe
        {
            // For vertical pipes (same column), offset perpendicular to the column direction (in the grid's Z direction)
            // Using positive X-axis as offset for vertical pipes (to the right of the grid)
            float dirr = gridPixel.column > 0 ? 1 : -1;
            float offset = (dirr * PipeSpaceFromGrid) * gridObject.pixelArrangeSpace;
            Vector3 offsetDirection = Vector3.right; // Positive X direction (right from grid perspective)
            return gridPosition + offsetDirection * offset;
        }
    }
    
    /// <summary>
    /// Validates if the pipe orientation is valid (horizontal or vertical only)
    /// </summary>
    /// <param name="startPixel">Start pixel (head)</param>
    /// <param name="endPixel">End pixel (tail)</param>
    /// <returns>True if pipe orientation is valid, false otherwise</returns>
    private bool IsValidPipeOrientation(PaintingPixel startPixel, PaintingPixel endPixel)
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
        pipeObjectSetups.Clear();
        
        // Copy all pipe setups from the config to this component
        if (paintingConfig.PipeSetups != null)
        {
            foreach (PipeObjectSetup pipeSetup in paintingConfig.PipeSetups)
            {
                if (pipeSetup != null)
                {
                    pipeObjectSetups.Add(pipeSetup);
                }
            }
        }
        int tmp = paintingConfig.PipeSetups != null ? paintingConfig.PipeSetups.Count : 0;
        Debug.Log($"Applied {tmp} pipe setups from PaintingConfig.");
    }

    public void Reload()
    {
        pipeObjectSetups = new List<PipeObjectSetup>(CurrentLevelObjectSetups);
        //return;
        foreach (var pipe in CurrentLevelObjectSetups)
        {
            var (newPipeObject, newPipePixels) = SpawnAPipeInScene(pipe);

            if (newPipeObject != null && newPipePixels != null)
            {
                // Create the PipeObjectSetup configuration using the newly created pipe pixels
                PipeObjectSetup pipeSetup = new PipeObjectSetup(newPipePixels, ColorCode, Scale);

                // Add the created pipe to the grid object's pipe list
                if (gridObject.pipeObjects == null)
                    gridObject.pipeObjects = new List<PipeObject>();

                gridObject.pipeObjects.Add(newPipeObject);
            }
        }
    }
}