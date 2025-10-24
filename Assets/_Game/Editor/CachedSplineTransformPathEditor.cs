using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CachedSplineTransformPath))]
public class CachedSplineTransformPathEditor : Editor
{
    private CachedSplineTransformPath transformPath;
    private bool showPathTransforms = true;

    void OnEnable()
    {
        transformPath = (CachedSplineTransformPath)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Spline Transform Path Information", EditorStyles.boldLabel);
        
        // Display metadata
        EditorGUILayout.LabelField("Metadata:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Total Distance", transformPath.totalDistance.ToString("F2"));
        EditorGUILayout.LabelField("Sample Count", transformPath.sampleCount.ToString());
        EditorGUILayout.LabelField("Resolution", transformPath.resolution.ToString());
        EditorGUILayout.LabelField("Version", transformPath.version);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isLocalSpace"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spaceReference"));
        
        EditorGUILayout.Space();

        // Toggle sections
        showPathTransforms = EditorGUILayout.Foldout(showPathTransforms, "Path Transforms");
        if (showPathTransforms && transformPath.pathTransforms != null)
        {
            SerializedProperty transformsProperty = serializedObject.FindProperty("pathTransforms");
            EditorGUILayout.PropertyField(transformsProperty, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        if (transformPath.IsValid() && transformPath.pathTransforms.Count > 1)
        {
            // Draw the cached spline path using the transforms
            Handles.color = Color.green;
            for (int i = 0; i < transformPath.pathTransforms.Count - 1; i++)
            {
                Transform current = transformPath.pathTransforms[i];
                Transform next = transformPath.pathTransforms[i + 1];
                
                if (current != null && next != null)
                {
                    Handles.DrawLine(current.position, next.position);
                }
            }
            
            // Draw position markers at each transform
            Handles.color = Color.cyan;
            foreach (Transform t in transformPath.pathTransforms)
            {
                if (t != null)
                {
                    Handles.DrawWireCube(t.position, Vector3.one * 0.1f);
                    
                    // Draw the forward and up vectors
                    Handles.color = Color.red;
                    Handles.DrawLine(t.position, t.position + t.forward * 0.3f);
                    
                    Handles.color = Color.blue;
                    Handles.DrawLine(t.position, t.position + t.up * 0.3f);
                }
            }
        }
    }
}