using System.Collections.Generic;
using System.Linq;
using GogoGaga.OptimizedRopesAndCables;
using UnityEngine;
// Custom ReadOnly attribute if not available in the project

public class ColorPixelsCollector : MonoBehaviour
{
    public CollectorVisualHandler VisualHandler;

    [Header("Identity")]
    public int OriginalIndex; // Original index of the collector in its squad
    
    [Header("Movement")]
    [Tooltip("Movement handler script for spline movement")]
    public CachedTransformPathMover MovementHandle;

    [Header("Target Grid")]
    [Tooltip("Current target grid object")]
    public PaintingGridObject CurrentGrid;

    [Header("Shooting Mechanics")]
    [Tooltip("Max number of bullets")]
    public int BulletCapacity = 10;

    [Tooltip("Bullets left")]
    public int BulletLeft;

    [Header("Color Matching")]
    [Tooltip("Pixel's color code that this object can shoot. Color of this object based on pixel's color")]
    public string CollectorColor = "WhiteDefault";

    [Header("Detection Settings")]
    [Tooltip("Distance threshold to detect and shoot nearby pixels")]
    public float DetectionRadius = 0.5f;

    [Header("CONFIG ATTRIBUTES")]
    public bool IsLocked;
    public bool IsHidden;
    public List<int> ConnectedCollectorsIndex; // original index of other collectors that connected to this collector

    [Header("Runtime Data")]
    public Vector3 CurrentTargetPosition;
    public bool IsCollectorActive = true;

    // List of possible targets around the grid outline
    public PaintingPixel[] possibleTargets;
    private int possibleTargetsCount;

    // Property to check if collector is still available (has bullets left)
    public bool Available 
    { 
        get 
        { 
            return BulletLeft > 0; 
        } 
    }

    // Variables to track which columns/rows have been processed in the current movement direction
    private HashSet<int> processedHorizontalPositions = new HashSet<int>(); // For tracking rows when moving horizontally
    private HashSet<int> processedVerticalPositions = new HashSet<int>();   // For tracking columns when moving vertically

    private void Start()
    {
        InitializeCollector();
    }

    private void InitializeCollector()
    {
        BulletLeft = BulletCapacity;
        
        if (MovementHandle != null)
        {
            //MovementHandle.StartMovement();
            // Initialize the previous movement direction
            if (MovementHandle.transformPath != null && MovementHandle.transformPath.IsValid())
            {
                Vector3 initialDirection = MovementHandle.GetTangentAtTF(MovementHandle.currentTF);
                previousMovementDirection = initialDirection;
                currentMovementDirection = DetermineMovementDirection(initialDirection);
            }
        }
        
        UpdatePossibleTargets();
        
        // Subscribe to grid change event
        GameplayEventsManager.OnGridPixelsChanged += OnGridPixelsChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe from grid change event
        GameplayEventsManager.OnGridPixelsChanged -= OnGridPixelsChanged;
    }

    private void Update()
    {
        if (!IsCollectorActive || CurrentGrid == null)
            return;

        // Check for movement direction change to reset processed positions
        CheckMovementDirectionChange();

        // Check for nearby pixels to destroy
        UpdatePossibleTargets();
        CheckAndDestroyNearbyPixels();
    }

    // Method to update possible targets based on the current grid state
    public void UpdatePossibleTargets()
    {
        if (CurrentGrid != null)
        {
            // Get all outline pixels with the matching color code
            System.Collections.Generic.List<PaintingPixel> outlinePixelsWithColor = 
                CurrentGrid.SelectOutlinePixelsWithColor(CollectorColor);
            
            if (outlinePixelsWithColor != null)
            {
                possibleTargets = outlinePixelsWithColor.ToArray();
                possibleTargetsCount = outlinePixelsWithColor.Count;
            }
            else
            {
                possibleTargets = new PaintingPixel[0];
                possibleTargetsCount = 0;
            }
        }
    }
    
