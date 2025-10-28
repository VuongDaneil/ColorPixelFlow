using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PaintingConfigSetup))]
public class PaintingConfigSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PaintingConfigSetup setup = (PaintingConfigSetup)target;

        SerializedProperty targetPaintingProp = serializedObject.FindProperty("TargetPainting");
        EditorGUILayout.PropertyField(targetPaintingProp, new GUIContent("Target Painting", "The original painting sprite to sample colors from"));
        
        SerializedProperty colorPaletteProp = serializedObject.FindProperty("colorPalette");
        EditorGUILayout.PropertyField(colorPaletteProp, new GUIContent("Color Palette", "The colors that will be used in the grid"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Color Filter Settings", EditorStyles.boldLabel);
        
        SerializedProperty useColorFilterProp = serializedObject.FindProperty("useColorFilter");
        EditorGUILayout.PropertyField(useColorFilterProp, new GUIContent("Use Color Filter", "Enable to restrict colors to only those in the Color Codes In Use list"));
        
        SerializedProperty colorCodeInUseProp = serializedObject.FindProperty("colorCodeInUse");
        // Only show the color code list if the filter is enabled
        if (setup.useColorFilter)
        {
            EditorGUILayout.PropertyField(colorCodeInUseProp, new GUIContent("Color Codes In Use", "Only colors with codes in this list will be used"), true);
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        // Validate inputs before allowing sample
        bool canSample = setup.CanSample();


        EditorGUI.BeginDisabledGroup(!canSample);
        
        if (GUILayout.Button("Sample Painting to Grid", GUILayout.Height(30)))
        {
            setup.SamplePaintingToGrid();
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (!canSample)
        {
            EditorGUILayout.HelpBox("Please assign all required inputs (Target Painting, Target Grid, and Color Palette) to enable sampling.", MessageType.Info);
        }
        
        // Display the result painting config
        EditorGUILayout.Space();
        
        // Display grid information if available
        if (setup.CurrentGridObject != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Grid Size: {setup.CurrentGridObject.gridSize.x} x {setup.CurrentGridObject.gridSize.y}");
            EditorGUILayout.LabelField($"Total Pixels: {setup.CurrentGridObject.GetTotalPixels()}");
        }
        
        // Display color palette information if available
        if (setup.colorPalette != null && setup.colorPalette.colorPallete.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Palette Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Colors in Palette: {setup.colorPalette.colorPallete.Count}");
            
            // Show information about color codes in use if specified
            if (setup.useColorFilter && setup.ColorCodeInUse != null && setup.ColorCodeInUse.Count > 0)
            {
                EditorGUILayout.LabelField($"Colors Being Used: {setup.ColorCodeInUse.Count}");
                
                // Check for invalid color codes
                List<string> invalidCodes = new List<string>();
                foreach (string code in setup.ColorCodeInUse)
                {
                    if (!setup.colorPalette.colorPallete.ContainsKey(code))
                    {
                        invalidCodes.Add(code);
                    }
                }
                
                if (invalidCodes.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Warning: The following color codes are not in the palette: {string.Join(", ", invalidCodes)}", MessageType.Warning);
                }
            }
            else if (setup.useColorFilter && (setup.ColorCodeInUse == null || setup.ColorCodeInUse.Count == 0))
            {
                EditorGUILayout.HelpBox("Color filtering is enabled but no color codes are specified!", MessageType.Warning);
            }
        }
    }
}