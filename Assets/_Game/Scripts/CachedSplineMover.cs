using UnityEngine;

public class CachedSplineMover : MonoBehaviour
{
    [Header("Spline Data")]
    public SplineDataContainer splineDataContainer;
    
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float speed = 1f;
    
    [Tooltip("Current position along the spline as a 0-1 value (0 = start, 1 = end)")]
    [Range(0f, 1f)]
    public float currentTF = 0f;
    
    [Tooltip("Movement direction: 1 for forward, -1 for backward")]
    public int direction = 1;
    
    [Header("Movement Mode")]
    public bool useDistanceBasedMovement = false;
    [Tooltip("Current position along the spline in world units")]
    public float currentDistance = 0f;
    
    [Header("Movement Type")]
    public MovementType movementType = MovementType.Clamp;
    
    [Header("Automatic Movement")]
    public bool autoMove = true;
    
    [Header("Orientation")]
    public bool orientToPath = true;
    public Space orientationSpace = Space.World;
    
    // Private variables
    private bool isInitialized = false;
    
    public enum MovementType
    {
        Clamp,      // Stop at ends
        Loop,       // Wrap around from end to start and vice versa
        PingPong    // Go back and forth
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Update()
    {
        if (autoMove && splineDataContainer != null && splineDataContainer.IsValid())
        {
            MoveAlongSpline();
        }
    }
    
    private void Initialize()
    {
        if (splineDataContainer == null)
        {
            Debug.LogError("SplineDataContainer is not assigned!", this);
            return;
        }
        
        if (!splineDataContainer.IsValid())
        {
            Debug.LogError("SplineDataContainer contains invalid data!", this);
            return;
        }
        
        isInitialized = true;
    }
    
    public void MoveAlongSpline()
    {
        if (!isInitialized || splineDataContainer == null || !splineDataContainer.IsValid())
            return;
            
        float deltaTime = Time.deltaTime;
        float movementDelta = speed * deltaTime * direction;
        
        if (useDistanceBasedMovement)
        {
            // Update distance-based position
            currentDistance += movementDelta;
            
            // Handle movement type based on distance
            switch (movementType)
            {
                case MovementType.Clamp:
                    currentDistance = Mathf.Clamp(currentDistance, 0f, splineDataContainer.totalDistance);
                    // Update TF to match distance
                    currentTF = currentDistance / splineDataContainer.totalDistance;
                    break;
                    
                case MovementType.Loop:
                    if (currentDistance > splineDataContainer.totalDistance)
                        currentDistance = 0f;
                    else if (currentDistance < 0f)
                        currentDistance = splineDataContainer.totalDistance;
                    
                    // Update TF to match distance
                    currentTF = (currentDistance < 0) ? 0 : currentDistance / splineDataContainer.totalDistance;
                    if (currentDistance < 0) currentTF = 1f + currentTF; // For negative values
                    currentTF = Mathf.Repeat(currentTF, 1f);
                    break;
                    
                case MovementType.PingPong:
                    if (currentDistance > splineDataContainer.totalDistance)
                    {
                        currentDistance = splineDataContainer.totalDistance;
                        direction = -1; // Reverse direction
                    }
                    else if (currentDistance < 0f)
                    {
                        currentDistance = 0f;
                        direction = 1; // Reverse direction
                    }
                    // Update TF to match distance
                    currentTF = currentDistance / splineDataContainer.totalDistance;
                    break;
            }
            
            // Update position based on current distance
            UpdatePositionByDistance(currentDistance);
        }
        else
        {
            // Update TF-based position
            currentTF += (movementDelta / splineDataContainer.totalDistance) * direction;
            
            // Handle movement type based on TF
            switch (movementType)
            {
                case MovementType.Clamp:
                    currentTF = Mathf.Clamp01(currentTF);
                    currentDistance = currentTF * splineDataContainer.totalDistance;
                    break;
                    
                case MovementType.Loop:
                    currentTF = Mathf.Repeat(currentTF, 1f);
                    currentDistance = currentTF * splineDataContainer.totalDistance;
                    break;
                    
                case MovementType.PingPong:
                    if (currentTF > 1f)
                    {
                        currentTF = 1f;
                        direction = -1; // Reverse direction
                    }
                    else if (currentTF < 0f)
                    {
                        currentTF = 0f;
                        direction = 1; // Reverse direction
                    }
                    currentDistance = currentTF * splineDataContainer.totalDistance;
                    break;
            }
            
            // Update position based on current TF
            UpdatePositionByTF(currentTF);
        }
    }
    
    public void SetPositionByTF(float tf)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid()) return;
        
