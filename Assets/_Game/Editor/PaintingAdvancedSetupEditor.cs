using System;
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

    private Plane groundPlane;

    private string pipeSetupLabel = "Pipe color";
    private int selectedColorItem = 0;
    private int lastSelectedColorItem = 0;
    private string[] PipeColors = new string[0];

    private void OnEnable()
    {
        manager = (PaintingAdvancedSetup)target;
        manager.ToolActive = false;
        groundPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));
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
            PipeColors = manager.PaintingSetupModule.ColorCodeInUse.ToArray();
        }
    }

    private void OnSceneGUI()
    {
        if (!manager.ToolActive) return;

        ShowMouseGridPosition();
        ShowToolButtonsCreate();
        ShowToolButtonsDelete();

        ShowPipeConfigLabel();
        CreatePipeMode();

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

    #region pipe
    PaintingPixel startPixel;
    PaintingPixel endPixel;
    public void CreatePipeMode()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            startPixel = new PaintingPixel();
            startPixel.row = currentRow;
            startPixel.column = currentColumn;
            e.Use();
        }

        if (e.type == EventType.MouseUp && e.button == 0 && !e.alt)
        {
            endPixel = new PaintingPixel();
            endPixel.row = currentRow;
            endPixel.column = currentColumn;

            int rowDistance = Mathf.Abs(endPixel.row - startPixel.row);
            int colDistance = Mathf.Abs(endPixel.column - startPixel.column);

            bool vertical = (rowDistance > colDistance);

            if (vertical) endPixel.column = startPixel.column;
            else endPixel.row = startPixel.row;

            e.Use();

            manager.PipeSetupModule.CreatePipe(startPixel, endPixel);
            manager.PipeSetupModule.Save();
        }
    }

    void ShowPipeConfigLabel()
    {
        Handles.BeginGUI();
        float width = 355f;
        float height = 100f;
        float x = (SceneView.currentDrawingSceneView.position.width - width);  // right align
        float y = SceneView.currentDrawingSceneView.position.height - height - 90f;

        GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

        GUILayout.Label("Pipe color:");
        if (manager.PaintingSetupModule == null) pipeSetupLabel = GUILayout.TextField(pipeSetupLabel);
        else if (PipeColors.Length > 0)
        {
            selectedColorItem = EditorGUILayout.Popup(selectedColorItem, PipeColors);
            GUILayout.Label("Current: " + PipeColors[selectedColorItem]);
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }
    #endregion
    int currentRow = 0;
    int currentColumn = 0;
    public void ShowMouseGridPosition()
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Handles.color = Color.green;

        // Tính vị trí chuột giao với mặt phẳng
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            (int col, int row, Vector3 cellPos) = manager.CurrentGrid.GetPredictedPixel(hitPoint);
            currentRow = row;
            currentColumn = col;

            Vector3 horizontalStart = new Vector3(-100, 0.35f, cellPos.z);
            Vector3 horizontalEnd = new Vector3(100, 0.35f, cellPos.z);

            Vector3 verticalStart = new Vector3(cellPos.x, 0.35f, -100);
            Vector3 verticalEnd = new Vector3(cellPos.x, 0.35f, 100);

            Handles.color = Color.green;
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.red;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleRight;

            Handles.DrawLine(horizontalStart, horizontalEnd);
            Handles.DrawLine(verticalStart, verticalEnd);

            Handles.color = Color.red;
            string labelText = $"({col}, {row})";

            Handles.Label(cellPos + Vector3.down * 0.7f, labelText, labelStyle);

            //Handles.DrawSolidDisc(hitPoint, Vector3.up, 0.02f);
        }

        // Bắt Scene view cập nhật lại mỗi frame để vẽ liên tục
        HandleUtility.Repaint();
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
        Handles.BeginGUI();

        float width = 355f;
        float height = 50f;
        float x = (SceneView.currentDrawingSceneView.position.width - width);  // right align
        float y = SceneView.currentDrawingSceneView.position.height - height - 20f;

        GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

        GUILayout.BeginHorizontal();

        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;

        if (GUILayout.Button(deleteLabels[0], GUILayout.Width(80), GUILayout.Height(30)))
        {
            manager.KeySetupModule.ClearAllKeySetups();
        }

        if (GUILayout.Button(deleteLabels[1], GUILayout.Width(80), GUILayout.Height(30)))
        {

        }

        if (manager.WallSetupModule.IsValidWallOrientation(manager.SelectedItems))
        {
            GUILayout.Button(deleteLabels[2], GUILayout.Width(80), GUILayout.Height(30));
        }

        GUI.backgroundColor = oldColor;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}