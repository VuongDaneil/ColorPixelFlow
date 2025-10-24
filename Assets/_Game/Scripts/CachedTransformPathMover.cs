using UnityEngine;

public class CachedTransformPathMover : MonoBehaviour
{
    [Header("Spline Data")]
    public CachedSplineTransformPath transformPath;
    
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float speed = 1f;
    
    [Tooltip("Current position along the spline as a 0-1 value (0 = start, 1 = end)")]
    [Range(0f, 1f)]
    public float currentTF = 0f;
    
    [Tooltip("End position for Clamp movement type as a 0-1 value (0 = start, 1 = end)")]
    [Range(0f, 1f)]
    public float endTF = 1f;
    
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
    
    [Header("Smooth Orientation")]
    [Tooltip("Enable smooth rotation instead of instant orientation")]
    public bool smoothOrientation = true;
    [Tooltip("Speed of rotation interpolation (higher = faster)")]
    public float orientationSpeed = 5f;
    [Tooltip("Interpolation method for rotation smoothing")]
    public RotationInterpolationType rotationInterpolation = RotationInterpolationType.Spherical;
    
    // Private variables for smooth rotation
    private Quaternion targetRotation;
    private Quaternion previousTargetRotation;
    
    public enum RotationInterpolationType
    {
        Spherical,  // Quaternion.Slerp - Smooth spherical interpolation
        Linear      // Quaternion.Lerp - Linear interpolation
    }
    
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
        previousTargetRotation = transform.rotation;
        targetRotation = transform.rotation;
    }
    
    private void Update()

    {
        if (autoMove && transformPath != null && transformPath.IsValid())
        {
            MoveAlongPath();
        }
    }
    
    private void Initialize()
    {
        if (transformPath == null)
        {
            Debug.LogError("CachedSplineTransformPath is not assigned!", this);
            return;
        }
        
        if (!transformPath.IsValid())
        {
            Debug.LogError("CachedSplineTransformPath contains invalid data!", this);
            return;
        }
        
        // Mark the distance cache as dirty to trigger recalculation
        transformPath.MarkDistanceCacheDirty();
        
        // Calculate total distance if not already set
        if (transformPath.totalDistance <= 0)
        {
            transformPath.totalDistance = transformPath.CalculateTotalDistance();
        }

        if (autoMove) SetPositionByTF(currentTF);

        isInitialized = true;
    }
    
    public void MoveAlongPath()
    {
        if (!isInitialized || transformPath == null || !transformPath.IsValid())
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
                    float maxDistance = endTF * transformPath.totalDistance;
                    currentDistance = Mathf.Clamp(currentDistance, 0f, maxDistance);
                    // Update TF to match distance
                    currentTF = currentDistance / transformPath.totalDistance;
                    break;
                    
                case MovementType.Loop:
                    if (currentDistance > transformPath.totalDistance)
                        currentDistance = 0f;
                    else if (currentDistance < 0f)
                        currentDistance = transformPath.totalDistance;
                    
                    // Update TF to match distance
                    currentTF = (currentDistance < 0) ? 0 : currentDistance / transformPath.totalDistance;
                    if (currentDistance < 0) currentTF = 1f + currentTF; // For negative values
                    currentTF = Mathf.Repeat(currentTF, 1f);
                    break;
                    
                case MovementType.PingPong:
                    float pingPongMaxDistance = endTF * transformPath.totalDistance;
                    if (currentDistance > pingPongMaxDistance)
                    {
                        currentDistance = pingPongMaxDistance;
                        direction = -1; // Reverse direction to go backward
                    }
                    else if (currentDistance < 0f)
                    {
                        currentDistance = 0f;
                        direction = 1; // Reverse direction to go forward
                    }
                    // Update TF to match distance
                    currentTF = currentDistance / transformPath.totalDistance;
                    break;
            }
            
            // Update position based on current distance
            UpdatePositionByDistance(currentDistance);
        }
        else
        {
            // Update TF-based position - movementDelta already includes direction
            currentTF += movementDelta / transformPath.totalDistance;
            
            // Handle movement type based on TF
            switch (movementType)
            {
                case MovementType.Clamp:
                    currentTF = Mathf.Clamp(currentTF, 0f, endTF);
                    currentDistance = currentTF * transformPath.totalDistance;
                    break;
                    
                case MovementType.Loop:
                    currentTF = Mathf.Repeat(currentTF, 1f);
                    currentDistance = currentTF * transformPath.totalDistance;
                    break;
                    
                case MovementType.PingPong:
                    if (currentTF > endTF)
                    {
                        currentTF = endTF;
                        direction = -1; // Reverse direction to go backward
                    }
                    else if (currentTF < 0f)
                    {
                        currentTF = 0f;
                        direction = 1; // Reverse direction to go forward
                    }
                    currentDistance = currentTF * transformPath.totalDistance;
                    break;
            }
            
            // Update position based on current TF
            UpdatePositionByTF(currentTF);
        }
    }
    
    public void SetPositionByTF(float tf)
    {
        if (transformPath == null || !transformPath.IsValid()) return;
        
        if (movementType == MovementType.Clamp)
        {
            currentTF = Mathf.Clamp(tf, 0f, endTF);
        }
        else
        {
            currentTF = Mathf.Clamp01(tf);
        }
        
        currentDistance = currentTF * transformPath.totalDistance;
        UpdatePositionByTF(currentTF);
    }
    
    public void SetPositionByDistance(float distance)
    {
        if (transformPath == null || !transformPath.IsValid()) return;
        
        if (movementType == MovementType.Clamp)
        {
            float maxDistance = endTF * transformPath.totalDistance;
            currentDistance = Mathf.Clamp(distance, 0f, maxDistance);
        }
        else
        {
            currentDistance = Mathf.Clamp(distance, 0f, transformPath.totalDistance);
        }
        
        currentTF = currentDistance / transformPath.totalDistance;
        UpdatePositionByDistance(currentDistance);
    }
    
    private void UpdatePositionByTF(float tf)
    {
        if (transformPath == null || !transformPath.IsValid()) return;
        
        // Get interpolated position, tangent, and up-vector at the given TF
        Vector3 position = transformPath.GetPositionAtTF(tf);
        Vector3 tangent = transformPath.GetTangentAtTF(tf);
        Vector3 upVector = transformPath.GetUpVectorAtTF(tf);
        
        // Update the transform position
        transform.position = position;
        
        // Update orientation if enabled
        if (orientToPath)
        {
            if (tangent != Vector3.zero)
            {
                Quaternion newRotation;
                
                if (orientationSpace == Space.Self)
                {
                    // Convert to local space if needed
                    newRotation = transform.parent ? 
                        transform.parent.rotation * Quaternion.LookRotation(tangent, upVector) :
                        Quaternion.LookRotation(tangent, upVector);
                }
                else
                {
                    newRotation = Quaternion.LookRotation(tangent, upVector);
                }
                
                if (smoothOrientation)
                {
                    // Only interpolate if the target rotation has changed
                    if (newRotation != previousTargetRotation)
                    {
                        targetRotation = newRotation;
                        previousTargetRotation = newRotation;
                    }
                    
                    // Smoothly interpolate to the target rotation using selected method
                    switch (rotationInterpolation)
                    {
                        case RotationInterpolationType.Spherical:
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, orientationSpeed * Time.deltaTime);
                            break;
                        case RotationInterpolationType.Linear:
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, orientationSpeed * Time.deltaTime);
                            break;
                    }
                }
                else
                {
                    // Instant orientation (original behavior)
                    transform.rotation = newRotation;
                    targetRotation = newRotation;
                    previousTargetRotation = newRotation;
                }
            }
        }
    }
    
    private void UpdatePositionByDistance(float distance)
    {
        if (transformPath == null || !transformPath.IsValid()) return;
        
        // Convert distance to TF and call the TF-based update
        float tf = distance / transformPath.totalDistance;
        UpdatePositionByTF(tf);
    }
    
    // Public methods for getting data at specific points
    public Vector3 GetPositionAtTF(float tf)
    {
        if (transformPath == null || !transformPath.IsValid())
            return transform.position;
        
        return transformPath.GetPositionAtTF(tf);
    }
    
    public Vector3 GetTangentAtTF(float tf)
    {
        if (transformPath == null || !transformPath.IsValid())
            return Vector3.forward;
        
        return transformPath.GetTangentAtTF(tf);
    }
    
    public Vector3 GetUpVectorAtTF(float tf)
    {
        if (transformPath == null || !transformPath.IsValid())
            return Vector3.up;
        
        return transformPath.GetUpVectorAtTF(tf);
    }
    
    public Vector3 GetPositionAtDistance(float distance)
    {
        if (transformPath == null || !transformPath.IsValid())
            return transform.position;
        
        return transformPath.GetPositionAtDistance(distance);
    }
    
    public Vector3 GetTangentAtDistance(float distance)
    {
        if (transformPath == null || !transformPath.IsValid())
            return Vector3.forward;
        
        return transformPath.GetTangentAtDistance(distance);
    }
    
    public Vector3 GetUpVectorAtDistance(float distance)
    {
        if (transformPath == null || !transformPath.IsValid())
            return Vector3.up;
        
        return transformPath.GetUpVectorAtDistance(distance);
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
        if (!Application.isPlaying && transformPath != null && transformPath.IsValid())
        {
            // Mark distance cache as dirty and recalculate total distance
            transformPath.MarkDistanceCacheDirty();
            if (transformPath.totalDistance <= 0)
            {
                transformPath.totalDistance = transformPath.CalculateTotalDistance();
            }
            
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
        endTF = Mathf.Clamp01(endTF); // Ensure endTF is between 0 and 1
        
        // In Clamp mode, make sure endTF is not less than currentTF
        if (movementType == MovementType.Clamp)
        {
            endTF = Mathf.Max(endTF, currentTF);
        }
    }
#endif
}