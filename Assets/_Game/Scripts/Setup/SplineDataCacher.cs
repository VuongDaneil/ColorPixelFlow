using UnityEngine;
using System.IO;
using FluffyUnderware.Curvy;
using UnityEditor;

[RequireComponent(typeof(CurvySpline))]
public class SplineDataCacher : MonoBehaviour
{
    [Header("Spline Settings")]
    [Tooltip("Reference to the CurvySpline component to cache data from")]
    private CurvySpline spline; // Use private since we get it from the component
    
    [Header("Caching Settings")]
    [Tooltip("Distance between sample points. Lower values = more accuracy but more memory usage")]
    [Range(0.1f, 10f)]
    public float resolution = 1f;
    
    [Header("Output")]
    [Tooltip("The ScriptableObject that will contain the cached spline data")]
    public SplineDataContainer splineDataContainer;
    
    [Header("Status")]
    public bool isCached = false;
    
    private void OnValidate()
    {
        // Validate resolution is positive
        resolution = Mathf.Max(0.01f, resolution);
        spline = GetComponent<CurvySpline>();
    }
    
    private void OnEnable()
    {
    }
    
    [ContextMenu("Cache Spline Data")]
    public void CacheSplineData()
    {
        if (!ValidateSpline())
            return;
            
        CacheSplineDataInternal();
    }
    
    public bool ValidateSpline()
    {
        if (spline == null)
        {
            Debug.LogError("Spline reference is null. SplineDataCacher requires a CurvySpline component on the same GameObject.", this);
            return false;
        }
        
        if (resolution <= 0)
        {
            Debug.LogError("Resolution must be greater than 0.", this);
            return false;
        }
        
        if (spline.Length <= 0)
        {
            Debug.LogError("Spline length is 0 or less. Make sure the spline is properly configured.", this);
            return false;
        }
        
        return true;
    }
    
    private void CacheSplineDataInternal()
    {
        // Create or get the SplineDataContainer
        if (splineDataContainer == null)
        {
            splineDataContainer = ScriptableObject.CreateInstance<SplineDataContainer>();
            string path = "Assets/Resources/SplineDataContainers";
            Directory.CreateDirectory(Path.GetDirectoryName(Application.dataPath) + "/" + path);
            string assetPath = path + "/" + spline.name + "Data.asset";
            AssetDatabase.CreateAsset(splineDataContainer, assetPath);
            AssetDatabase.SaveAssets();
        }
        
        // Calculate the number of samples needed
        int sampleCount = Mathf.CeilToInt(spline.Length / resolution) + 1;
        sampleCount = Mathf.Max(2, sampleCount); // Ensure at least 2 points for a valid spline
        
        // Initialize the arrays in the data container
        splineDataContainer.InitializeArrays(sampleCount);
        splineDataContainer.resolution = resolution;
        splineDataContainer.totalDistance = spline.Length;
        
        // Sample the spline at regular intervals
        for (int i = 0; i < sampleCount; i++)
        {
            float tf = (float)i / (sampleCount - 1); // Normalize to 0-1 range
            float distance = tf * spline.Length; // Convert TF to distance
            
            // Get position, tangent, and up-vector at this point
            splineDataContainer.positions[i] = spline.InterpolateByDistance(distance);
            splineDataContainer.tangents[i] = spline.GetTangentByDistance(distance);
            splineDataContainer.upVectors[i] = spline.GetOrientationUpFast(spline.DistanceToTF(distance));
            
            // Calculate cumulative distance
            splineDataContainer.distances[i] = distance;
        }
        
        // Mark as cached
        isCached = true;
        
        // Save the asset
        EditorUtility.SetDirty(splineDataContainer);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Successfully cached spline data with {sampleCount} samples for spline: {spline.name}", this);
    }
    
    // Method to get the number of samples that would be generated for current settings
    public int GetExpectedSampleCount()
    {
        if (spline == null || spline.Length <= 0)
            return 0;
            
        return Mathf.CeilToInt(spline.Length / resolution) + 1;
    }
}