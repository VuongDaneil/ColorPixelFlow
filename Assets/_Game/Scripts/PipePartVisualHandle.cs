using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipePartVisualHandle : MonoBehaviour
{
    public List<Renderer > pipeRenderers = new List<Renderer>();
    
    /// <summary>
    /// Change the color of the pipe part using MaterialPropertyBlock
    /// </summary>
    /// <param name="color">The color to apply</param>
    public void SetColor(Color color)
    {
        if (pipeRenderers != null)
        {
            // Create a MaterialPropertyBlock to set the color
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetColor("_Color", color);
            
            // Apply the color to all renderers in the pipe part
            foreach (Renderer renderer in pipeRenderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }
    }
}
