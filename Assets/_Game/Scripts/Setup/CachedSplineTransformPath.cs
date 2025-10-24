using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "CachedSplineTransformPath", menuName = "Curvy/Cached Spline Transform Path", order = 2)]
public class CachedSplineTransformPath : ScriptableObject
{
    [Header("Spline Path Transforms")]
    [Tooltip("List of transforms that define the cached spline path. These can be in world or local space.")]
    public List<Transform> pathTransforms = new List<Transform>();
    
    [Header("Spline Metadata")]
    public float totalDistance;
    public int sampleCount;
    public float resolution;
    
    [Header("Coordinate Space")]
    public bool isLocalSpace = false;
    public Transform spaceReference; // If null, uses world space
    
    // Version field to help detect when cache needs updating
    public string version = "1.0";
    
    // Arc-length parameterization data
    private List<float> cumulativeDistances;
    private bool isDistanceCacheDirty = true;
    
    // Method to check if the data is valid
    public bool IsValid()
    {
        // Check if the path transforms count has changed, which means cache should be refreshed
        if (pathTransforms != null && cumulativeDistances != null) 
        {
            if (pathTransforms.Count != cumulativeDistances.Count) 
            {
                isDistanceCacheDirty = true;
            }
        }
        
        return pathTransforms != null && pathTransforms.Count > 0;
    }
    
