using System;
using UnityEngine;

[Serializable]
public class PaintingPixel
{
    public string name;
    public int column;
    public int row;
    public Color color;
    public string colorCode;
    public Vector3 worldPos;
    public int Hearts; // Number of hits the pixel can take before being destroyed
    public bool destroyed;
    public GameObject pixelObject; // Reference to the GameObject associated with this pixel
    public PaintingPixelComponent PixelComponent;
    public bool Hidden;

    public PaintingPixel()
    {
        this.name = "PaintingPixel";
        this.column = 0;
        this.row = 0;
        this.color = Color.white;
        this.colorCode = "WhiteDefault";
        this.worldPos = Vector3.zero;
        this.Hearts = 1;
        this.destroyed = false;
        this.pixelObject = null;
        this.Hidden = false;
    }

    public PaintingPixel(int column, int row, Color color, Vector3 worldPos, int heart, bool hidden, GameObject pixelObject = null)
    {
        this.name = $"Pixel ({column}, {row})";
        this.column = column;
        this.row = row;
        this.color = color;
        this.colorCode = "WhiteDefault";
        this.worldPos = worldPos;
        this.Hearts = heart;
        this.destroyed = false;
        this.pixelObject = pixelObject;
        this.Hidden = hidden;
    }

    public PaintingPixel(PaintingPixelConfig config)
    {
        this.name = $"Pixel ({config.column}, {config.row})";
        this.column = config.column;
        this.row = config.row;
        this.color = config.color;
        this.colorCode = config.colorCode;
        this.worldPos = Vector3.zero;
        this.Hearts = 1;
        this.destroyed = false;
        this.pixelObject = null;
        this.Hidden = config.Hidden;
    }

    public void SetUp(Color color, string colorCode, bool hidden)
    {
        this.color = color;
        this.colorCode = colorCode;
        this.Hidden = hidden;

        PixelComponent?.SetUp(this);

        if (Hidden)
        {
            destroyed = true;
            pixelObject.SetActive(false);
        }
        else PixelComponent?.ApplyVisual();
    }

    public void SetPosition(Vector3 newPos)
    {
        this.worldPos = newPos;
    }

    public void DestroyPixel(bool invokeEvent = true)
    {
        //Todo: deal dmg through PixelComponent
        this.destroyed = true;
        this.PixelComponent.Destroyed();
        if (pixelObject != null)
        {
            pixelObject.SetActive(false);
        }
        if (invokeEvent) GameplayEventsManager.OnAPixelDestroyed?.Invoke(this);
    }

    public void DestroyObject()
    {
        this.destroyed = true;
        if (pixelObject != null)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(pixelObject);
            }
            else
            {
                GameObject.DestroyImmediate(pixelObject);
            }
        }
    }

    public void ShowPixelObject()
    {
        pixelObject?.SetActive(true);
        PixelComponent.ShowVisualOnly();
    }
}