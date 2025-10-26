using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Playables;

public class LevelCollectorsSystem : MonoBehaviour
{
    [Header("Configuration")]
    public ColorPalleteData ColorPalette; // The color source that collector will get color from by using colorCode
    public LevelColorCollectorsConfig CurrentLevelCollectorsConfig; // Current level config
    
    [Header("Formation Settings")]
    public Transform FormationCenter; // Center of the formation, formation's columns will aligns to this center
    public float SpaceBetweenColumns = 1.0f; // Space between each column
    public float SpaceBetweenCollectors = 1.0f; // Space between each collector in a column
    public GameObject CollectorPrefab; // Collector prefab to spawn
    public Transform CollectorContainer; // Collector's parent to contain collector gameobject
    public Vector3 CollectorRotation = Vector3.zero; // Rotation to apply to spawned collectors

    [Header("Runtime Data")]
    public List<ColorPixelsCollectorObject> CurrentCollectors; // Spawned collectors in current config
    public List<CollectorColumn> CollectorColumns; // List of columns created

    private void Start()
    {
        SetupCollectors();
    }

    public void SetupCollectors()
    {
        // Clear any existing collectors
        ClearExistingCollectors();
        
        if (CurrentLevelCollectorsConfig == null)
        {
            Debug.LogWarning("No LevelColorCollectorsConfig assigned!");
            return;
        }

        if (CollectorPrefab == null)
        {
            Debug.LogError("CollectorPrefab is not assigned!");
            return;
        }

        if (CurrentLevelCollectorsConfig.CollectorColumns == null || CurrentLevelCollectorsConfig.CollectorColumns.Count == 0)
        {
            Debug.LogWarning("No collector setups found in the config!");
            return;
        }

        int numberOfColumns = CurrentLevelCollectorsConfig.NumberOfColumns();
        int totalCollectors = CurrentLevelCollectorsConfig.NumberOfCollectors();

        if (numberOfColumns <= 0)
        {
            Debug.LogWarning("NumberOfColumns must be greater than 0!");
            numberOfColumns = 1; // Default to 1 column if not set properly
        }

        // Initialize lists
        CurrentCollectors = new List<ColorPixelsCollectorObject>();
        CollectorColumns = new List<CollectorColumn>();

        // Create collectors in row-major order (filling horizontally first)

        SetUpAndArrangePosition(numberOfColumns);

        SetupConnectedCollectors();
    }

    public void ClearExistingCollectors()
    {
        if (CurrentCollectors != null)
        {
            foreach (var collector in CurrentCollectors)
            {
                if (collector != null)
                {
                    DestroyImmediate(collector.gameObject);
                }
            }
            CurrentCollectors.Clear();
        }
        
        if (CollectorColumns != null)
        {
            CollectorColumns.Clear();
        }
    }

