using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PaintingAdvancedSetup))]
public class PaintingAdvancedSetupEditor : Editor
{
    PaintingAdvancedSetup manager;
    private static readonly string[] createLabels = { "Set Key", "Create Pipe", "Create Wall"};
    private static readonly string[] deleteLabels = { "Delete all Key", "Delete all Pipe", "Delete all Wall" };

    private void OnEnable()
    {
        manager = (PaintingAdvancedSetup)target;
        manager.ToolActive = false;
    }

    private void OnDisable()
    {
        manager.ToolActive = false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("ACTIVE TOOL", GUILayout.Height(30)))
        {
            manager.SetToolActive();
        }
    }

    private void OnSceneGUI()
    {
        if (!manager.ToolActive) return;

        ShowToolButtonsCreate();
        ShowToolButtonsDelete();

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(worldRay, out RaycastHit hit, 1000f, manager.BlockObjectLayermask))
            {
                var item = hit.collider.GetComponent<PaintingPixelComponent>();
                if (item != null)
                {
                    if (e.control)
                    {
                        if (!manager.SelectedItems.Contains(item))
                            manager.SelectedItems.Add(item);
                    }
                    else
                    {
                        manager.SelectedItems.Clear();
                        manager.SelectedItems.Add(item);
                    }

                    Selection.objects = manager.SelectedItems.Select(i => i.gameObject).ToArray();

                    SceneView.RepaintAll();
                    e.Use();
                }
            }
            else manager.SelectedItems.Clear();
        }
    }

    private void ShowToolButtonsCreate()
    {
        if (manager.SelectedItems.Count <= 0) return;

        Handles.BeginGUI();

        float width = 355f;
        float height = 50f;
        float x = (SceneView.currentDrawingSceneView.position.width - width);  // right align
        float y = SceneView.currentDrawingSceneView.position.height - height - 80f;

        GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

        GUILayout.BeginHorizontal();

        Color color = GUI.backgroundColor;
        GUI.backgroundColor = color;

        if (manager.KeySetupModule.IsValidKeyOrientation(manager.SelectedItems))
        {
            if (GUILayout.Button(createLabels[0], GUILayout.Width(80), GUILayout.Height(30)))
            {
                manager.KeySetupModule.CreateKey(manager.SelectedItems);
            }
        }

        if (manager.SelectedItems.Count == 2)
        {
            if (manager.PipeSetupModule.IsValidPipeOrientation( manager.SelectedItems[0].PixelData, manager.SelectedItems[1].PixelData))
            {
                GUILayout.Button(createLabels[1], GUILayout.Width(80), GUILayout.Height(30));
            }
        }

        if (manager.SelectedItems.Count > 1)
        {
            if (manager.WallSetupModule.IsValidWallOrientation(manager.SelectedItems))
            {
                GUILayout.Button(createLabels[2], GUILayout.Width(80), GUILayout.Height(30));
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void ShowToolButtonsDelete()
    {
        if (manager.SelectedItems.Count <= 0) return;

        Handles.BeginGUI();

        float width = 355f;
        float height = 50f;
        float x = (SceneView.currentDrawingSceneView.position.width - width);  // right align
        float y = SceneView.currentDrawingSceneView.position.height - height - 20f;

        GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

        GUILayout.BeginHorizontal();

        Color color = GUI.backgroundColor;
        GUI.backgroundColor = color;

        if (GUILayout.Button(deleteLabels[0], GUILayout.Width(80), GUILayout.Height(30)))
        {
            manager.KeySetupModule.CreateKey(manager.SelectedItems);
        }

        if (manager.PipeSetupModule.IsValidPipeOrientation(manager.SelectedItems[0].PixelData, manager.SelectedItems[1].PixelData))
        {
            GUILayout.Button(deleteLabels[1], GUILayout.Width(80), GUILayout.Height(30));
        }

        if (manager.WallSetupModule.IsValidWallOrientation(manager.SelectedItems))
        {
            GUILayout.Button(deleteLabels[2], GUILayout.Width(80), GUILayout.Height(30));
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}