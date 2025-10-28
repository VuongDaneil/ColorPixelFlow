using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTransformBasedCached : MonoBehaviour
{
    public static PathTransformBasedCached Instance
    {
        get; private set;
    }

    public List<Transform> PathPoints = new List<Transform>();
    private List<float> cumulativeDistances = new List<float>();
    private bool isDistanceCacheDirty = false;
    public float totalDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public bool IsValid()
    {
        // Check if the path transforms count has changed, which means cache should be refreshed
        if (PathPoints != null && cumulativeDistances != null)
        {
            if (PathPoints.Count != cumulativeDistances.Count)
            {
                isDistanceCacheDirty = true;
            }
        }

        return PathPoints != null && PathPoints.Count > 0;
    }

    // Get position at a given TF (0-1) value
    public Vector3 GetPositionAtTF(float tf)
    {
        if (!IsValid() || PathPoints.Count < 2)
            return Vector3.zero;

        tf = Mathf.Clamp01(tf);

        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);

        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, PathPoints.Count - 1);

        float lerpFactor = (truePosition - index1);

        Vector3 pos1 = GetTransformPosition(PathPoints[index1]);
        Vector3 pos2 = GetTransformPosition(PathPoints[index2]);

        return Vector3.Lerp(pos1, pos2, lerpFactor);
    }

    // Get tangent at a given TF (0-1) value
    public Vector3 GetTangentAtTF(float tf)
    {
        if (!IsValid() || PathPoints.Count < 2)
            return Vector3.forward;

        tf = Mathf.Clamp01(tf);

        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);

        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, PathPoints.Count - 1);

        Vector3 pos1 = GetTransformPosition(PathPoints[index1]);
        Vector3 pos2 = GetTransformPosition(PathPoints[index2]);

        Vector3 tangent = (pos2 - pos1).normalized;

        return tangent != Vector3.zero ? tangent : Vector3.forward;
    }

    // Get up vector at a given TF (0-1) value
    public Vector3 GetUpVectorAtTF(float tf)
    {
        if (!IsValid() || PathPoints.Count < 2)
            return Vector3.up;

        tf = Mathf.Clamp01(tf);

        // Convert TF to true position using arc-length parameterization
        float truePosition = GetTrueTfFromNormalizedTf(tf);

        // Find the two closest transforms
        int index1 = Mathf.FloorToInt(truePosition);
        int index2 = Mathf.Min(index1 + 1, PathPoints.Count - 1);

        float lerpFactor = (truePosition - index1);

        Vector3 up1 = GetTransformUpVector(PathPoints[index1]);
        Vector3 up2 = GetTransformUpVector(PathPoints[index2]);

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
        if (!IsValid() || PathPoints.Count < 2)
            return 0f;

        float distance = 0f;
        for (int i = 1; i < PathPoints.Count; i++)
        {
            if (PathPoints[i] != null && PathPoints[i - 1] != null)
            {
                distance += Vector3.Distance(
                    GetTransformPosition(PathPoints[i - 1]),
                    GetTransformPosition(PathPoints[i])
                );
            }
        }

        return distance;
    }

    // Calculate cumulative distances for arc-length parameterization
    private void CalculateCumulativeDistances()
    {
        if (!IsValid() || PathPoints.Count < 2)
        {
            cumulativeDistances = null;
            return;
        }

        cumulativeDistances = new List<float>();
        cumulativeDistances.Add(0f); // Start at 0

        float cumulativeDistance = 0f;
        for (int i = 1; i < PathPoints.Count; i++)
        {
            if (PathPoints[i] != null && PathPoints[i - 1] != null)
            {
                float segmentDistance = Vector3.Distance(
                    GetTransformPosition(PathPoints[i - 1]),
                    GetTransformPosition(PathPoints[i])
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
        return PathPoints.Count - 1;
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
                    return (float)i / (PathPoints.Count - 1); // Handle case where distances are the same

                float segmentT = (distance - cumulativeDistances[i]) / (cumulativeDistances[i + 1] - cumulativeDistances[i]);
                return Mathf.Clamp01((i + segmentT) / (PathPoints.Count - 1));
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
            return normalizedTf * (PathPoints.Count - 1);
        }

        // Convert the normalized TF (0-1) to the true position based on arc-length
        float truePosition = ConvertTfToDistance(normalizedTf);
        return truePosition;
    }
}
