using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyObject : MonoBehaviour
{
    [Header("Wall Structure")]
    public Transform KeyTransform;                    // Head transform
    public List<PaintingPixel> PaintingPixelsCovered;

    [Header("VISUAL")]
    public Renderer KeyRenderer;

    private void Awake()
    {
        KeyTransform = transform;
    }

    /// <summary>
    /// Initialize the pipe structure with head and body parts
    /// </summary>
    /// <param name="head">The head transform</param>
    /// <param name="bodyParts">List of body parts transforms (including tail), ordered from head to tail</param>
    /// <param name="isHorizontal">True if the pipe is horizontal (in same row), false if vertical (in same column)</param>
    public void Initialize(List<PaintingPixel> pipePixels)
    {
        KeyTransform = transform;
        PaintingPixelsCovered = pipePixels ?? new List<PaintingPixel>();
    }

    public void OnAPixelDestroyed()
    {
        for (int i = 0; i < PaintingPixelsCovered.Count; i++)
        {
            PaintingPixelsCovered[i].DestroyPixel(invokeEvent: i == (PaintingPixelsCovered.Count - 1));
        }
        GameplayEventsManager.OnCollectAKey?.Invoke();
    }

    public void SelfDestroy()
    {
        if (Application.isPlaying) GameObject.Destroy(KeyTransform.gameObject);
        else GameObject.DestroyImmediate(KeyTransform.gameObject);
    }

    public void ChangeColor(Color _color)
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        KeyRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_Color", _color);
        KeyRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}