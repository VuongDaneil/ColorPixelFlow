using static PaintingSharedAttributes;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class PaintingConfigSetup : MonoBehaviour
{
    [Header("Input Settings")]
    public Sprite TargetPainting;
    public PaintingGridObject targetGrid;
    public ColorPalleteData colorPalette;
    
    [Header("Color Filter Settings")]
    public bool useColorFilter = false;
    public List<string> colorCodeInUse = new List<string>();

    [Header("Result")]
    [ReadOnly] public PaintingConfig ResultPaintingConfig;

    public void SamplePaintingToGrid(Sprite _sprite = null)
    {
        if (_sprite == null) _sprite = TargetPainting;
        if (_sprite == null)
        {
            Debug.LogError("TargetPainting sprite is not assigned!");
            return;
        }

        if (targetGrid == null)
        {
            Debug.LogError("TargetGrid is not assigned!");
            return;
        }

        if (colorPalette == null)
        {
            Debug.LogError("ColorPalette is not assigned!");
            return;
        }

        // Get grid size from target grid
        Vector2 gridSize = targetGrid.gridSize;
        int gridWidth = (int)gridSize.x;
        int gridHeight = (int)gridSize.y;

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("Invalid grid size!");
            return;
        }

        // Get the texture from the sprite
        Texture2D paintingTexture = GetTextureFromSprite(_sprite);
        if (paintingTexture == null)
        {
            Debug.LogError("Could not get texture from sprite!");
            return;
        }

        // Sample colors from painting based on grid dimensions
        List<PaintingPixelConfig> pixels = new List<PaintingPixelConfig>();
        
        // Calculate half dimensions for coordinate transformation
        int halfWidth = (int)(gridWidth / 2);
        int halfHeight = (int)(gridHeight / 2);
        
        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                // Calculate the center position of the grid cell in the painting texture
                float texX = (col + 0.5f) * paintingTexture.width / gridWidth;
                float texY = (row + 0.5f) * paintingTexture.height / gridHeight;

                // Get the color at the center of the grid cell
                Color sampledColor = paintingTexture.GetPixel((int)texX, (int)texY);

                // Find the closest color and color code in the palette
                var (closestColor, colorCode) = FindClosestColorInPalette(sampledColor, colorPalette);

                // Transform coordinates to match GridGenerator system (center at 0,0 with negative indices)
                // For columns: from right (positive) to left (negative)
                int gridCol = halfWidth - col;
                if (gridWidth % 2 == 0) {
                    gridCol -= 1; // Adjust for even number of columns
                }

                // For rows: from bottom (negative) to top (positive) 
                int gridRow = row - halfHeight;
                if (gridHeight % 2 == 0) {
                    // No adjustment needed for even number of rows in this implementation
                }

                // Create a new pixel config
                PaintingPixelConfig pixelConfig = new PaintingPixelConfig();
                pixelConfig.row = gridRow;
                pixelConfig.column = gridCol;
                pixelConfig.color = closestColor;
                pixelConfig.colorCode = colorCode;
                pixelConfig.Hidden = colorCode.Equals(TransparentColorKey);

                pixels.Add(pixelConfig);
            }
        }

        // Create and configure the PaintingConfig asset
        CreatePaintingConfigAsset(pixels, gridSize, _sprite);
    }

    private Texture2D GetTextureFromSprite(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        // If the sprite texture is readable, we can use it directly
        if (sprite.texture.isReadable)
        {
            return sprite.texture;
        }
        else
        {
            // If not readable, we need to create a readable copy
            Texture2D readableTexture = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                sprite.texture.width, 
                sprite.texture.height, 
                0, 
                RenderTextureFormat.Default, 
                RenderTextureReadWrite.Linear);

            // Set the sprite texture to the render texture
            Graphics.Blit(sprite.texture, renderTexture);

            // Store the previous render texture
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            // Copy the pixels from the render texture to the readable texture
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();

            // Restore the previous render texture
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            return readableTexture;
        }
    }

    private (Color color, string colorCode) FindClosestColorInPalette(Color targetColor, ColorPalleteData palette)
    {
        if (!useColorFilter) return (targetColor, DefaultColorKey);
        if (targetColor.a < 1f)
        {
            return (targetColor, TransparentColorKey);
        }
        if (palette.colorPallete.Count == 0)
        {
            Debug.LogWarning("Color palette is empty!");
            return (targetColor, "");
        }

        Color closestColor = Color.white;
        string closestColorCode = "";
        float minDistance = float.MaxValue;

        foreach (var kvp in palette.colorPallete)
        {
            // Only consider colors that are in the colorCodeInUse list if useColorFilter is enabled and the list is not empty
            if (useColorFilter && colorCodeInUse.Count > 0 && !colorCodeInUse.Contains(kvp.Key))
            {
                continue;
            }

            float distance = ColorDistanceLAB(targetColor, kvp.Value);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestColor = kvp.Value;
                closestColorCode = kvp.Key; // Store the key (color code)
            }
        }

        // If no matching color was found in the filtered list, return the original color or a warning
        if (minDistance == float.MaxValue)
        {
            Debug.LogWarning($"No matching color found in the specified color codes for target color {targetColor}. Returning original color.");
            return (targetColor, "");
        }

        if (useColorFilter) return (closestColor, closestColorCode);
        else return (targetColor, closestColorCode);
    }

    // Calculate color distance using CIE76 formula (Euclidean distance in LAB space)
    // This is more perceptually accurate than RGB distance
    private float ColorDistanceLAB(Color color1, Color color2)
    {
        // Convert RGB to LAB for perceptually uniform color space
        Vector3 lab1 = RGBToLAB(color1);
        Vector3 lab2 = RGBToLAB(color2);
        
        float deltaL = lab1.x - lab2.x;
        float deltaA = lab1.y - lab2.y;
        float deltaB = lab1.z - lab2.z;
        
        return Mathf.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);
    }

    // Convert RGB color to LAB color space
    private Vector3 RGBToLAB(Color color)
    {
        // Convert RGB (0-1) to XYZ
        float r = color.r <= 0.04045f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
        float g = color.g <= 0.04045f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
        float b = color.b <= 0.04045f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);

        float x = (r * 0.4124f + g * 0.3576f + b * 0.1805f) / 0.95047f;
        float y = (r * 0.2126f + g * 0.7152f + b * 0.0722f) / 1f;
        float z = (r * 0.0193f + g * 0.1192f + b * 0.9505f) / 1.08883f;

        x = x > 0.008856f ? Mathf.Pow(x, 1f / 3f) : (7.787f * x) + (16f / 116f);
        y = y > 0.008856f ? Mathf.Pow(y, 1f / 3f) : (7.787f * y) + (16f / 116f);
        z = z > 0.008856f ? Mathf.Pow(z, 1f / 3f) : (7.787f * z) + (16f / 116f);

        float L = (116f * y) - 16f;
        float A = 500f * (x - y);
        float B = 200f * (y - z);

        return new Vector3(L, A, B);
    }

    private void CreatePaintingConfigAsset(List<PaintingPixelConfig> pixels, Vector2 gridSize, Sprite originalSprite)
    {
#if UNITY_EDITOR
        // Create a new PaintingConfig asset
        PaintingConfig paintingConfig = ScriptableObject.CreateInstance<PaintingConfig>();
        
        paintingConfig.Pixels = new List<PaintingPixelConfig>(pixels);
        paintingConfig.PaintingSize = gridSize;
        paintingConfig.Sprite = originalSprite;

        // Use the target painting name with "_PaintingConfig" suffix for the asset name
        string assetName = TargetPainting != null ? TargetPainting.name + "_PaintingConfig" : "PaintingConfig";
        
        // Create the asset file using the specified path and computed name
        string assetPath = PaintingConfigPath + assetName + ".asset";
        UnityEditor.AssetDatabase.CreateAsset(paintingConfig, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log("PaintingConfig asset created at: " + assetPath);
        
        // Assign the created PaintingConfig to the result field
        ResultPaintingConfig = paintingConfig;
#else
        Debug.LogWarning("PaintingConfig asset creation is only supported in the Unity Editor.");
#endif
    }

    public bool CanSample() 
    { 
        // If color filtering is not enabled, we can use any color from the palette
        if (!useColorFilter)
        {
            return TargetPainting != null && targetGrid != null && colorPalette != null;
        }
        
        // If color filtering is enabled but colorCodeInUse is empty, we can't filter
        if (colorCodeInUse.Count == 0)
        {
            return false;
        }
        
        // If color filtering is enabled and colorCodeInUse has values, check if they exist in the palette
        foreach (string colorCode in colorCodeInUse)
        {
            if (!colorPalette.colorPallete.ContainsKey(colorCode))
            {
                return false; // Invalid color code in the list
            }
        }
        
        return TargetPainting != null && targetGrid != null && colorPalette != null;
    }

    public void CreateNewPaintingConfig(Sprite _painting, string name = "")
    {

    }
}