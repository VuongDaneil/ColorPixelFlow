using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(LevelCollectorsConfigSetup))]
public class LevelCollectorsConfigSetupEditor : Editor
{
    private string newConfigName = "CollectorsConfig";
    private string newConfigPath = "Assets/_Game/Data/GunnerConfig/";

    LevelCollectorsConfigSetup manager;
    public ColorPixelsCollector selectedItem;
    private static int selectedMode = 0;
    private static readonly string[] labels = { "Move", "Rotate", "Scale" };

    private void OnEnable()
    {
        manager = (LevelCollectorsConfigSetup)target;
        manager.ToolActive = false;
    }

    private void OnDisable()
    {
        manager.ToolActive = false;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        manager = (LevelCollectorsConfigSetup)target;

        #region _inspector

        if (GUILayout.Button("Generate Default Config"))
        {
            manager.GenerateDefaultConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Config Asset:", EditorStyles.boldLabel);
        newConfigPath = EditorGUILayout.TextField("Path", newConfigPath);

        if (GUILayout.Button("Create Config Asset"))
        {
            if (manager != null)
            {
                if (manager.paintingConfig != null) newConfigName = manager.paintingConfig.name.Replace("_PaintingConfig", "") + "_CollectorsConfig";
                LevelColorCollectorsConfig newConfig = manager.CreateConfigAsset(newConfigName, newConfigPath);
                if (newConfig != null)
                {
                    manager.configAsset = newConfig;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load & Preview Config:", EditorStyles.boldLabel);
        if (GUILayout.Button("Load & Preview Config"))
        {
            if (manager.configAsset != null)
            {
                manager.LoadConfigAsset(manager.configAsset);
            }
            else
            {
                Debug.LogWarning("Please assign a config asset to load!");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import Config:", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh"))
        {
            foreach (var collector in manager.previewSystem.CurrentCollectors)
            {
                collector.VisualHandler.SetColor(collector.CollectorColor);
            }
        }
        if (GUILayout.Button("Import from Scene"))
        {
            if (EditorUtility.DisplayDialog("Confirm Generation",
                    "This will replace all existing collector setups in the config asset with new ones based on the one in scene. Are you sure?",
                    "Yes", "Cancel"))
            {
                if (manager.previewSystem != null && manager.configAsset != null)
                {
                    manager.ImportCollectorsFromScene();
                }
                else
                {
                    Debug.LogWarning("Please assign both source system and target config asset!");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connections:", EditorStyles.boldLabel);
        if (GUILayout.Button("Ensure Bidirectional Connections"))
        {
            manager.EnsureBidirectionalConnections();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import from PaintingConfig:", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate from PaintingConfig"))
        {
            if (manager.paintingConfig != null && manager.configAsset != null)
            {
                if (EditorUtility.DisplayDialog("Confirm Generation",
                    "This will replace all existing collector setups in the config asset with new ones based on the painting config. Are you sure?",
                    "Yes", "Cancel"))
                {
                    manager.GenerateCollectorsFromPaintingConfig();
                }
            }
            else
            {
                Debug.LogWarning("Please assign both paintingConfig and configAsset!");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Clear Setup:", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Setup in Scene"))
        {
            manager.previewSystem.ClearExistingCollectors();
        }
        #endregion

        #region _scene kits
        GUILayout.Space(10);
        GUILayout.Label("TOOL", EditorStyles.foldoutHeader);
        if (GUILayout.Button("ACTIVE TOOL"))
        {
            manager.StartUpTool();
        }
        GUILayout.Label("Scene Interaction", EditorStyles.boldLabel);
        GUILayout.Label(selectedItem ? $"Selected: {selectedItem.name}" : "Click an item in Scene");
        #endregion
    }

    private void OnSceneGUI()
    {
        if (!manager.ToolActive) return;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(worldRay, out RaycastHit hit, 1000f, manager.CollectorObjectLayerMask))
            {
                var item = hit.collider.GetComponent<ColorPixelsCollector>();
                if (item != null)
                {
                    selectedItem = item;
                    Selection.activeGameObject = item.gameObject;
                    SceneView.RepaintAll();
                    //e.Use(); // chặn click này không bị SceneView dùng
                }
            }
        }

        if (selectedItem != null)
        {
            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(selectedItem.transform.position, selectedItem.transform.rotation);

            Undo.RecordObject(selectedItem.transform, "Move Item");
            selectedItem.transform.position = newPos;

            CheckCollisions(selectedItem);

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(selectedItem.transform.position, Vector3.up, manager.CollisionRadius);
            //EditorGUI.EndChangeCheck();
        }

        ShowToggles();
    }

    private void CheckCollisions(ColorPixelsCollector current)
    {
        foreach (var other in manager.previewSystem.CurrentCollectors)
        {
            if (other == null || other == current) continue;

            float distance = Vector3.Distance(current.transform.position, other.transform.position);
            float minDist = manager.CollisionRadius * 2;

            if (distance < minDist)
            {
                Handles.color = Color.red;
                Handles.DrawLine(current.transform.position, other.transform.position);

                Debug.Log($"Collision between {current.name} and {other.name}");

                if (current.TryGetComponent<Renderer>(out var rend))
                    rend.sharedMaterial.color = Color.red;
            }
        }
    }

    private void ShowToggles()
    {
        Handles.BeginGUI();

        // Lấy kích thước SceneView
        float width = 300f; // chiều rộng khung chứa 3 toggle
        float height = 50f;
        float x = (sceneView.position.width - width) * 0.5f;  // canh giữa
        float y = sceneView.position.height - height - 20f;   // cách đáy 20px

        GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

        GUILayout.BeginHorizontal();

        for (int i = 0; i < labels.Length; i++)
        {
            bool newValue = GUILayout.Toggle(selectedMode == i, labels[i], "Button", GUILayout.Height(30));
            if (newValue && selectedMode != i)
            {
                selectedMode = i;
                Debug.Log($"Selected Mode: {labels[i]}");
                SceneView.RepaintAll();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Handles.EndGUI();
    }
}
