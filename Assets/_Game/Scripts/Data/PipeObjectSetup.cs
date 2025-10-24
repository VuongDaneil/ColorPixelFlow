using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PipeObjectSetup
{
    public List<PaintingPixel> PixelCovered; // Grid pixels that pipe lie on (sort from head to tail)
    public string ColorCode; // PipeColorCode
    public Vector3 Scale = Vector3.one;      // Scale applied to pipe parts (direct scale values)

    public PipeObjectSetup()
    {
        PixelCovered = new List<PaintingPixel>();
        ColorCode = "";
        Scale = Vector3.one;
    }

    public PipeObjectSetup(List<PaintingPixel> pixelCovered, string colorCode, Vector3 scale)
    {
        this.PixelCovered = pixelCovered != null ? new List<PaintingPixel>(pixelCovered) : new List<PaintingPixel>();
        this.ColorCode = colorCode;
        this.Scale = scale;
    }
}