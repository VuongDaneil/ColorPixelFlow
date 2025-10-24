# Painting Sample to Grid System

## Overview
The Painting Sample to Grid System is a Unity-based solution that converts a painting (sprite) into a grid configuration by sampling colors from the painting and matching them to a predefined color palette. This system creates a `PaintingConfig` asset that maps colors to specific grid positions, enabling the creation of grid-based representations of paintings.

## Components

### 1. PaintingPixelConfig
- A serializable class that represents a single pixel in the grid
- **Properties:**
  - `column` (int): The column position in the grid
  - `row` (int): The row position in the grid
  - `color` (Color): The color value for this pixel

### 2. PaintingConfig
- A ScriptableObject that contains all the pixel data for a painting
- **Properties:**
  - `Pixels` (List<PaintingPixelConfig>): All pixels in the painting grid
  - `PaintingSize` (Vector2): The size of the painting as a grid (columns, rows)
  - `Sprite` (Sprite): The original painting sprite

### 3. PaintingConfigSetup
- A MonoBehaviour that handles the sampling process from painting to grid
- **Input Properties:**
  - `targetPainting` (Sprite): The original painting to sample colors from
  - `targetGrid` (PaintingGridObject): The grid object to match the painting to
  - `colorPalette` (ColorPalleteData): The colors that will be used in the grid
  - `configAssetName` (string): The name for the generated PaintingConfig asset
- **Key Methods:**
  - `SamplePaintingToGrid()`: Executes the entire sampling and export process

### 4. PaintingConfigSetupEditor
- A custom Unity Editor script for the PaintingConfigSetup
- Provides an intuitive interface in the Unity inspector
- Includes validation and a "Sample Painting to Grid" button

## Process Flow

1. **Input Validation**: The system validates that all required inputs (painting, grid, and color palette) are assigned
2. **Texture Extraction**: Gets the texture from the input sprite (handles non-readable textures by creating a readable copy)
3. **Grid Division**: Divides the painting texture into smaller parts based on the grid dimensions
4. **Color Sampling**: Samples the center pixel of each grid section
5. **Color Matching**: Finds the closest matching color in the provided color palette using perceptually accurate LAB color space distance
6. **Asset Creation**: Creates and exports a PaintingConfig asset with the sampled pixel data

## Color Matching Algorithm
The system uses the CIE76 formula (Euclidean distance in LAB color space) for accurate color matching. This approach provides better perceptual results than simple RGB distance calculations.

## Usage
1. Attach the `PaintingConfigSetup` script to a GameObject in your scene
2. Assign the required inputs in the Unity inspector:
   - Target Painting (Sprite)
   - Target Grid (PaintingGridObject)
   - Color Palette (ColorPalleteData)
   - Config Asset Name (string)
3. Click the "Sample Painting to Grid" button to generate the PaintingConfig asset

## Editor Features
- Visual validation of inputs
- Grid information display (size and total pixels)
- Color palette information (number of colors)
- Disabled sampling button when required inputs are missing

## Output
The system generates a `PaintingConfig.asset` file in the `Assets/_Game/Data/` folder with the sampled pixel data, ready to be used by other systems in the project.

## Dependencies
- Requires existing `PaintingGridObject.cs` and `ColorPalleteData.cs` scripts in the project
- Uses AYellowpaper.SerializedCollections for the color palette data structure
- Uses Unity's ScriptableObjects and Editor systems

## Color picker
- Include a List<string> of color codes call it ColorCodeInUse.
- From this list, we sample color closest to those color based on colorPalette.
- Only use color that has code in this list.