    // Method to check and destroy nearby pixels
    private void CheckAndDestroyNearbyPixels()
    {
        if (BulletLeft <= 0)
            return;

        // Determine the current movement direction using the tangent vector
        bool hasValidMovementDirection = currentMovementDirection != MovementDirection.Unknown;

        PaintingPixel tempPixel = null;
        for (int i = 0; i < possibleTargetsCount; i++)
        {
            tempPixel = possibleTargets[i];
            
            if (tempPixel != null && !tempPixel.destroyed && !tempPixel.Hidden)
            {
                bool isCloseEnough = false;
                bool hasObstacle = false;
                bool hasBeenProcessed = false;
                
                if (hasValidMovementDirection)
                {
                    // Use coordinate-based detection depending on movement direction
                    // If moving more horizontally (X-axis), check only X positions
                    // If moving more vertically (Z-axis), check only Z positions
                    
                    if (IsHorizontalDirection(currentMovementDirection))
                    {
                        // Moving horizontally - only compare X positions
                        float positionDiff = Mathf.Abs(transform.position.x - tempPixel.worldPos.x);
                        isCloseEnough = positionDiff <= DetectionRadius;
                        
                        // Check if this column (X position) has already been processed in current direction
                        hasBeenProcessed = processedHorizontalPositions.Contains(tempPixel.column);
                        
                        // Check for obstacles between collector and target pixel
                        hasObstacle = CheckForObstaclesInColumn(tempPixel);
                    }
                    else if (IsVerticalDirection(currentMovementDirection))
                    {
                        // Moving vertically - only compare Z positions
                        float positionDiff = Mathf.Abs(transform.position.z - tempPixel.worldPos.z);
                        isCloseEnough = positionDiff <= DetectionRadius;
                        
                        // Check if this row (Z position) has already been processed in current direction
                        hasBeenProcessed = processedVerticalPositions.Contains(tempPixel.row);

                        // Check for obstacles between collector and target pixel
                        hasObstacle = CheckForObstaclesInRow(tempPixel);
                    }
                }
                else
                {
                    // Fallback to distance check if no valid movement direction
                    float distance = Vector3.Distance(transform.position, tempPixel.worldPos);
                    isCloseEnough = distance <= DetectionRadius;
                }
                
                if (isCloseEnough && !hasObstacle && !hasBeenProcessed)
                {
                    // Destroy the pixel
                    ShooPixel(tempPixel);
                    
                    // Mark position as processed in current direction
                    if (hasValidMovementDirection)
                    {
                        if (IsHorizontalDirection(currentMovementDirection))
                        {
                            // Moving horizontally - mark column as processed
                            processedHorizontalPositions.Add(tempPixel.column);
                        }
                        else if (IsVerticalDirection(currentMovementDirection))
                        {
                            // Moving vertically - mark row as processed
                            processedVerticalPositions.Add(tempPixel.row);
                        }
                    }
                }
            }
        }
    }

    // Method to destroy a specific pixel
    private void ShooPixel(PaintingPixel pixel)
    {
        if (pixel != null && !pixel.destroyed && !pixel.Hidden && BulletLeft > 0)
        {
            // Mark bullet as used
            BulletLeft--;
            
            // Destroy the pixel using the grid's method
            CurrentGrid.ShootPixel(pixel);
            
            // Update possible targets since the grid has changed
            UpdatePossibleTargets();
        }
    }

    // Callback for when grid pixels change
    private void OnGridPixelsChanged(PaintingGridObject changedGrid)
    {
        // Only update if this collector is targeting the changed grid
        if (changedGrid == CurrentGrid)
        {
            UpdatePossibleTargets();
        }
    }
    
    // Method to check for obstacles in the same row when moving horizontally
    private bool CheckForObstaclesInRow(PaintingPixel targetPixel)
    {
        if (CurrentGrid == null || CurrentGrid.paintingPixels == null)
            return false;

        // Check for pixels in the same row (z coordinate similar to target)
        // that are between the collector and target in the x-axis

        var pixelOnSameRow = CurrentGrid.GetPixelsInRow(targetPixel.row);

        switch (currentMovementDirection)
        {
            case MovementDirection.VerticalBottomToTop:
                foreach (PaintingPixel pixel in pixelOnSameRow)
                {
                    if (!pixel.destroyed && !pixel.Hidden && pixel.column > targetPixel.column)
                    {
                        return true;
                    }
                }
                return false;
            case MovementDirection.VerticalTopToBottom:
                foreach (PaintingPixel pixel in pixelOnSameRow)
                {
                    if (!pixel.destroyed && !pixel.Hidden && pixel.column < targetPixel.column)
                    {
                        return true;
                    }
                }
                return false;
        }
        
        return false; // No obstacles found
    }
    
    // Method to check for obstacles in the same column when moving vertically
    private bool CheckForObstaclesInColumn(PaintingPixel targetPixel)
    {
        if (CurrentGrid == null || CurrentGrid.paintingPixels == null)
            return false;

        // Check for pixels in the same column (x coordinate similar to target)
        // that are between the collector and target in the z-axis

        var pixelOnSameColumn = CurrentGrid.GetPixelsInColumn(targetPixel.column);

        switch (currentMovementDirection)
        {
            case MovementDirection.HorizontalLeftToRight:
                foreach (PaintingPixel pixel in pixelOnSameColumn)
                {
                    if (!pixel.destroyed && !pixel.Hidden && pixel.row < targetPixel.row)
                    {
                        return true;
                    }
                }
                return false;
            case MovementDirection.HorizontalRightToLeft:
                foreach (PaintingPixel pixel in pixelOnSameColumn)
                {
                    if (!pixel.destroyed && !pixel.Hidden && pixel.row > targetPixel.row)
                    {
                        return true;
                    }
                }
                return false;
        }

        return false; // No obstacles found
    }
    
    // Store the previous movement direction to detect changes
    private Vector3 previousMovementDirection = Vector3.zero;
    
