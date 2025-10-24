using UnityEngine;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using DG.Tweening;
using NaughtyAttributes;

public class PipeObject : MonoBehaviour
{
    [Header("Pipe Structure")]
    public Transform PipeHead;                    // Head transform
    public List<Transform> PipeBodyParts;         // All pipe body parts including tail sorted from head to tail
    public List<PaintingPixel> PaintingPixelsCovered;
    [ReadOnly] public int pixelDestroyed = 0;
    Transform parentTransform;
    
    [Header("Pipe Properties")]
    public bool IsHorizontal;                     // True if pipe is horizontal (in same row), false if vertical (in same column)

    private void Awake()
    {
        if (PipeBodyParts == null)
            PipeBodyParts = new List<Transform>();
    }

    /// <summary>
    /// Initialize the pipe structure with head and body parts
    /// </summary>
    /// <param name="head">The head transform</param>
    /// <param name="bodyParts">List of body parts transforms (including tail), ordered from head to tail</param>
    /// <param name="isHorizontal">True if the pipe is horizontal (in same row), false if vertical (in same column)</param>
    public void Initialize(Transform head, List<Transform> bodyParts, List<PaintingPixel> pipePixels, bool isHorizontal = false)
    {
        PipeHead = head;
        PipeBodyParts = bodyParts != null ? new List<Transform>(bodyParts) : new List<Transform>();
        PaintingPixelsCovered = pipePixels != null ? pipePixels : new List<PaintingPixel>();
        IsHorizontal = isHorizontal;
        parentTransform = transform.parent;
    }

    /// <summary>
    /// Add a body part to the pipe
    /// </summary>
    /// <param name="bodyPart">The body part transform to add</param>
    public void AddBodyPart(Transform bodyPart)
    {
        if (bodyPart != null && !PipeBodyParts.Contains(bodyPart))
        {
            PipeBodyParts.Add(bodyPart);
        }
    }

    /// <summary>
    /// Remove a body part from the pipe
    /// </summary>
    /// <param name="bodyPart">The body part transform to remove</param>
    public void RemoveBodyPart(Transform bodyPart)
    {
        if (bodyPart != null)
        {
            PipeBodyParts.Remove(bodyPart);
        }
    }

    /// <summary>
    /// Get the total number of parts in the pipe (head + body parts)
    /// </summary>
    /// <returns>Count of all parts in the pipe</returns>
    public int GetTotalPartsCount()
    {
        return 1 + PipeBodyParts.Count; // 1 for head + number of body parts
    }

    /// <summary>
    /// Get all parts of the pipe (head + body parts)
    /// </summary>
    /// <returns>List containing head and all body parts in order</returns>
    public List<Transform> GetAllParts()
    {
        List<Transform> allParts = new List<Transform>();
        if (PipeHead != null)
            allParts.Add(PipeHead);
        
        allParts.AddRange(PipeBodyParts);
        
        return allParts;
    }
    
    /// <summary>
    /// Rotates all pipe parts based on orientation
    /// </summary>
    public void ApplyOrientationRotation()
    {
        var pipeHead = PaintingPixelsCovered[0];
        var pipeTail = PaintingPixelsCovered[^1];
        if (IsHorizontal)
        {
            // Rotate 90 degrees on Y axis for horizontal pipes
            bool leftToRight = pipeHead.column < pipeTail.column;
            float rotateHead = leftToRight ? -90f : 90f;
            if (PipeHead != null) PipeHead.localEulerAngles = new Vector3(0, rotateHead, 0);
                
            foreach (Transform bodyPart in PipeBodyParts)
            {
                if (bodyPart != null && bodyPart != PipeHead) bodyPart.Rotate(Vector3.up, 90f, Space.Self);
            }
        }
        else
        {
            bool bottomToTop = pipeHead.row < pipeTail.row;
            float rotateHead = bottomToTop ? 180f : 0;
            if (PipeHead != null) PipeHead.Rotate(Vector3.up, rotateHead, Space.Self);
        }
    }

    public void OnAPixelDestroyed()
    {
        if (pixelDestroyed >= PaintingPixelsCovered.Count) return; 
        PaintingPixelsCovered[^(1 + pixelDestroyed)].DestroyPixel(false);
        for (int i = 0; i < PaintingPixelsCovered.Count - pixelDestroyed; i++)
        {
            PaintingPixelsCovered[0].destroyed = false;
        }

        pixelDestroyed++;
        HandlePipeShortening();
    }

    /// <summary>
    /// Handles the shortening animation when a PaintingPixel in the pipe is destroyed
    /// Moves all parts after the destroyed pixel toward the head, removing parts that reach the head position
    /// </summary>
    /// <param name="destroyedPixel">The pixel that was destroyed and triggered the shortening</param>
    [Button("Test")]
    public void HandlePipeShortening()
    {
        pixelDestroyed++;
        if (pixelDestroyed >= PipeBodyParts.Count)
        {
            parentTransform.DOScale(0, 0.5f);
            return;
        }
        for (int i = 1; i < PipeBodyParts.Count; i++)
        {
            int perviousPartIndex = Mathf.Max(i - pixelDestroyed, 0);
            if (!Application.isPlaying) PipeBodyParts[i].position = PaintingPixelsCovered[perviousPartIndex].worldPos;
            else
            {
                DOTween.Kill(PipeBodyParts[i]);
                Transform tmp = PipeBodyParts[i];
                tmp.DOLocalMove(PaintingPixelsCovered[perviousPartIndex].worldPos, 0.1f);
            }
        }
    }

    public void SelfDestroy()
    {
        if (parentTransform == null) parentTransform = transform.parent;
        foreach (var pixel in PaintingPixelsCovered)
        {
            if (Application.isPlaying) GameObject.Destroy(pixel.PixelComponent?.gameObject);
            else GameObject.DestroyImmediate(pixel.PixelComponent?.gameObject);
        }
        if (Application.isPlaying) GameObject.Destroy(parentTransform.gameObject);
        else GameObject.DestroyImmediate(parentTransform.gameObject);
    }
}