using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    public List<PaintingPixelConfig> GetAllWorkingPixels()
    {
        List<PaintingPixelConfig> allPixels = new List<PaintingPixelConfig>();

        // Add pixels from PaintingConfig.Pixels (convert from PaintingPixelConfig to PaintingPixel)
        foreach (var pixelConfig in Pixels)
        {
            if (pixelConfig.Hidden) continue;
            allPixels.Add(pixelConfig);
        }

        // Add pixels from PipeSetups.PixelCovered
        foreach (var pipeSetup in PipeSetups)
        {
            foreach (var pixel in pipeSetup.PixelCovered)
            {
                if (allPixels.Any(x => (x.column == pixel.column && x.row == pixel.row)))
                {
                    allPixels.Remove(allPixels.First(x => (x.column == pixel.column && x.row == pixel.row)));
                }
                allPixels.Add(pixel);
            }
        }

        // Add pixels from PipeSetups.WallSetups
        foreach (var wallSetup in WallSetups)
        {
            int pixelCoveredCount = wallSetup.PixelCovered.Count;

            foreach (PaintingPixelConfig _p in wallSetup.PixelCovered)
            {
                if (allPixels.Any(x => (x.column == _p.column && x.row == _p.row)))
                {
                    allPixels.Remove(allPixels.First(x => (x.column == _p.column && x.row == _p.row)));
                }
            }

            for (int i = 0; i < wallSetup.Hearts; i++)
            {
                PaintingPixelConfig _new = new PaintingPixelConfig(wallSetup.PixelCovered[i % (pixelCoveredCount - 1)]);
                _new.colorCode = wallSetup.ColorCode;
                allPixels.Add(_new);
            }
        }

        // Add pixels from PipeSetups.KeySetups
        foreach (var keySetup in KeySetups)
        {
            int pixelCoveredCount = keySetup.PixelCovered.Count;

            foreach (PaintingPixelConfig _p in keySetup.PixelCovered)
            {
                if (allPixels.Any(x => (x.column == _p.column && x.row == _p.row)))
                {
                    allPixels.Remove(allPixels.First(x => (x.column == _p.column && x.row == _p.row)));
                }
                allPixels.Add(_p);
            }
        }
        return allPixels;
    }
}