    // Current movement direction
    public MovementDirection currentMovementDirection = MovementDirection.Unknown;
    
    // Method to reset processed positions when movement direction changes
    private void CheckMovementDirectionChange()
    {
        if (MovementHandle.transformPath != null && MovementHandle.transformPath.IsValid())
        {
            Vector3 rawMovementDirection = MovementHandle.GetTangentAtTF(MovementHandle.currentTF);
            
            // Only consider significant direction changes (not minor fluctuations)
            if (rawMovementDirection != Vector3.zero)
            {
                // Determine the new movement direction based on the tangent vector
                MovementDirection newMovementDirection = DetermineMovementDirection(rawMovementDirection);
                
                // Check if the primary axis of movement has changed
                bool wasHorizontal = IsHorizontalDirection(currentMovementDirection);
                bool isHorizontal = IsHorizontalDirection(newMovementDirection);
                
                // If movement direction has changed significantly (from horizontal to vertical or vice versa)
                if (wasHorizontal != isHorizontal)
                {
                    // Reset the appropriate processed positions
                    if (wasHorizontal)
                    {
                        processedHorizontalPositions.Clear(); // Clear row tracking when switching from horizontal to vertical
                    }
                    else
                    {
                        processedVerticalPositions.Clear();   // Clear column tracking when switching from vertical to horizontal
                    }
                }
                
                // Update the current movement direction
                currentMovementDirection = newMovementDirection;
                previousMovementDirection = rawMovementDirection;
            }
        }
    }
    
    // Helper method to determine movement direction from tangent vector
    private MovementDirection DetermineMovementDirection(Vector3 tangent)
    {
        // Check if movement is primarily horizontal or vertical
        if (Mathf.Abs(tangent.x) > Mathf.Abs(tangent.z))
        {
            // Horizontal movement
            if (tangent.x > 0)
            {
                return MovementDirection.HorizontalRightToLeft;  // Moving in positive X direction (right to left)
            }
            else
            {
                return MovementDirection.HorizontalLeftToRight;  // Moving in negative X direction (left to right)
            }
        }
        else
        {
            // Vertical movement
            if (tangent.z > 0)
            {
                return MovementDirection.VerticalTopToBottom;    // Moving in positive Z direction (top to bottom)
            }
            else
            {
                return MovementDirection.VerticalBottomToTop;    // Moving in negative Z direction (bottom to top)
            }
        }
    }
    
    // Helper method to check if a direction is horizontal
    private bool IsHorizontalDirection(MovementDirection direction)
    {
        return direction == MovementDirection.HorizontalLeftToRight || 
               direction == MovementDirection.HorizontalRightToLeft;
    }
    
    // Helper method to check if a direction is vertical
    private bool IsVerticalDirection(MovementDirection direction)
    {
        return direction == MovementDirection.VerticalBottomToTop || 
               direction == MovementDirection.VerticalTopToBottom;
    }

    // Public method to reset the collector
    public void ResetCollector()
    {
        BulletLeft = BulletCapacity;
        IsCollectorActive = true;
        UpdatePossibleTargets();
        
        // Clear processed positions when resetting
        processedHorizontalPositions.Clear();
        processedVerticalPositions.Clear();
        
        if (MovementHandle != null)
        {
            MovementHandle.StartMovement();
            
            // Update movement direction after reset
            if (MovementHandle.transformPath != null && MovementHandle.transformPath.IsValid())
            {
                Vector3 initialDirection = MovementHandle.GetTangentAtTF(MovementHandle.currentTF);
                previousMovementDirection = initialDirection;
                currentMovementDirection = DetermineMovementDirection(initialDirection);
            }
        }
    }

    // Public method to activate/deactivate the collector
    public void SetCollectorActive(bool active)
    {
        IsCollectorActive = active;
        
        if (MovementHandle != null)
        {
            if (active)
            {
                MovementHandle.StartMovement();
                
                // Update movement direction after activation
                if (MovementHandle.transformPath != null && MovementHandle.transformPath.IsValid())
                {
                    Vector3 initialDirection = MovementHandle.GetTangentAtTF(MovementHandle.currentTF);
                    previousMovementDirection = initialDirection;
                    currentMovementDirection = DetermineMovementDirection(initialDirection);
                }
            }
            else
            {
                MovementHandle.StopMovement();
                // Clear processed positions when deactivating
                processedHorizontalPositions.Clear();
                processedVerticalPositions.Clear();
            }
        }
    }

#if UNITY_EDITOR
    // Visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        if (IsCollectorActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);
        }
    }
#endif
}

public enum MovementDirection
{
    HorizontalLeftToRight,  // Bottom path: moving left to right
    HorizontalRightToLeft,  // Top path: moving right to left
    VerticalBottomToTop,    // Right path: moving bottom to top
    VerticalTopToBottom,     // Left path: moving top to bottom
    Unknown
}