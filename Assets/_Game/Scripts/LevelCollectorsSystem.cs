using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class LevelCollectorsSystem : MonoBehaviour
{
    #region PROPERTIES
    [Header("Configuration")]
    public ColorPalleteData ColorPalette; // The color source that collector will get color from by using colorCode
    [ReadOnly] public LevelColorCollectorsConfig CurrentLevelCollectorsConfig; // Current level config
    
    [Header("Formation Settings")]
    public Transform FormationCenter; // Center of the formation, formation's columns will aligns to this center
    public float SpaceBetweenColumns = 1.0f; // Space between each column
    public float SpaceBetweenCollectors = 1.0f; // Space between each collector in a column
    public GameObject CollectorPrefab; // Collector prefab to spawn
    public Transform CollectorContainer; // Collector's parent to contain collector gameobject
    public Vector3 CollectorRotation = Vector3.zero; // Rotation to apply to spawned collectors

    [Header("Runtime Data")]
    public List<ColorPixelsCollectorObject> CurrentCollectors; // Spawned collectors in current config
    public List<LockObject> CurrentLocks; // Spawned collectors in current config
    public List<CollectorColumn> CollectorColumns; // List of columns created
    
    public GameObject LockPrefab; // Collector prefab to spawn
    #endregion

    #region UNITY CORE
    private void Start()
    {
        SetupCollectorsAndMechanic();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleLeft;

        if (CurrentCollectors.Count > 0)
        {
            for (int i = 0; i < CurrentCollectors.Count; i++)
            {
                if (CurrentCollectors[i] == null) continue;
                Vector3 labelPos = CurrentCollectors[i].transform.position + CurrentCollectors[i].transform.right * 0.5f;
                //Handles.Label(labelPos, i.ToString() + $"({CurrentCollectors[i].BulletCapacity})", style);
                Handles.Label(labelPos, $"({CurrentCollectors[i].BulletCapacity})", style);
            }
        }
    }
#endif
    #endregion

    #region MAIN

    #region _initialize
    public void SetupCollectorsAndMechanic()
    {
        // Clear any existing collectors
        ClearExistingLocks();
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

        if (numberOfColumns <= 0)
        {
            Debug.LogWarning("NumberOfColumns must be greater than 0!");
            numberOfColumns = 1; // Default to 1 column if not set properly
        }

        // Initialize lists
        CurrentLocks = new List<LockObject>();
        CurrentCollectors = new List<ColorPixelsCollectorObject>();
        CollectorColumns = new List<CollectorColumn>();

        // Create collectors in row-major order (filling horizontally first)

        SetUpAndArrangePosition(numberOfColumns);

        SetupConnectedCollectors();
    }
    public void CreateLockObject()
    {
        LockObjectConfig config = new LockObjectConfig();
        // Spawn the collector with specified rotation

        Vector3 spawnPosition = FormationCenter.position;

        // Apply horizontal offset (perpendicular to forward direction) based on column
        spawnPosition += FormationCenter.right * (0 - (CurrentLevelCollectorsConfig.NumberOfColumns() - 1) / 2.0f) * SpaceBetweenColumns;

        // Apply depth offset (opposite to forward direction) based on row
        spawnPosition -= FormationCenter.forward * (CollectorColumns[0].CollectorsInColumn.Count) * SpaceBetweenCollectors;

        GameObject lockGO = Instantiate(LockPrefab, spawnPosition, Quaternion.identity, CollectorContainer);
        lockGO.transform.localEulerAngles = CollectorRotation;
        LockObject lockObj = lockGO.GetComponent<LockObject>();

        if (lockObj != null)
        {
            lockObj.ID = config.ID;
            lockObj.Row = config.Row;

            // Add to our lists
            CollectorColumns[0].CollectorsInColumn.Add(lockObj);
            CurrentLocks.Add(lockObj);
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

    public void ClearExistingLocks()
    {
        if (CurrentLocks != null)
        {
            foreach (var _lock in CurrentLocks)
            {
                if (_lock != null)
                {
                    DestroyImmediate(_lock.gameObject);
                }
            }
            CurrentLocks.Clear();
        }
    }

    public void SetupConnectedCollectors()
    {
        List<int> progressedIndex = new List<int>();
        List<ColorPixelsCollectorObject> currentGroup = new List<ColorPixelsCollectorObject>();
        for (int i = 0; i < CurrentCollectors.Count; i++)
        {
            var collector = CurrentCollectors[i];
            if (progressedIndex.Contains(i)) continue;
            if (collector.ConnectedCollectorsIDs.Count <= 0)
            {
                progressedIndex.Add(i);
                collector.VisualHandler.SetupRope(false);
                continue;
            }
            currentGroup = new List<ColorPixelsCollectorObject>();
            currentGroup.Add(collector);
            foreach (int _id in collector.ConnectedCollectorsIDs)
            {
                ColorPixelsCollectorObject connectTarget = GetCollectorByID(_id, out int _index);

                if (_index == -1) continue;
                progressedIndex.Add(_index);
                currentGroup.Add(connectTarget);
            }

            for (int j = 0; j < currentGroup.Count - 1; j++)
            {
                currentGroup[j].VisualHandler.SetupRope(true, currentGroup[j + 1].VisualHandler);
#if UNITY_EDITOR
                currentGroup[j].VisualHandler.TankRopeMesh.OnValidate();
#endif
            }
        }
    }

    public void SetUpAndArrangePosition(int collumnCount)
    {
        CollectorColumns = new List<CollectorColumn>();
        int col = 0;
        foreach (ColumnOfCollectorConfig colConfig in CurrentLevelCollectorsConfig.CollectorColumns)
        {
            int row = 0;
            var locks = colConfig.Locks;
            var collectors = colConfig.Collectors;
            CollectorColumn colObjects = new CollectorColumn();
            int totalObjectCount = collectors.Count + locks.Count;

            int lockIndex = 0;
            int collectorIndex = 0;
            for (int i = 0; i < totalObjectCount; i++)
            {

                // Calculate the position relative to the formation center (which is the highest point)
                // Use the formation center's transform to properly orient the formation
                Vector3 spawnPosition = FormationCenter.position;

                // Apply horizontal offset (perpendicular to forward direction) based on column
                spawnPosition += FormationCenter.right * (col - (collumnCount - 1) / 2.0f) * SpaceBetweenColumns;

                // Apply depth offset (opposite to forward direction) based on row
                spawnPosition -= FormationCenter.forward * row * SpaceBetweenCollectors;

                if (locks.Any(x => x.Row == i))
                {
                    LockObjectConfig config = locks[lockIndex];
                    // Spawn the collector with specified rotation
                    GameObject lockGO = Instantiate(LockPrefab, spawnPosition, Quaternion.identity, CollectorContainer);
                    lockGO.transform.localEulerAngles = CollectorRotation;
                    LockObject lockObj = lockGO.GetComponent<LockObject>();

                    if (lockObj != null)
                    {
                        lockObj.ID = config.ID;
                        lockObj.Row = config.Row;

                        // Add to our lists
                        colObjects.CollectorsInColumn.Add(lockObj);
                        CurrentLocks.Add(lockObj);
                    }
                    lockIndex++;
                }
                else
                {
                    SingleColorCollectorConfig config = collectors[collectorIndex];
                    // Spawn the collector with specified rotation
                    GameObject collectorObj = Instantiate(CollectorPrefab, spawnPosition, Quaternion.identity, CollectorContainer);
                    collectorObj.transform.localEulerAngles = CollectorRotation;
                    ColorPixelsCollectorObject collector = collectorObj.GetComponent<ColorPixelsCollectorObject>();

                    if (collector != null)
                    {
                        collector.ID = config.ID;

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
                        collector.ConnectedCollectorsIDs = new List<int>(config.ConnectedCollectorsIDs);
                        collector.IsLocked = config.Locked;
                        collector.IsHidden = config.Hidden;

                        // Set locked state (this might be handled by deactivating the collector)
                        if (false) collector.SetCollectorActive(!config.Locked);

                        collector.IsCollectorActive = false;
                        collector.ApplyHiddenState();
                        collector.ApplyLockedState();

                        // Add to our lists
                        colObjects.CollectorsInColumn.Add(collector);
                        CurrentCollectors.Add(collector);
                    }
                    collectorIndex++;
                }

                row++;
            }

            col++;
            CollectorColumns.Add(colObjects);
        }
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

    #region _events
    private void RegisterEvent()
    {
        GameplayEventsManager.OnCollectAKey += OnPlayerCollectAKey;
    }

    private void UnregisterEvent()
    {
        GameplayEventsManager.OnCollectAKey -= OnPlayerCollectAKey;
    }

    private void OnPlayerCollectAKey()
    {
        LockObject collectorToUnlock = GetFirstLockMet();
        if (collectorToUnlock != null)
        {
            collectorToUnlock.Unlock();
        }
    }
    #endregion

    #endregion

    #region SUPPORTIVE
    private LockObject GetFirstLockMet()
    {
        foreach (CollectorColumn column in CollectorColumns)
        {
            foreach (var _object in column.CollectorsInColumn)
            {
                if (_object is LockObject)
                {
                    return _object as LockObject;
                }
            }
        }

        return null;
    }

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

    public ColorPixelsCollectorObject CloneNewFromCollector(ColorPixelsCollectorObject original)
    {
        // Spawn the collector with specified rotation
        GameObject collectorObj = Instantiate(CollectorPrefab, Vector3.zero, Quaternion.identity, CollectorContainer);
        collectorObj.transform.localEulerAngles = CollectorRotation;
        ColorPixelsCollectorObject collector = collectorObj.GetComponent<ColorPixelsCollectorObject>();

        if (collector != null)
        {
            collector.ID = -1;

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
            collector.ConnectedCollectorsIDs = new List<int>(original.ConnectedCollectorsIDs);
            collector.IsLocked = original.IsLocked;
            collector.IsHidden = original.IsHidden;

            // Set locked state (this might be handled by deactivating the collector)
            if (false) collector.SetCollectorActive(!original.IsLocked);

            collector.ApplyHiddenState();
            collector.ApplyLockedState();

            CurrentCollectors.Add(collector);
        }

        return collector;
    }

    private ColorPixelsCollectorObject GetCollectorByID(int ID, out int index)
    {
        index = -1;
        for (int i = 0; i < CurrentCollectors.Count; i++)
        {
            if (CurrentCollectors[i].ID == ID)
            {
                index = i;
                return CurrentCollectors[i];
            }
        }
        return null;
    }

    #endregion
}