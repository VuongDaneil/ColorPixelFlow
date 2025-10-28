using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineDataContainerToTransformConverter))]
public class SplineDataContainerToTransformConverterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        SplineDataContainerToTransformConverter converter = (SplineDataContainerToTransformConverter)target;
        
        EditorGUILayout.LabelField("Spline Data Container to Transform Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool converts a SplineDataContainer asset to a CachedSplineTransformPath asset by creating Transform objects for each cached point.", MessageType.Info);
        
        // Draw the default inspector properties
        EditorGUILayout.PropertyField(serializedObject.FindProperty("splineDataContainer"), new GUIContent("Input SplineDataContainer", "The SplineDataContainer asset to convert"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathParent"), new GUIContent("Path Parent", "Parent GameObject for the created path transforms (optional)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathPointPrefab"), new GUIContent("Path Point Prefab", "Prefab to use for each path point (optional)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("worldSpace"), new GUIContent("World Space", "If true, creates path in world space; if false, creates in local space relative to path parent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathName"), new GUIContent("Path Name", "Name for the created path GameObject"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outputTransformPath"), new GUIContent("Output Transform Path", "The resulting CachedSplineTransformPath asset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PathCachedObj"), new GUIContent("Path Cached Obj", "The resulting CachedSplineTransformPath asset"));
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Convert SplineDataContainer to Transforms", GUILayout.Height(30)))
        {
            converter.ConvertToTransforms();
        }
        
        if (GUILayout.Button("Convert via Static Method (Alternative)", GUILayout.Height(25)))
        {
            if (converter.splineDataContainer != null)
            {
                string assetPath = "Assets/Resources/SplineDataContainers/" + converter.splineDataContainer.name + "_TransformPath.asset";
                CachedSplineTransformPath result = SplineDataContainerToTransformConverter.ConvertSplineDataContainer(converter.splineDataContainer, assetPath);
                
                if (result != null)
                {
                    Debug.Log("Conversion completed successfully! Created: " + result.name);
                }
            }
            else
            {
                Debug.LogError("Please assign a SplineDataContainer first!");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}