using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PipeObjectSetup
{
    public List<PaintingPixelConfig> PixelCovered; // Grid pixels that pipe lie on (sort from head to tail)
    public string ColorCode; // PipeColorCode
    public Vector3 Scale = Vector3.one;      // Scale applied to pipe parts (direct scale values)

    public PipeObjectSetup()
    {
        PixelCovered = new List<PaintingPixelConfig>();
        ColorCode = "";
        Scale = Vector3.one;
    }

    public PipeObjectSetup(List<PaintingPixelConfig> pixelCovered, string colorCode, Vector3 scale)
    {
        this.PixelCovered = pixelCovered != null ? new List<PaintingPixelConfig>(pixelCovered) : new List<PaintingPixelConfig>();
        this.ColorCode = colorCode;
        this.Scale = scale;
    }
}