using UnityEngine;

public class PaintingPixelComponent : MonoBehaviour
{
    public PaintingPixel PixelData;
    public MeshRenderer CubeRenderer;
    public int CurrentHearts = 0;
    public void SetUp(PaintingPixel newPixel)
    {
        newPixel.PixelComponent = this;
        PixelData = newPixel;
        CurrentHearts = PixelData.Hearts;
    }

    // Update the visual representation based on the pixel data
    public void ApplyVisual()
    {
        if (PixelData != null)
        {
            CubeRenderer.enabled = !PixelData.Hidden;
            // Apply color using MaterialPropertyBlock
            if (CubeRenderer != null)
            {
                SetColor(PixelData.color);
            }
        }
    }

    // Get world position of this pixel
    public Vector3 GetWorldPosition()
    {
        if (PixelData != null)
        {
            return PixelData.worldPos;
        }
        return transform.position;
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        CubeRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_Color", color);
        CubeRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    // Check if this pixel is destroyed
    public bool IsDestroyed()
    {
        return PixelData != null && PixelData.destroyed;
    }

    // Destroy this pixel
    public void Destroyed()
    {
        PixelData.destroyed = true;
    }

    public void ApplyPosition()
    {
        transform.localPosition = PixelData.worldPos;
    }

    public void ShowVisualOnly()
    {
        CubeRenderer.enabled = true;
    }
    public void HideVisualOnly()
    {
        CubeRenderer.enabled = false;
    }
}