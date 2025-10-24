using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineDataContainer))]
public class SplineDataContainerEditor : Editor
{
    private SplineDataContainer splineDataContainer;
    private bool showPositions = true;
    private bool showTangents = true;
    private bool showUpVectors = true;
    private bool showDistances = true;

    void OnEnable()
    {
        splineDataContainer = (SplineDataContainer)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Spline Data Information", EditorStyles.boldLabel);
        
        // Display metadata
        EditorGUILayout.LabelField("Metadata:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Total Distance", splineDataContainer.totalDistance.ToString());
        EditorGUILayout.LabelField("Sample Count", splineDataContainer.sampleCount.ToString());
        EditorGUILayout.LabelField("Resolution", splineDataContainer.resolution.ToString());
        EditorGUILayout.LabelField("Version", splineDataContainer.version);
        
        EditorGUILayout.Space();

        // Toggle sections
        showPositions = EditorGUILayout.Foldout(showPositions, "Positions");
        if (showPositions && splineDataContainer.positions != null)
        {
            for (int i = 0; i < Mathf.Min(splineDataContainer.positions.Length, 10); i++)
            {
                EditorGUILayout.Vector3Field($"Position {i}", splineDataContainer.positions[i]);
            }
            if (splineDataContainer.positions.Length > 10)
            {
                EditorGUILayout.HelpBox($"... and {splineDataContainer.positions.Length - 10} more positions", MessageType.None);
            }
        }

        showTangents = EditorGUILayout.Foldout(showTangents, "Tangents");
        if (showTangents && splineDataContainer.tangents != null)
        {
            for (int i = 0; i < Mathf.Min(splineDataContainer.tangents.Length, 10); i++)
            {
                EditorGUILayout.Vector3Field($"Tangent {i}", splineDataContainer.tangents[i]);
            }
            if (splineDataContainer.tangents.Length > 10)
            {
                EditorGUILayout.HelpBox($"... and {splineDataContainer.tangents.Length - 10} more tangents", MessageType.None);
            }
        }

        showUpVectors = EditorGUILayout.Foldout(showUpVectors, "Up Vectors");
        if (showUpVectors && splineDataContainer.upVectors != null)
        {
            for (int i = 0; i < Mathf.Min(splineDataContainer.upVectors.Length, 10); i++)
            {
                EditorGUILayout.Vector3Field($"Up Vector {i}", splineDataContainer.upVectors[i]);
            }
            if (splineDataContainer.upVectors.Length > 10)
            {
                EditorGUILayout.HelpBox($"... and {splineDataContainer.upVectors.Length - 10} more up vectors", MessageType.None);
            }
        }

        showDistances = EditorGUILayout.Foldout(showDistances, "Distances");
        if (showDistances && splineDataContainer.distances != null)
        {
            for (int i = 0; i < Mathf.Min(splineDataContainer.distances.Length, 10); i++)
            {
                EditorGUILayout.FloatField($"Distance {i}", splineDataContainer.distances[i]);
            }
            if (splineDataContainer.distances.Length > 10)
            {
                EditorGUILayout.HelpBox($"... and {splineDataContainer.distances.Length - 10} more distances", MessageType.None);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        if (splineDataContainer.IsValid())
        {
            // Draw the cached spline path
            Handles.color = Color.green;
            for (int i = 0; i < splineDataContainer.positions.Length - 1; i++)
            {
                Handles.DrawLine(splineDataContainer.positions[i], splineDataContainer.positions[i + 1]);
            }
            
            // Draw position markers
            Handles.color = Color.cyan;
            foreach (Vector3 position in splineDataContainer.positions)
            {
                Handles.DrawWireCube(position, Vector3.one * 0.1f);
            }
            
            // Draw tangent indicators
            if (splineDataContainer.tangents != null)
            {
                Handles.color = Color.red;
                for (int i = 0; i < splineDataContainer.positions.Length; i++)
                {
                    Vector3 tangent = splineDataContainer.tangents[i];
                    Vector3 position = splineDataContainer.positions[i];
                    Handles.DrawLine(position, position + tangent * 0.2f);
                }
            }
            
            // Draw up vector indicators
            if (splineDataContainer.upVectors != null)
            {
                Handles.color = Color.blue;
                for (int i = 0; i < splineDataContainer.positions.Length; i++)
                {
                    Vector3 upVector = splineDataContainer.upVectors[i];
                    Vector3 position = splineDataContainer.positions[i];
                    Handles.DrawLine(position, position + upVector * 0.2f);
                }
            }
        }
    }
}