using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class WallObject : MonoBehaviour
{
    [Header("Wall Structure")]
    public Transform WallTransform;                    // Head transform
    public List<PaintingPixel> PaintingPixelsCovered;
    public int CurrentHeart = 0;

    [Header("VISUAL")]
    public Renderer WallRenderer;

    private void Awake()
    {
        WallTransform = transform;
    }

    /// <summary>
    /// Initialize the pipe structure with head and body parts
    /// </summary>
    /// <param name="head">The head transform</param>
    /// <param name="bodyParts">List of body parts transforms (including tail), ordered from head to tail</param>
    /// <param name="isHorizontal">True if the pipe is horizontal (in same row), false if vertical (in same column)</param>
    public void Initialize(List<PaintingPixel> pipePixels, int heart, Color color)
    {
        WallTransform = transform;
        PaintingPixelsCovered = pipePixels != null ? pipePixels : new List<PaintingPixel>();
        if (heart > 0) CurrentHeart = heart;
        else
        {
            foreach (var pixel in PaintingPixelsCovered)
            {
                CurrentHeart += pixel.Hearts;
            }
        }
        ChangeColor(color);
    }

    public void OnAPixelDestroyed()
    {
        CurrentHeart--;
        CurrentHeart = Mathf.Clamp(CurrentHeart, 0, int.MaxValue);

        if (CurrentHeart <= 0)
        {
            foreach (var pixel in PaintingPixelsCovered)
            {
                pixel.DestroyPixel(invokeEvent: false);
            }
            gameObject.SetActive(false);
            return;
        }

        foreach (var pixel in PaintingPixelsCovered)
        {
            pixel.destroyed = false;
        }
    }

    public void SelfDestroy()
    {
        if (Application.isPlaying) GameObject.Destroy(WallTransform.gameObject);
        else GameObject.DestroyImmediate(WallTransform.gameObject);
    }

    public void ChangeColor(Color _color)
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        WallRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_Color", _color);
        WallRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
