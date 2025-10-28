using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SplineDataContainerToTransformConverter : MonoBehaviour
{
    [Header("Conversion Settings")]
    [Tooltip("The SplineDataContainer asset to convert to transforms")]
    public SplineDataContainer splineDataContainer;
    
    [Tooltip("Parent GameObject for the created path transforms (optional)")]
    public Transform pathParent;
    
    [Tooltip("Prefab to use for each path point (optional, will create empty GameObject if null)")]
    public GameObject pathPointPrefab;
    
    [Header("Conversion Output")]
    [Tooltip("The resulting CachedSplineTransformPath asset")]
    public CachedSplineTransformPath outputTransformPath;
    public PathTransformBasedCached PathCachedObj;
    
    [Header("Options")]
    [Tooltip("If true, will create the path in world space. If false, will create in local space relative to pathParent")]
    public bool worldSpace = true;
    
    [Tooltip("Name for the created path GameObject")]
    public string pathName = "ConvertedSplinePath";
    
    [ContextMenu("Convert SplineDataContainer to Transforms")]
    public void ConvertToTransforms()
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
        
        if (Application.isPlaying)
        {
            Debug.LogWarning("Conversion should be done in edit mode. Results may not persist.");
            ConvertInPlayMode();
            return;
        }
        
        ConvertInEditMode();
    }
    
    private void ConvertInEditMode()
    {
        // Create path parent if not provided
        GameObject pathParentGO = null;
        if (pathParent == null)
        {
            pathParentGO = new GameObject(pathName);
            pathParent = pathParentGO.transform;
        }
        
        // Clear existing children
        for (int i = pathParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(pathParent.GetChild(i).gameObject);
        }
        
        // Create new transforms based on the cached data
        List<Transform> pathTransforms = new List<Transform>();
        
        for (int i = 0; i < splineDataContainer.positions.Length; i++)
        {
            GameObject pointGO;
            
            if (pathPointPrefab != null)
            {
                pointGO = Instantiate(pathPointPrefab, splineDataContainer.positions[i], Quaternion.identity);
            }
            else
            {
                pointGO = new GameObject($"PathPoint_{i:000}");
                pointGO.transform.position = splineDataContainer.positions[i];
            }
            
            pointGO.transform.SetParent(pathParent, false);
            
            // Set rotation based on tangent and up vector if available
            if (i < splineDataContainer.tangents.Length && i < splineDataContainer.upVectors.Length)
            {
                Vector3 tangent = splineDataContainer.tangents[i];
                Vector3 upVector = splineDataContainer.upVectors[i];
                
                if (tangent != Vector3.zero && upVector != Vector3.zero)
                {
                    pointGO.transform.rotation = Quaternion.LookRotation(tangent, upVector);
                }
            }
            
            pathTransforms.Add(pointGO.transform);
        }
        
        // Create the CachedSplineTransformPath asset
        if (outputTransformPath == null)
        {
            outputTransformPath = ScriptableObject.CreateInstance<CachedSplineTransformPath>();
            string assetPath = "Assets/Resources/SplineDataContainers";
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources/SplineDataContainers");
            
            // Create unique asset name
            string assetFileName = splineDataContainer.name + "_TransformPath.asset";
            string fullPath = assetPath + "/" + assetFileName;
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            
            AssetDatabase.CreateAsset(outputTransformPath, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        PathCachedObj.PathPoints = pathTransforms;
        // Populate the new asset with the transforms
        outputTransformPath.pathTransforms = pathTransforms;
        outputTransformPath.sampleCount = splineDataContainer.sampleCount;
        outputTransformPath.resolution = splineDataContainer.resolution;
        outputTransformPath.totalDistance = splineDataContainer.totalDistance;
        outputTransformPath.isLocalSpace = !worldSpace;
        outputTransformPath.spaceReference = worldSpace ? null : pathParent;
        
        // Calculate total distance
        outputTransformPath.totalDistance = outputTransformPath.CalculateTotalDistance();
        
        EditorUtility.SetDirty(outputTransformPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Successfully converted SplineDataContainer to {pathTransforms.Count} transforms. Created CachedSplineTransformPath asset: {outputTransformPath.name}", this);
    }
    
    private void ConvertInPlayMode()
    {
        // This would be a runtime version of the conversion
        // For runtime use, we'd need to create a temporary parent
        if (splineDataContainer == null || !splineDataContainer.IsValid())
        {
            Debug.LogError("Invalid spline data for conversion!");
            return;
        }
        
        // Create path parent if not provided
        GameObject pathParentGO = null;
        if (pathParent == null)
        {
            pathParentGO = new GameObject(pathName);
            pathParent = pathParentGO.transform;
        }
        
        // Clear existing children
        for (int i = pathParent.childCount - 1; i >= 0; i--)
        {
            Destroy(pathParent.GetChild(i).gameObject);
        }
        
        // Create new transforms based on the cached data
        List<Transform> pathTransforms = new List<Transform>();
        
        for (int i = 0; i < splineDataContainer.positions.Length; i++)
        {
            GameObject pointGO;
            
            if (pathPointPrefab != null)
            {
                pointGO = Instantiate(pathPointPrefab, splineDataContainer.positions[i], Quaternion.identity);
            }
            else
            {
                pointGO = new GameObject($"PathPoint_{i:000}");
                pointGO.transform.position = splineDataContainer.positions[i];
            }
            
            pointGO.transform.SetParent(pathParent, false);
            
            // Set rotation based on tangent and up vector if available
            if (i < splineDataContainer.tangents.Length && i < splineDataContainer.upVectors.Length)
            {
                Vector3 tangent = splineDataContainer.tangents[i];
                Vector3 upVector = splineDataContainer.upVectors[i];
                
                if (tangent != Vector3.zero && upVector != Vector3.zero)
                {
                    pointGO.transform.rotation = Quaternion.LookRotation(tangent, upVector);
                }
            }
            
            pathTransforms.Add(pointGO.transform);
        }
        
        Debug.Log($"Converted SplineDataContainer to {pathTransforms.Count} transforms at runtime.", this);
    }
    
    // Static method to convert from editor script
    public static CachedSplineTransformPath ConvertSplineDataContainer(SplineDataContainer input, string assetPath = "")
    {
        if (input == null || !input.IsValid())
        {
            Debug.LogError("Invalid input SplineDataContainer!");
            return null;
        }
        
        // Create GameObjects to represent the path
        GameObject pathParent = new GameObject("ConvertedPath_" + input.name);
        
        // Create new transforms based on the cached data
        List<Transform> pathTransforms = new List<Transform>();
        
        for (int i = 0; i < input.positions.Length; i++)
        {
            GameObject pointGO = new GameObject($"PathPoint_{i:000}");
            pointGO.transform.position = input.positions[i];
            pointGO.transform.SetParent(pathParent.transform, false);
            
            // Set rotation based on tangent and up vector if available
            if (i < input.tangents.Length && i < input.upVectors.Length)
            {
                Vector3 tangent = input.tangents[i];
                Vector3 upVector = input.upVectors[i];
                
                if (tangent != Vector3.zero && upVector != Vector3.zero)
                {
                    pointGO.transform.rotation = Quaternion.LookRotation(tangent, upVector);
                }
            }
            
            pathTransforms.Add(pointGO.transform);
        }
        
        // Create the CachedSplineTransformPath asset
        CachedSplineTransformPath output = ScriptableObject.CreateInstance<CachedSplineTransformPath>();
        
        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = "Assets/Resources/SplineDataContainers/" + input.name + "_TransformPath.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }
        else
        {
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }
        
        output.pathTransforms = pathTransforms;
        output.sampleCount = input.sampleCount;
        output.resolution = input.resolution;
        output.totalDistance = input.totalDistance;
        
        AssetDatabase.CreateAsset(output, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        return output;
    }
}