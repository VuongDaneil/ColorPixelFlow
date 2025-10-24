# Painting Grid Generator System

## Overview
The Painting Grid Generator System is a Unity-based grid system that creates a 2D grid of paintable pixels. The system uses negative indices and allows for runtime color changes using MaterialPropertyBlocks. The coordinate system uses X for columns (horizontal axis) and Y for rows (vertical axis), with [0,0] at the center. It is designed to be used in painting or grid-based applications where individual pixels can be modified.

## Architecture

### Core Components

#### 0. PaintingConfig.cs (ScriptableObject)
- ScriptableObject that defines a painting configuration
- Properties:
  - `Pixels` (List<PaintingPixelConfig>): List of pixel configurations to apply
  - `PaintingSize` (Vector2): Size of the painting
  - `Sprite` (Sprite): Associated sprite if needed

#### 1. PaintingPixelConfig.cs (Serializable)
- Serializable class defining a single pixel configuration
- Properties:
  - `column` (int): Grid column position
  - `row` (int): Grid row position
  - `color` (Color): Color to apply to the pixel

#### 2. PaintingPixel.cs (MonoBehaviour)
- Represents a single pixel/unit in the grid
- Properties:
  - `name` (string): Default "PaintingPixel"
  - `column` (int): Grid column position (X coordinate, can be negative)
  - `row` (int): Grid row position (Y coordinate, can be negative)
  - `color` (Color): Current pixel color
  - `worldPos` (Vector3): World position of the pixel
  - `destroyed` (bool): Flag indicating if pixel is destroyed
  - `pixelObject` (GameObject): Reference to the GameObject associated with this pixel

- Methods:
  - `SetColor(Color newColor)`: Updates pixel color
  - `SetPosition(Vector3 newPos)`: Updates world position
  - `DestroyPixel()`: Sets destroyed flag to true

#### 3. PaintingPixelComponent.cs (MonoBehaviour)
- Component that attaches to pixel GameObjects to manage their visual representation
- Properties:
  - `Pixel` (PaintingPixel): Reference to the PaintingPixel data

- Methods:
  - `SetPixel(PaintingPixel newPixel)`: Sets the pixel data reference
  - `UpdateVisual()`: Updates the GameObject's visual based on pixel data
  - `GetWorldPosition()`: Returns the world position
  - `SetColor(Color newColor)`: Changes the color using MaterialPropertyBlock
  - `IsDestroyed()`/`DestroyPixel()`: Checks/sets destroyed state

#### 4. PaintingGridObject.cs (MonoBehaviour)
- A MonoBehaviour that serves as a container for all pixels in a grid
- Properties:
  - `gridSize` (Vector2): Size of the grid (columns x rows)
  - `paintingPixels` (List<PaintingPixel>): List of all pixels in the grid
  - `pointArrangeSpace` (float): Space between each pixel
  - `pointPrefab` (GameObject): Prefab to use for pixels
  - `paintingConfig` (PaintingConfig): Painting configuration to apply to the grid

- Methods:
  - `InitializeGrid(Vector2 size, float arrangeSpace, GameObject prefab)`: Sets up the grid properties
  - `GenerateGrid(Transform centerPoint)`: Creates the grid of pixels
  - `UpdatePixelColor(int col, int row, Color newColor)`: Updates color of a specific pixel
  - `GetPixelAt(int col, int row)`: Gets a specific pixel
  - `GetTotalPixels()`: Returns total number of pixels
  - `ClearAllPixels()`: Removes all pixels
  - `ApplyPaintingConfig()`: Applies the assigned PaintingConfig to the grid by setting colors for paintingPixels
  - `ClearGridToWhite()`: Clears the grid and restores pixels to white color as default
  - Various helper methods for grid management

