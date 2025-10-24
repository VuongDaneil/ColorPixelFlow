using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

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
    public List<ColorPixelsCollector> CurrentCollectors; // Spawned collectors in current config
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

        if (CurrentLevelCollectorsConfig.CollectorSetups == null || CurrentLevelCollectorsConfig.CollectorSetups.Count == 0)
        {
            Debug.LogWarning("No collector setups found in the config!");
            return;
        }

        int totalCollectors = CurrentLevelCollectorsConfig.CollectorSetups.Count;
        int numberOfColumns = CurrentLevelCollectorsConfig.NumberOfColumns;

        if (numberOfColumns <= 0)
        {
            Debug.LogWarning("NumberOfColumns must be greater than 0!");
            numberOfColumns = 1; // Default to 1 column if not set properly
        }

        // Initialize lists
        CurrentCollectors = new List<ColorPixelsCollector>();
        CollectorColumns = new List<CollectorColumn>();

        // Calculate rows needed based on number of collectors and columns
        int numberOfRows = Mathf.CeilToInt((float)totalCollectors / numberOfColumns);

        // Create collectors in row-major order (filling horizontally first)
        for (int i = 0; i < totalCollectors; i++)
        {
            SingleColorCollectorObject config = CurrentLevelCollectorsConfig.CollectorSetups[i];

            // Calculate position based on row-major order
            int row = i / numberOfColumns; // Row index
            int col = i % numberOfColumns; // Column index

            // Calculate the position relative to the formation center (which is the highest point)
            // Use the formation center's transform to properly orient the formation
            Vector3 spawnPosition = FormationCenter.position;
            
            // Apply horizontal offset (perpendicular to forward direction) based on column
            spawnPosition += FormationCenter.right * (col - (numberOfColumns - 1) / 2.0f) * SpaceBetweenColumns;
            
            // Apply depth offset (opposite to forward direction) based on row
            spawnPosition -= FormationCenter.forward * row * SpaceBetweenCollectors;

            // Spawn the collector with specified rotation
            GameObject collectorObj = Instantiate(CollectorPrefab, spawnPosition, Quaternion.identity, CollectorContainer);
            collectorObj.transform.localEulerAngles = CollectorRotation;
            ColorPixelsCollector collector = collectorObj.GetComponent<ColorPixelsCollector>();

            if (collector != null)
            {
                // Assign the configuration data to the collector
                collector.OriginalIndex = config.OriginalIndex;
                
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
                CurrentCollectors.Add(collector);
            }
        }

        // Organize collectors into columns as specified in the design
        OrganizeCollectorsIntoColumns(totalCollectors, numberOfColumns);

        SetupConnectedCollectors();
    }

    private void OrganizeCollectorsIntoColumns(int totalCollectors, int numberOfColumns)
    {
        // Create column containers
        for (int colIdx = 0; colIdx < numberOfColumns; colIdx++)
        {
            CollectorColumn column = new CollectorColumn();
            CollectorColumns.Add(column);

            // Add collectors to this column (every nth collector where n is the number of columns)
            // In row-major order, collectors in the same column are at indices: colIdx, colIdx+numberOfColumns, colIdx+2*numberOfColumns, etc.
            for (int idx = colIdx; idx < totalCollectors; idx += numberOfColumns)
            {
                if (idx < CurrentCollectors.Count)
                {
                    column.CollectorsInColumn.Add(CurrentCollectors[idx]);
                }
            }
        }
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
                Vector3 labelPos = CurrentCollectors[i].transform.position + CurrentCollectors[i].transform.up * 1.5f;
                Handles.Label(labelPos, i.ToString() + $"({CurrentCollectors[i].BulletCapacity})", style);
            }
        }
    }
#endif
}