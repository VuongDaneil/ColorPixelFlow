using UnityEngine;

[System.Serializable]
public class PaintingPixelConfig
{
    public int column;
    public int row;
    public Color color;
    public string colorCode;
    public bool Hidden;

    public PaintingPixelConfig() { }

    public PaintingPixelConfig(PaintingPixel _pixelObj)
    {
        column = _pixelObj.column;
        row = _pixelObj.row;
        color = _pixelObj.color;
        colorCode = _pixelObj.colorCode;
        Hidden = _pixelObj.Hidden;
    }
}