    public void SetupConnectedCollectors()
    {
        List<int> progressedIndex = new List<int>();
        for (int i = 0; i < CurrentCollectors.Count; i++)
        {
            var collector = CurrentCollectors[i];
            if (collector.ConnectedCollectorsIndex.Count <= 0 || progressedIndex.Contains(i)) 
            {
                progressedIndex.Add(i);
                collector.VisualHandler.SetupRope(false);
                continue;
            }
            foreach(int index in collector.ConnectedCollectorsIndex)
            {
                progressedIndex.Add(index);
                collector.VisualHandler.SetupRope(true, CurrentCollectors[index].VisualHandler);
#if UNITY_EDITOR
                collector.VisualHandler.TankRopeMesh.OnValidate();
#endif
            }
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        //style.fontStyle = FontStyle.Bold;

        if (CurrentCollectors.Count > 0)
        {
            for (int i = 0; i < CurrentCollectors.Count; i++)
            {
                if (CurrentCollectors[i] == null) continue;
                Vector3 labelPos = CurrentCollectors[i].transform.position + CurrentCollectors[i].transform.up * 1.5f;
                Handles.Label(labelPos, i.ToString() + $"({CurrentCollectors[i].BulletCapacity})", style);
            }
        }
    }
#endif

    #region SUPPORTIVE
    
    #region _tool modules
    public void RemoveCollector(ColorPixelsCollectorObject target)
    {
        CurrentCollectors.Remove(target);
        foreach (var colObjects in CollectorColumns)
        {
            if (colObjects.CollectorsInColumn.Contains(target))
            {
                colObjects.CollectorsInColumn.Remove(target);
                break;
            }
        }
        DestroyImmediate(target.gameObject);
        ReArrangePosition();
        SetupConnectedCollectors();
    }

    public void SetUpAndArrangePosition(int collumnCount)
    {
        CollectorColumns = new List<CollectorColumn>();
        int col = 0;
        foreach (ColumnOfCollectorConfig colConfig in CurrentLevelCollectorsConfig.CollectorColumns)
        {
            int row = 0;
            var collectors = colConfig.Collectors;
            CollectorColumn colObjects = new CollectorColumn();
            for (int i = 0; i < collectors.Count; i++)
            {
                SingleColorCollectorConfig config = collectors[i];

                // Calculate the position relative to the formation center (which is the highest point)
                // Use the formation center's transform to properly orient the formation
                Vector3 spawnPosition = FormationCenter.position;

                // Apply horizontal offset (perpendicular to forward direction) based on column
                spawnPosition += FormationCenter.right * (col - (collumnCount - 1) / 2.0f) * SpaceBetweenColumns;

                // Apply depth offset (opposite to forward direction) based on row
                spawnPosition -= FormationCenter.forward * row * SpaceBetweenCollectors;

                // Spawn the collector with specified rotation
                GameObject collectorObj = Instantiate(CollectorPrefab, spawnPosition, Quaternion.identity, CollectorContainer);
                collectorObj.transform.localEulerAngles = CollectorRotation;
                ColorPixelsCollectorObject collector = collectorObj.GetComponent<ColorPixelsCollectorObject>();

                if (collector != null)
                {
                    // Find color from palette based on ColorCode
                    if (ColorPalette != null && ColorPalette.colorPallete.ContainsKey(config.ColorCode))
                    {
                        // Set the collector's color and shooting color
                        collector.CollectorColor = config.ColorCode;
                        collector.VisualHandler.SetColor(config.ColorCode);
                    }

                    // Apply bullet settings
                    collector.BulletCapacity = config.Bullets;
                    collector.BulletLeft = config.Bullets;
                    collector.ConnectedCollectorsIndex = new List<int>(config.ConnectedCollectorsIndex);
                    collector.IsLocked = config.Locked;
                    collector.IsHidden = config.Hidden;

                    // Set locked state (this might be handled by deactivating the collector)
                    if (false) collector.SetCollectorActive(!config.Locked);

                    // Add to our lists
                    colObjects.CollectorsInColumn.Add(collector);
                    CurrentCollectors.Add(collector);
                }

                row++;
            }

            col++;
            CollectorColumns.Add(colObjects);
        }
    }

    public ColorPixelsCollectorObject CloneNewFromCollector(ColorPixelsCollectorObject original)
    {
        // Spawn the collector with specified rotation
        GameObject collectorObj = Instantiate(CollectorPrefab, Vector3.zero, Quaternion.identity, CollectorContainer);
        collectorObj.transform.localEulerAngles = CollectorRotation;
        ColorPixelsCollectorObject collector = collectorObj.GetComponent<ColorPixelsCollectorObject>();

        if (collector != null)
        {
            // Find color from palette based on ColorCode
            if (ColorPalette != null && ColorPalette.colorPallete.ContainsKey(original.CollectorColor))
            {
                // Set the collector's color and shooting color
                collector.CollectorColor = original.CollectorColor;
                collector.VisualHandler.SetColor(original.CollectorColor);
            }

            // Apply bullet settings
            collector.BulletCapacity = original.BulletCapacity;
            collector.BulletLeft = original.BulletCapacity;
            collector.ConnectedCollectorsIndex = new List<int>(original.ConnectedCollectorsIndex);
            collector.IsLocked = original.IsLocked;
            collector.IsHidden = original.IsHidden;

            // Set locked state (this might be handled by deactivating the collector)
            if (false) collector.SetCollectorActive(!original.IsLocked);

            CurrentCollectors.Add(collector);
        }

        return collector;
    }

    public void ReArrangePosition()
    {
        int col = 0;
        foreach (CollectorColumn column in CollectorColumns)
        {
            int row = 0;
            int columnCount = CollectorColumns.Count;
            var collectors = column.CollectorsInColumn;
            for (int i = 0; i < collectors.Count; i++)
            {
                // Calculate the position relative to the formation center (which is the highest point)
                // Use the formation center's transform to properly orient the formation
                Vector3 spawnPosition = FormationCenter.position;

                // Apply horizontal offset (perpendicular to forward direction) based on column
                spawnPosition += (col - (columnCount - 1) / 2.0f) * SpaceBetweenColumns * FormationCenter.right;

                // Apply depth offset (opposite to forward direction) based on row
                spawnPosition -= row * SpaceBetweenCollectors * FormationCenter.forward;

                // Spawn the collector with specified rotation
                collectors[i].transform.position = spawnPosition;
                collectors[i].transform.localEulerAngles = CollectorRotation;
                row++;
            }
            col++;
        }
    }
    #endregion

    #endregion
}