    // Get position at a given TF (0-1) value
    public Vector3 GetPositionAtTF(float tf)
    {
        if (!IsValid() || pathTransforms.Count < 2)
            return Vector3.zero;
        
        tf = Mathf.Clamp01(tf);
        
        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);
        
        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, pathTransforms.Count - 1);
        
        float lerpFactor = (truePosition - index1);
        
        Vector3 pos1 = GetTransformPosition(pathTransforms[index1]);
        Vector3 pos2 = GetTransformPosition(pathTransforms[index2]);
        
        return Vector3.Lerp(pos1, pos2, lerpFactor);
    }
    
    // Get tangent at a given TF (0-1) value
    public Vector3 GetTangentAtTF(float tf)
    {
        if (!IsValid() || pathTransforms.Count < 2)
            return Vector3.forward;
        
        tf = Mathf.Clamp01(tf);
        
        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);
        
        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, pathTransforms.Count - 1);
        
        Vector3 pos1 = GetTransformPosition(pathTransforms[index1]);
        Vector3 pos2 = GetTransformPosition(pathTransforms[index2]);
        
        Vector3 tangent = (pos2 - pos1).normalized;
        
        return tangent != Vector3.zero ? tangent : Vector3.forward;
    }
    
    // Get up vector at a given TF (0-1) value
    public Vector3 GetUpVectorAtTF(float tf)
    {
        if (!IsValid() || pathTransforms.Count < 2)
            return Vector3.up;
        
        tf = Mathf.Clamp01(tf);
        
        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);
        
        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, pathTransforms.Count - 1);
        
        float lerpFactor = (truePosition - index1);
        
        Vector3 up1 = GetTransformUpVector(pathTransforms[index1]);
        Vector3 up2 = GetTransformUpVector(pathTransforms[index2]);
        
        Vector3 upVector = Vector3.Lerp(up1, up2, lerpFactor);
        
        return upVector != Vector3.zero ? upVector.normalized : Vector3.up;
    }
    
    // Get position at a given distance
    public Vector3 GetPositionAtDistance(float distance)
    {
        if (!IsValid() || totalDistance <= 0)
            return Vector3.zero;
        
        float tf = Mathf.Clamp01(distance / totalDistance);
        return GetPositionAtTF(tf);
    }
    
    // Get tangent at a given distance
    public Vector3 GetTangentAtDistance(float distance)
    {
        if (!IsValid() || totalDistance <= 0)
            return Vector3.forward;
        
        float tf = Mathf.Clamp01(distance / totalDistance);
        return GetTangentAtTF(tf);
    }
    
    // Get up vector at a given distance
    public Vector3 GetUpVectorAtDistance(float distance)
    {
        if (!IsValid() || totalDistance <= 0)
            return Vector3.up;
        
        float tf = Mathf.Clamp01(distance / totalDistance);
        return GetUpVectorAtTF(tf);
    }
    
    // Helper method to get position from transform, considering coordinate space
    private Vector3 GetTransformPosition(Transform t)
    {
        if (t == null) return Vector3.zero;
        return t.position;
    }
    
    // Helper method to get up vector from transform, considering rotation
    private Vector3 GetTransformUpVector(Transform t)
    {
        if (t == null) return Vector3.up;
        return t.up;
    }
    
    // Helper method to get forward vector from transform
    private Vector3 GetTransformForwardVector(Transform t)
    {
        if (t == null) return Vector3.forward;
        return t.forward;
    }
    
    // Mark that the distance cache is dirty and needs to be recalculated
    public void MarkDistanceCacheDirty()
    {
        isDistanceCacheDirty = true;
    }
    
    // Method to calculate total distance of the path
    public float CalculateTotalDistance()
    {
        if (!IsValid() || pathTransforms.Count < 2)
            return 0f;
        
        float distance = 0f;
        for (int i = 1; i < pathTransforms.Count; i++)
        {
            if (pathTransforms[i] != null && pathTransforms[i-1] != null)
            {
                distance += Vector3.Distance(
                    GetTransformPosition(pathTransforms[i-1]), 
                    GetTransformPosition(pathTransforms[i])
                );
            }
        }
        
        return distance;
    }
    
    // Calculate cumulative distances for arc-length parameterization
    private void CalculateCumulativeDistances()
    {
        if (!IsValid() || pathTransforms.Count < 2)
        {
            cumulativeDistances = null;
            return;
        }
        
        cumulativeDistances = new List<float>();
        cumulativeDistances.Add(0f); // Start at 0
        
        float cumulativeDistance = 0f;
        for (int i = 1; i < pathTransforms.Count; i++)
        {
            if (pathTransforms[i] != null && pathTransforms[i-1] != null)
            {
                float segmentDistance = Vector3.Distance(
                    GetTransformPosition(pathTransforms[i-1]), 
                    GetTransformPosition(pathTransforms[i])
                );
                cumulativeDistance += segmentDistance;
                cumulativeDistances.Add(cumulativeDistance);
            }
            else
            {
                // If a transform is null, maintain the same distance value
                cumulativeDistances.Add(cumulativeDistance);
            }
        }
        
        totalDistance = cumulativeDistance;
        isDistanceCacheDirty = false;
    }
    
    // Convert normalized TF (0-1) to actual distance along the path using arc-length parameterization
    private float ConvertTfToDistance(float tf)
    {
        if (!IsValid() || cumulativeDistances == null || cumulativeDistances.Count == 0)
            return 0f;
        
        tf = Mathf.Clamp01(tf);
        float targetDistance = tf * totalDistance;
        
        // Find the segment that contains the target distance
        for (int i = 0; i < cumulativeDistances.Count - 1; i++)
        {
            if (targetDistance >= cumulativeDistances[i] && targetDistance <= cumulativeDistances[i + 1])
            {
                if (cumulativeDistances[i + 1] == cumulativeDistances[i])
                    return i; // Handle case where distances are the same
                
                float segmentT = (targetDistance - cumulativeDistances[i]) / (cumulativeDistances[i + 1] - cumulativeDistances[i]);
                return i + segmentT;
            }
        }
        
        // Handle edge cases (end of path)
        return pathTransforms.Count - 1;
    }
    
    // Convert actual distance to normalized TF (0-1) using arc-length parameterization
    private float ConvertDistanceToTf(float distance)
    {
        if (!IsValid() || cumulativeDistances == null || cumulativeDistances.Count == 0)
            return 0f;
        
        distance = Mathf.Clamp(distance, 0f, totalDistance);
        
        // Find the segment that contains the target distance
        for (int i = 0; i < cumulativeDistances.Count - 1; i++)
        {
            if (distance >= cumulativeDistances[i] && distance <= cumulativeDistances[i + 1])
            {
                if (cumulativeDistances[i + 1] == cumulativeDistances[i])
                    return (float)i / (pathTransforms.Count - 1); // Handle case where distances are the same
                
                float segmentT = (distance - cumulativeDistances[i]) / (cumulativeDistances[i + 1] - cumulativeDistances[i]);
                return Mathf.Clamp01((i + segmentT) / (pathTransforms.Count - 1));
            }
        }
        
        // Handle edge cases (end of path)
        return 1f;
    }
    
    // Get the actual TF value to use for interpolation based on arc-length parameterization
    private float GetTrueTfFromNormalizedTf(float normalizedTf)
    {
        if (isDistanceCacheDirty || cumulativeDistances == null)
        {
            CalculateCumulativeDistances();
        }
        
        if (cumulativeDistances == null || cumulativeDistances.Count < 2)
        {
            // Fall back to linear parameterization if arc-length is not available
            return normalizedTf * (pathTransforms.Count - 1);
        }
        
        // Convert the normalized TF (0-1) to the true position based on arc-length
        float truePosition = ConvertTfToDistance(normalizedTf);
        return truePosition;
    }
}