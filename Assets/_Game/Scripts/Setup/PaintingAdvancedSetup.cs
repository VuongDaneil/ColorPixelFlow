using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class PaintingAdvancedSetup : MonoBehaviour
{
    [ReadOnly] public PaintingConfig CurrentLevelPaintingConfig;

    [Header("MODULE(s)")]
    public KeyObjectConfigSetup KeySetupModule;
    public PipeObjectConfigSetup PipeSetupModule;
    public WallObjectConfigSetup WallSetupModule;

    [Header("TOOL(s)")]
    public bool ToolActive = false;
    public LayerMask BlockObjectLayermask;
    [ReadOnly] public List<PaintingPixelComponent> SelectedItems = new List<PaintingPixelComponent>();

    [Button("ACTIVE TOOL")]
    public void SetToolActive()
    {
        ToolActive = !ToolActive;
        if (ToolActive)
        {
            SelectedItems.Clear();
        }
    }
}
