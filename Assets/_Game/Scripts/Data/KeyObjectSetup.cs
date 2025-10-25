using System.Collections.Generic;
using System;

[Serializable]
public class KeyObjectSetup
{
    public List<PaintingPixelConfig> PixelCovered; // Grid pixels that pipe lie on (sort from head to tail)
    public readonly string ColorCode = "KeyColor";

    public KeyObjectSetup()
    {
        PixelCovered = new List<PaintingPixelConfig>();
        ColorCode = PaintingSharedAttributes.KeyColorDefine;
    }

    public KeyObjectSetup(List<PaintingPixelConfig> pixelCovered)
    {
        this.PixelCovered = pixelCovered != null ? new List<PaintingPixelConfig>(pixelCovered) : new List<PaintingPixelConfig>();
        this.ColorCode = PaintingSharedAttributes.KeyColorDefine;
    }
}