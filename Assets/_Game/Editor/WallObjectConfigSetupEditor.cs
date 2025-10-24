using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WallObjectConfigSetup))]
public class WallObjectConfigSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WallObjectConfigSetup wallConfigSetup = (WallObjectConfigSetup)target;

        // Draw the default inspector fields
        DrawDefaultInspector();

        // Add a separator
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wall Creation", EditorStyles.boldLabel);

        if (GUILayout.Button("Reload"))
        {
            wallConfigSetup.Reload();
        }

        // Button to create a pipe between the selected start and end pixels
        if (GUILayout.Button("Create Wall"))
        {

            if (wallConfigSetup.gridObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the GridObject reference.", "OK");
                return;
            }

            // Create the pipe
            wallConfigSetup.CreateWall();

            // Mark scene as dirty to save changes
            EditorUtility.SetDirty(wallConfigSetup);
            if (wallConfigSetup.gameObject != null)
                EditorUtility.SetDirty(wallConfigSetup.gameObject);
        }

        // Button to clear all pipe setups
        if (GUILayout.Button("Clear All Wall Setups"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear all wall setups?", "Yes", "No"))
            {
                wallConfigSetup.ClearWallSetups();

                // Also clear pipe objects from the grid if they exist
                if (wallConfigSetup.gridObject != null && wallConfigSetup.gridObject.pipeObjects != null)
                {
                    // Destroy the pipe gameobjects
                    List<WallObject> currentWalls = new List<WallObject>(wallConfigSetup.gridObject.wallObjects);
                    foreach (var wallObj in currentWalls)
                    {
                        wallConfigSetup.gridObject.RemoveWallObject(wallObj);
                    }
                    wallConfigSetup.gridObject.pipeObjects.Clear();
                }

                EditorUtility.SetDirty(wallConfigSetup);
                if (wallConfigSetup.gameObject != null)
                    EditorUtility.SetDirty(wallConfigSetup.gameObject);
            }
        }

        // Button to import pipe setups to PaintingConfig
        if (GUILayout.Button("SAVE"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Save to config?", "Yes", "No"))
            {
                if (wallConfigSetup.gridObject == null || wallConfigSetup.gridObject.paintingConfig == null)
                {
                    EditorUtility.DisplayDialog("Error", "GridObject or PaintingConfig reference is missing.", "OK");
                    return;
                }

                wallConfigSetup.ImportPipesToPaintingConfig(wallConfigSetup.gridObject.paintingConfig);
                EditorUtility.SetDirty(wallConfigSetup.gridObject.paintingConfig);
            }
        }

        // Display some useful information
        EditorGUILayout.Space();
        if (wallConfigSetup.wallObjectSetups != null)
        {
            EditorGUILayout.LabelField($"Wall Setups Created: {wallConfigSetup.wallObjectSetups.Count}", EditorStyles.helpBox);
        }

        if (wallConfigSetup.gridObject != null && wallConfigSetup.gridObject.pipeObjects != null)
        {
            EditorGUILayout.LabelField($"Wall Objects in Scene: {wallConfigSetup.gridObject.pipeObjects.Count}", EditorStyles.helpBox);
        }

        // Add information about pipe rotation
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Note: Horizontal Wall (in same row) will be rotated 90 degrees on Y-axis. Vertical pipes (in same column) maintain default orientation.", MessageType.Info);
    }
}
