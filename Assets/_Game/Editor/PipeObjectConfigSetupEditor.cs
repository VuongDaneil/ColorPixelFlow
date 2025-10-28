using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PipeObjectConfigSetup))]
public class PipeObjectConfigSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PipeObjectConfigSetup pipeConfigSetup = (PipeObjectConfigSetup)target;

        // Draw the default inspector fields
        DrawDefaultInspector();

        // Add a separator
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pipe Creation", EditorStyles.boldLabel);

        if (GUILayout.Button("Reload"))
        {
            pipeConfigSetup.Reload();
        }

        // Button to create a pipe between the selected start and end pixels
        if (GUILayout.Button("Create Pipe Between Selected Pixels"))
        {
            if (pipeConfigSetup.StartPixelComponent == null || pipeConfigSetup.EndPixelComponent == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both StartPixelComponent and EndPixelComponent.", "OK");
                return;
            }

            if (pipeConfigSetup.gridObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the GridObject reference.", "OK");
                return;
            }

            if (pipeConfigSetup.PipeHeadPrefab == null || pipeConfigSetup.PipeBodyPrefab == null || pipeConfigSetup.PipeTailPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign all pipe prefabs (Head, Body, Tail).", "OK");
                return;
            }

            // Check if the pixels are valid for pipe connection
            if (pipeConfigSetup.StartPixelComponent.PixelData == null || pipeConfigSetup.EndPixelComponent.PixelData == null)
            {
                EditorUtility.DisplayDialog("Error", "Start or End Pixel components don't have valid PixelData.", "OK");
                return;
            }

            // Validate pipe orientation (horizontal or vertical)
            PaintingPixel startPixel = pipeConfigSetup.StartPixelComponent.PixelData;
            PaintingPixel endPixel = pipeConfigSetup.EndPixelComponent.PixelData;

            if (startPixel.row != endPixel.row && startPixel.column != endPixel.column)
            {
                EditorUtility.DisplayDialog("Error", "Pipe must be either horizontal (same row) or vertical (same column).", "OK");
                return;
            }

            // Create the pipe
            pipeConfigSetup.CreatePipe();
            
            // Mark scene as dirty to save changes
            EditorUtility.SetDirty(pipeConfigSetup);
            if (pipeConfigSetup.gameObject != null)
                EditorUtility.SetDirty(pipeConfigSetup.gameObject);
        }

        // Button to clear all pipe setups
        if (GUILayout.Button("Clear All Pipe Setups"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear all pipe setups?", "Yes", "No"))
            {
                pipeConfigSetup.ClearAllPipeSetups();
            }
        }

        // Button to import pipe setups to PaintingConfig
        if (GUILayout.Button("SAVE"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Save to config?", "Yes", "No"))
            {
                if (pipeConfigSetup.gridObject == null || pipeConfigSetup.gridObject.paintingConfig == null)
                {
                    EditorUtility.DisplayDialog("Error", "GridObject or PaintingConfig reference is missing.", "OK");
                    return;
                }
            }

            pipeConfigSetup.ImportPipesToPaintingConfig(pipeConfigSetup.gridObject.paintingConfig);
            EditorUtility.SetDirty(pipeConfigSetup.gridObject.paintingConfig);
        }

        // Display some useful information
        EditorGUILayout.Space();
        if (pipeConfigSetup.pipeObjectSetups != null)
        {
            EditorGUILayout.LabelField($"Pipe Setups Created: {pipeConfigSetup.pipeObjectSetups.Count}", EditorStyles.helpBox);
        }
        
        if (pipeConfigSetup.gridObject != null && pipeConfigSetup.gridObject.pipeObjects != null)
        {
            EditorGUILayout.LabelField($"Pipe Objects in Scene: {pipeConfigSetup.gridObject.pipeObjects.Count}", EditorStyles.helpBox);
        }

        // Add a warning if required fields are missing
        if (pipeConfigSetup.StartPixelComponent == null || pipeConfigSetup.EndPixelComponent == null)
        {
            EditorGUILayout.HelpBox("Assign StartPixelComponent and EndPixelComponent to create pipes.", MessageType.Warning);
        }
        else if (pipeConfigSetup.gridObject == null)
        {
            EditorGUILayout.HelpBox("Assign GridObject reference to create pipes.", MessageType.Warning);
        }
        else if (pipeConfigSetup.PipeHeadPrefab == null || pipeConfigSetup.PipeBodyPrefab == null || pipeConfigSetup.PipeTailPrefab == null)
        {
            EditorGUILayout.HelpBox("Assign all pipe prefabs (Head, Body, Tail) to create pipes.", MessageType.Warning);
        }

        // Add information about pipe rotation
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Note: Horizontal pipes (in same row) will be rotated 90 degrees on Y-axis. Vertical pipes (in same column) maintain default orientation.", MessageType.Info);
    }
}