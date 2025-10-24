using UnityEngine;
using UnityEditor;
using FluffyUnderware.Curvy;

[CustomEditor(typeof(SplineDataCacher))]
public class SplineDataCacherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        SplineDataCacher cacher = (SplineDataCacher)target;
        
        // Show default inspector properties
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resolution"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("splineDataContainer"));
        
        // Show validation information
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spline Information", EditorStyles.boldLabel);
        
        if (cacher.GetComponent<CurvySpline>() != null)
        {
            CurvySpline spline = cacher.GetComponent<CurvySpline>();
            EditorGUILayout.LabelField("Spline Name", spline.name);
            EditorGUILayout.LabelField("Spline Length", spline.Length.ToString("F2"));
            EditorGUILayout.LabelField("Expected Sample Count", cacher.GetExpectedSampleCount().ToString());
            EditorGUILayout.LabelField("Estimated Resolution", (spline.Length / (cacher.GetExpectedSampleCount() - 1)).ToString("F2"));
            
            // Show status
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            if (cacher.isCached)
            {
                EditorGUILayout.LabelField("Status", "Data Cached", EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.LabelField("Status", "Not Cached", EditorStyles.helpBox);
            }
            
            // Validation checks
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            if (spline.Length <= 0)
            {
                EditorGUILayout.HelpBox("Error: Spline length is 0 or negative. Check spline configuration.", MessageType.Error);
            }
            else if (cacher.resolution <= 0)
            {
                EditorGUILayout.HelpBox("Error: Resolution must be greater than 0.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Validation Status", "All validations passed", EditorStyles.helpBox);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Error: No CurvySpline component found on this GameObject.", MessageType.Error);
        }
        
        // Cache button
        EditorGUILayout.Space();
        if (GUILayout.Button("Cache Spline Data", GUILayout.Height(30)))
        {
            if (cacher.ValidateSpline())
            {
                cacher.CacheSplineData();
            }
            else
            {
                Debug.LogError("Cannot cache spline data due to validation errors.");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}