        currentTF = Mathf.Clamp01(tf);
        currentDistance = currentTF * splineDataContainer.totalDistance;
        UpdatePositionByTF(currentTF);
    }
    
    public void SetPositionByDistance(float distance)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid()) return;
        
        currentDistance = Mathf.Clamp(distance, 0f, splineDataContainer.totalDistance);
        currentTF = currentDistance / splineDataContainer.totalDistance;
        UpdatePositionByDistance(currentDistance);
    }
    
    private void UpdatePositionByTF(float tf)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid()) return;
        
        // Get interpolated position, tangent, and up-vector at the given TF
        Vector3 position = GetPositionAtTF(tf);
        Vector3 tangent = GetTangentAtTF(tf);
        Vector3 upVector = GetUpVectorAtTF(tf);
        
        // Update the transform position
        transform.position = position;
        
        // Update orientation if enabled
        if (orientToPath)
        {
            if (tangent != Vector3.zero)
            {
                transform.LookAt(transform.position + tangent, upVector);
                
                if (orientationSpace == Space.Self)
                {
                    // Convert to local space if needed
                    transform.rotation = transform.parent ? 
                        transform.parent.rotation * Quaternion.LookRotation(tangent, upVector) :
                        Quaternion.LookRotation(tangent, upVector);
                }
            }
        }
    }
    
    private void UpdatePositionByDistance(float distance)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid()) return;
        
        // Convert distance to TF and call the TF-based update
        float tf = distance / splineDataContainer.totalDistance;
        UpdatePositionByTF(tf);
    }
    
    // Public methods for getting data at specific points
    public Vector3 GetPositionAtTF(float tf)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid() || splineDataContainer.positions.Length < 2)
            return transform.position;
        
        tf = Mathf.Clamp01(tf);
        
        // Find the two closest cached points
        float scaledIndex = tf * (splineDataContainer.positions.Length - 1);
        int index1 = Mathf.FloorToInt(scaledIndex);
        int index2 = Mathf.Min(index1 + 1, splineDataContainer.positions.Length - 1);
        
        float lerpFactor = (scaledIndex - index1);
        
        // Interpolate between the two points
        Vector3 position = Vector3.Lerp(
            splineDataContainer.positions[index1], 
            splineDataContainer.positions[index2], 
            lerpFactor
        );
        
        return position;
    }
    
    public Vector3 GetTangentAtTF(float tf)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid() || splineDataContainer.tangents.Length < 2)
            return Vector3.forward;
        
        tf = Mathf.Clamp01(tf);
        
        // Find the two closest cached points
        float scaledIndex = tf * (splineDataContainer.tangents.Length - 1);
        int index1 = Mathf.FloorToInt(scaledIndex);
        int index2 = Mathf.Min(index1 + 1, splineDataContainer.tangents.Length - 1);
        
        float lerpFactor = (scaledIndex - index1);
        
        // Interpolate between the two tangents
        Vector3 tangent = Vector3.Lerp(
            splineDataContainer.tangents[index1], 
            splineDataContainer.tangents[index2], 
            lerpFactor
        );
        
        return tangent.normalized;
    }
    
    public Vector3 GetUpVectorAtTF(float tf)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid() || splineDataContainer.upVectors.Length < 2)
            return Vector3.up;
        
        tf = Mathf.Clamp01(tf);
        
        // Find the two closest cached points
        float scaledIndex = tf * (splineDataContainer.upVectors.Length - 1);
        int index1 = Mathf.FloorToInt(scaledIndex);
        int index2 = Mathf.Min(index1 + 1, splineDataContainer.upVectors.Length - 1);
        
        float lerpFactor = (scaledIndex - index1);
        
        // Interpolate between the two up vectors
        Vector3 upVector = Vector3.Lerp(
            splineDataContainer.upVectors[index1], 
            splineDataContainer.upVectors[index2], 
            lerpFactor
        );
        
        return upVector.normalized;
    }
    
    public Vector3 GetPositionAtDistance(float distance)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid())
            return transform.position;
        
        float clampedDistance = Mathf.Clamp(distance, 0f, splineDataContainer.totalDistance);
        float tf = clampedDistance / splineDataContainer.totalDistance;
        
        return GetPositionAtTF(tf);
    }
    
    public Vector3 GetTangentAtDistance(float distance)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid())
            return Vector3.forward;
        
        float clampedDistance = Mathf.Clamp(distance, 0f, splineDataContainer.totalDistance);
        float tf = clampedDistance / splineDataContainer.totalDistance;
        
        return GetTangentAtTF(tf);
    }
    
    public Vector3 GetUpVectorAtDistance(float distance)
    {
        if (splineDataContainer == null || !splineDataContainer.IsValid())
            return Vector3.up;
        
        float clampedDistance = Mathf.Clamp(distance, 0f, splineDataContainer.totalDistance);
        float tf = clampedDistance / splineDataContainer.totalDistance;
        
        return GetUpVectorAtTF(tf);
    }
    
    // Methods to start/stop automatic movement
    public void StartMovement()
    {
        autoMove = true;
    }
    
    public void StopMovement()
    {
        autoMove = false;
    }
    
    public void SetMovementDirection(int newDirection)
    {
        direction = (newDirection >= 0) ? 1 : -1;
    }
    
#if UNITY_EDITOR
    // For debugging purposes in the editor
    private void OnValidate()
    {
        if (!Application.isPlaying && splineDataContainer != null && splineDataContainer.IsValid())
        {
            // Update position in edit mode when TF changes
            if (!autoMove)
            {
                if (useDistanceBasedMovement)
                    UpdatePositionByDistance(currentDistance);
                else
                    UpdatePositionByTF(currentTF);
            }
        }
        
        speed = Mathf.Max(0f, speed); // Ensure speed is not negative
    }
#endif
}