#### 5. GridGenerator.cs (MonoBehaviour)
- Main system controller that manages grid creation and operations
- Properties:
  - `gridSize` (Vector2): Size of the grid
  - `centerPoint` (Transform): Transform to center the grid around (defaults to script's transform)
  - `pointPrefab` (GameObject): Prefab for individual pixels
  - `pointScaleFactor` (float): Scale multiplier for pixels
  - `pointArrangeSpace` (float): Distance between pixels
  - `paintingGridObject` (PaintingGridObject): The grid object component
  - `pixelsParent` (Transform): Transform to parent all pixel objects to

- Methods:
  - `GenerateGrid()`/`ContextGenerateGrid()`: Creates the grid
  - `ClearGrid()`/`ContextClearGrid()`: Clears the grid
  - `UpdatePixelColor(int col, int row, Color newColor)`: Updates color of a specific pixel
  - `GetPixelAt(int col, int row)`: Gets a specific pixel
  - `GetWorldPosition(int col, int row)`: Gets world position of grid coordinate
  - `GetTotalPixels()`: Returns total number of pixels in grid

#### 6. GridGeneratorEditor.cs (Editor)
- Custom Unity editor for the GridGenerator component
- Provides inspector buttons for grid operations (Generate, Clear, Regenerate)
- Displays grid information in the inspector

#### 7. PaintingGridObjectEditor.cs (Editor)
- Custom Unity editor for the PaintingGridObject component
- Provides inspector buttons for painting configuration operations
- Features:
  - "Apply Painting Config" button: Applies the assigned PaintingConfig to the grid
  - "Clear Grid to White" button: Clears the grid and restores pixels to white color
  - "Regenerate Grid" button: Convenience button to regenerate the grid

## Coordinate System
- Grid uses a center-point coordinate system where [0,0] is at the center
- X coordinates: positive values toward the right, negative toward the left (represents column)
- Y coordinates: positive values toward the top, negative toward the bottom (represents row)
- For a 20x40 grid example: 
  - Center point is [0, 0]
  - Far left point is [-10, 0] (when cols=20, rows=40)
  - Far right point is [10, 0]
  - Top most point is [0, 20]
  - Lowest point is [0, -20]

## Material Property Block Implementation
- Uses MaterialPropertyBlock for efficient color changes without material instance creation
- Colors are changed via the "_Color" shader property
- Allows for per-pixel color changes without performance impact
- Compatible with the existing "UnlitReceiveShadow" shader

## Usage Instructions

### Setting Up a Grid
1. Create an empty GameObject in the scene
2. Attach the `GridGenerator` component to the GameObject
3. Create or assign a pixel prefab with:
   - MeshRenderer component
   - Material that supports the "_Color" property
   - Optionally, the `PaintingPixelComponent` attached
4. In the GridGenerator inspector:
   - Set the grid size (e.g., 20x40)
   - Assign the `pointPrefab`
   - Adjust `pointScaleFactor` and `pointArrangeSpace` as needed
   - Optionally assign a `pixelsParent` Transform for pixel parenting
5. Use the "Generate Grid" button in the inspector to create the grid

### Runtime Operations
- Use `gridGenerator.UpdatePixelColor(col, row, newColor)` to change pixel colors
- Use `gridGenerator.GetPixelAt(col, row)` to get pixel data
- Use `gridGenerator.GetWorldPosition(col, row)` to get world position

### Edit Mode Operations
- Use context menu options or inspector buttons to regenerate/clear the grid
- Modify grid parameters in the inspector and regenerate

### Painting Configuration Operations
- Assign a PaintingConfig asset to the `paintingConfig` field in the PaintingGridObject component
- Use the "Apply Painting Config" button in the inspector to apply the configuration to the grid
- Use the "Clear Grid to White" button to reset all pixels to white color
- Use the "Regenerate Grid" button as a convenience to recreate the grid

## Development Notes
- The system is designed for edit-time setup but can be extended for runtime grid modifications
- Negative indices are fully supported in both columns and rows
- The system uses parent-child relationships for efficient organization
- MaterialPropertyBlock implementation ensures optimal performance for color changes
- The editor script provides user-friendly controls for non-programmers

## File Locations
- Scripts: `Assets/_Game/Scripts/`
  - PaintingPixel.cs
  - PaintingGridObject.cs
  - PaintingPixelComponent.cs
  - PointObject.cs
  - GridGenerator.cs
- Editor: `Assets/_Game/Editor/`
  - GridGeneratorEditor.cs
  - PaintingGridObjectEditor.cs
- Materials: `Assets/_Game/Materials/`
  - PointMaterial.mat