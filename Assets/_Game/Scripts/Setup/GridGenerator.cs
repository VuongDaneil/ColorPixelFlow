using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2 gridSize = new Vector2(20, 40); // 20 columns, 40 rows
    public Transform centerPoint; // Grid's center pivot for spawning objects around
    public GameObject pixelPrefab; // Prefab to spawn as a pixel
    [Range(0.1f, 5.0f)]
    public float pixelScaleFactor = 1.0f; // Pixel gameobject scale factor
    public float pixelArrangeSpace = 1.0f; // Space distance between each pixels
    public float YOffset = 0.0f; // Y Offset for pixel placement

    [Header("Generated Grid")]
    public PaintingGridObject paintingGridObject; // The grid object that will be the parent of all pixels
    public Transform pixelsParent; // Transform to use as parent for pixel objects

    private void Reset()
    {
        // Default values when component is added
        gridSize = new Vector2(20, 40); // 20 columns, 40 rows
        pixelScaleFactor = 1.0f;
        pixelArrangeSpace = 1.0f;
        
        // Set default center point
        centerPoint = this.transform;
    }

    public void ContextGenerateGrid()
    {
        GenerateGrid();
    }
    
    public void ContextClearGrid()
    {
        ClearGrid();
    }

    public void GenerateGrid()
    {
        // Clear previous grid if exists
        if (paintingGridObject != null)
        {
            ClearGrid();
        }

        // Create or get the grid object to use as parent for pixels
        GameObject gridObj = null;
        
        if (pixelsParent != null)
        {
            // Use the provided parent transform
            gridObj = pixelsParent.gameObject;
        }
        else
        {
            // Create a new GameObject to hold the pixels
            gridObj = new GameObject("PaintingGridObject");
            gridObj.transform.SetParent(this.transform);
            paintingGridObject = gridObj.AddComponent<PaintingGridObject>();
        }

        // If the parent doesn't have the PaintingGridObject component, add it
        if (paintingGridObject == null)
        {
            paintingGridObject = gridObj.GetComponent<PaintingGridObject>();
            if (paintingGridObject == null)
            {
                paintingGridObject = gridObj.AddComponent<PaintingGridObject>();
            }
        }

        // Initialize the grid
        paintingGridObject.InitializeGrid(gridSize, pixelArrangeSpace, pixelPrefab, pixelScaleFactor);

        // Generate the grid using the PaintingGridObject component
        paintingGridObject.GenerateGrid(centerPoint, YOffset);
    }

    public void ClearGrid()
    {
        if (paintingGridObject != null)
        {
            paintingGridObject.DestroyAllPixelsObjects();
        }
    }

    // Get the PaintingPixel at a specific position
    public PaintingPixel GetPixelAt(int col, int row)
    {
        if (paintingGridObject != null)
        {
            return paintingGridObject.GetPixelAt(col, row);
        }
        return null;
    }

    // Method to get the position of a specific grid coordinate in world space
    public Vector3 GetWorldPosition(int col, int row)
    {
        if (centerPoint == null)
        {
            centerPoint = this.transform;
        }

        // Calculate the world position based on grid coordinates
        float xPos = centerPoint.position.x + col * pixelArrangeSpace;
        float zPos = centerPoint.position.z + row * pixelArrangeSpace;

        return new Vector3(xPos, YOffset, zPos);
    }
    
    // Get the total number of pixels in the grid
    public int GetTotalPixels()
    {
        if (paintingGridObject != null)
        {
            return paintingGridObject.GetTotalPixels();
        }
        return 0;
    }
}