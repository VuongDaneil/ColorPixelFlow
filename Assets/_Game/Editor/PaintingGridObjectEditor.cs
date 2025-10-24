using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PaintingGridObject))]
public class PaintingGridObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get reference to the script
        PaintingGridObject paintingGridObject = (PaintingGridObject)target;

        // Add a separator
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Painting Config Operations", EditorStyles.boldLabel);

        // Apply Painting Config button
        if (GUILayout.Button("Apply Painting Config"))
        {
            if (paintingGridObject.paintingConfig != null)
            {
                paintingGridObject.ApplyPaintingConfig();
                EditorUtility.SetDirty(target);
                Debug.Log("Painting config applied successfully.");
            }
            else
            {
                Debug.LogWarning("No PaintingConfig assigned to the grid object.");
            }
        }

        // Clear Grid to White button
        if (GUILayout.Button("Clear Painting"))
        {
            paintingGridObject.ClearAllPipe();
            paintingGridObject.ClearToWhite();
            EditorUtility.SetDirty(target);
            Debug.Log("Grid cleared to white successfully.");
        }
        
        // Regenerate Grid button (if needed for convenience)
        if (GUILayout.Button("Regenerate Grid"))
        {
            if (paintingGridObject.GetComponent<GridGenerator>() != null)
            {
                GridGenerator gridGenerator = paintingGridObject.GetComponent<GridGenerator>();
                if (gridGenerator.centerPoint != null)
                {
                    gridGenerator.ClearGrid();
                    gridGenerator.GenerateGrid();
                    Debug.Log("Grid regenerated successfully.");
                }
                else
                {
                    Debug.LogWarning("No center point assigned in GridGenerator component.");
                }
            }
            else
            {
                Debug.LogWarning("No GridGenerator component found on this object.");
            }
        }
    }
}