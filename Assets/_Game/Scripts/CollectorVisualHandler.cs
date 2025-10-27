using System.Collections;
using GogoGaga.OptimizedRopesAndCables;
using System.Collections.Generic;
using UnityEngine;

public class CollectorVisualHandler : MonoBehaviour
{
    public ColorPalleteData colorPalette;
    public List<Renderer> GunnerRenderers = new List<Renderer>();

    [Header("Object: ROPE")]
    public GameObject TankRopeObj;
    public Rope TankRope;
    public RopeMesh TankRopeMesh;

    [Header("Object: LOCK")]
    public SpriteRenderer LockSpriteRenderer;

    [Header("Object: HIDDEN")]
    public Material OriginalMat;
    public Material HiddenMat;

    public Color CurrentColor;

    /// <summary>
    /// Change the color of the pipe part using MaterialPropertyBlock
    /// </summary>
    /// <param name="color">The color to apply</param>
    public void SetColor(string colrCode)
    {
        if (GunnerRenderers != null)
        {
            Color color = colorPalette.GetColorByCode(colrCode);
            SetMeshColor(color);
            CurrentColor = color;
        }
    }
    private void SetMeshColor(Color _color)
    {
        // Create a MaterialPropertyBlock to set the color
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        materialPropertyBlock.SetColor("_Color", _color);

        // Apply the color to all renderers in the pipe part
        foreach (Renderer renderer in GunnerRenderers)
        {
            if (renderer != null)
            {
                renderer.material = OriginalMat;
                renderer.SetPropertyBlock(materialPropertyBlock);
            }
        }
    }
    public void SetRopeColor(Color _color)
    {
        TankRopeMesh.SetColor(_color);
    }

    public void SetupRope(bool active, CollectorVisualHandler target = null)
    {
        if (active)
        {
            TankRopeObj.SetActive(true);
            TankRope.SetEndPoint(target.TankRopeObj.transform);
            SetRopeColor(CurrentColor);
        }
        else TankRopeObj.SetActive(false);
    }

    public void RefreshColor()
    {
        if (CurrentColor != null)
        {
            SetMeshColor(CurrentColor);
        }
    }

    #region _lock
    public void SetLockedIcon(bool locked)
    {
        LockSpriteRenderer.enabled = locked;
    }
    #endregion

    #region _hidden
    public void SetHiddenState(bool hidden)
    {
        if (hidden)
        {
            foreach (Renderer renderer in GunnerRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = HiddenMat;
                }
            }
        }
        else
        {
            SetMeshColor(CurrentColor);
        }
    }
    #endregion
}