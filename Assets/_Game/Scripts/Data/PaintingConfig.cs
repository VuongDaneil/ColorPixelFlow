using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PaintingConfig", menuName = "ScriptableObjects/PaintingConfig", order = 1)]
public class PaintingConfig : ScriptableObject
{
    [SerializeField]
    private Vector2 _paintingSize;

    [SerializeField]
    private Sprite _sprite;

    [SerializeField]
    private List<PaintingPixelConfig> _pixels = new List<PaintingPixelConfig>();

    [Header("SPECIAL OBJECT: PIPE")]
    [SerializeField]
    private List<PipeObjectSetup> _pipeSetups = new List<PipeObjectSetup>();

    [Header("SPECIAL OBJECT: WALL")]
    [SerializeField]
    private List<WallObjectSetup> _wallSetups = new List<WallObjectSetup>();

    [Header("SPECIAL OBJECT: KEY")]
    [SerializeField]
    private List<KeyObjectSetup> _keySetups = new List<KeyObjectSetup>();

    public List<PaintingPixelConfig> Pixels
    {
        get { return _pixels; }
        set { _pixels = value; }
    }

    public Vector2 PaintingSize
    {
        get { return _paintingSize; }
        set { _paintingSize = value; }
    }

    public Sprite Sprite
    {
        get { return _sprite; }
        set { _sprite = value; }
    }

    public List<PipeObjectSetup> PipeSetups
    {
        get { return _pipeSetups; }
        set { _pipeSetups = value; }
    }

    public List<WallObjectSetup> WallSetups
    {
        get { return _wallSetups; }
        set { _wallSetups = value; }
    }

    public List<KeyObjectSetup> KeySetups
    {
        get { return _keySetups; }
        set { _keySetups = value; }
    }

    /// <summary>
    /// Sets the Hidden property to true for any PaintingPixelConfig in _pixels 
    /// that appears in any PipeObjectSetup's PixelCovered list based on matching row and column
    /// </summary>
    public void HidePixelsUnderPipes()
    {
        foreach (var pixelConfig in _pixels)
        {
            bool isPixelUnderPipe = false;
            
            // Check if this pixel config appears in any pipe setup
            foreach (var pipeSetup in _pipeSetups)
            {
                foreach (var coveredPixel in pipeSetup.PixelCovered)
                {
                    if (pixelConfig.row == coveredPixel.row && pixelConfig.column == coveredPixel.column)
                    {
                        isPixelUnderPipe = true;
                        break;
                    }
                }
                
                if (isPixelUnderPipe)
                    break;
            }
            
            // If the pixel is covered by a pipe, set it as hidden
            if (isPixelUnderPipe)
            {
                pixelConfig.Hidden = true;
            }
        }
    }
}