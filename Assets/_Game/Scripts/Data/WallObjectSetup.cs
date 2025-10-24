using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WallObjectSetup
{
    public int Hearts;
    public List<PaintingPixelConfig> PixelCovered; // Grid pixels that pipe lie on (sort from head to tail)
    public string ColorCode; // PipeColorCode

    public WallObjectSetup()
    {
        PixelCovered = new List<PaintingPixelConfig>();
        ColorCode = "";
    }

    public WallObjectSetup(List<PaintingPixelConfig> pixelCovered, string colorCode, int hearts)
    {
        this.PixelCovered = pixelCovered != null ? new List<PaintingPixelConfig>(pixelCovered) : new List<PaintingPixelConfig>();
        this.ColorCode = colorCode;
    }
}