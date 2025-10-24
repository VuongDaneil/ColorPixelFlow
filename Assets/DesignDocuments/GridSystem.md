# Grid System Overview

## Introduction
The Grid System is a Unity-based framework for creating and managing 2D grids of interactive elements. It's designed for applications that require paintable pixels, game board systems, or any grid-based visual interface. The system supports negative indices, runtime modifications, and efficient rendering through MaterialPropertyBlocks.

## System Architecture

### Core Components

#### 1. PaintingConfig
- **Type**: ScriptableObject
- **Purpose**: Defines configurations for painting operations
- **Key Properties**:
  - `Pixels` (List<PaintingPixelConfig>): Collection of pixel configurations
  - `PaintingSize` (Vector2): Dimensions of the painting area
  - `Sprite` (Sprite): Optional associated sprite

#### 2. PaintingPixelConfig
- **Type**: Serializable class
- **Purpose**: Defines configuration for a single pixel
- **Key Properties**:
  - `column` (int): Grid column position (X coordinate)
  - `row` (int): Grid row position (Y coordinate)
  - `color` (Color): Color to apply to the pixel

#### 3. PaintingPixel
- **Type**: MonoBehaviour
- **Purpose**: Represents a single pixel in the grid
- **Key Properties**:
  - `column` (int): Grid column position (can be negative)
  - `row` (int): Grid row position (can be negative)
  - `color` (Color): Current pixel color
  - `colorCode` (string): Code representing the pixel's color for gameplay matching
  - `worldPos` (Vector3): World position in 3D space
  - `destroyed` (bool): Flag indicating destruction state
  - `pixelObject` (GameObject): Reference to the GameObject representing the pixel

#### 4. PaintingPixelComponent
- **Type**: MonoBehaviour
- **Purpose**: Manages visual representation of a pixel
- **Key Properties**:
  - `Pixel` (PaintingPixel): Reference to the pixel data
- **Key Methods**:
  - `SetColor(Color)`: Changes the pixel color
  - `UpdateVisual()`: Updates the visual representation
  - `GetWorldPosition()`: Returns world position
  - `DestroyPixel()`: Marks the pixel as destroyed

#### 5. PaintingGridObject
- **Type**: MonoBehaviour
- **Purpose**: Container managing all pixels in a grid
- **Key Properties**:
  - `gridSize` (Vector2): Size of the grid (columns x rows)
  - `paintingPixels` (List<PaintingPixel>): List of all pixels in the grid
  - `pipeObjectsPixels` (List<PaintingPixel>): List of pipe pixels positioned outside the grid
  - `pointArrangeSpace` (float): Space between pixels
  - `pointPrefab` (GameObject): Prefab used for individual pixels
  - `paintingConfig` (PaintingConfig): Configuration applied to the grid
  - `pipeSetups` (List<PipeObjectSetup>): List of pipe configurations
  - `pipeObjects` (List<PipeObject>): List of pipe objects in the scene
  - `DefaultPipeHeadPrefab`, `DefaultPipeBodyPrefab`, `DefaultPipeTailPrefab`: Default prefabs for pipe objects
- **Key Methods**:
  - `InitializeGrid()`: Sets up grid properties
  - `GenerateGrid(Transform)`: Creates the grid of pixels
  - `UpdatePixelColor(int, int, Color)`: Updates color of a specific pixel
  - `GetPixelAt(int, int)`: Retrieves a specific pixel
  - `ApplyPaintingConfig()`: Applies painting configuration to the grid
  - `ApplyPipeConfigurations()`: Applies pipe configurations to the grid
  - `DestroyAllPixelsObjects()`: Destroys all pixels and pipe objects in the grid

#### 6. GridGenerator
- **Type**: MonoBehaviour
- **Purpose**: Main controller managing grid creation and operations
- **Key Properties**:
  - `gridSize` (Vector2): Size of the grid
  - `centerPoint` (Transform): Transform around which the grid is centered
  - `pointPrefab` (GameObject): Prefab for individual pixels
  - `pointArrangeSpace` (float): Distance between pixels
  - `paintingGridObject` (PaintingGridObject): Reference to grid object component
- **Key Methods**:
  - `GenerateGrid()`: Creates the grid
  - `ClearGrid()`: Removes all pixels from the grid
  - `UpdatePixelColor(int, int, Color)`: Updates color of a specific pixel
  - `GetPixelAt(int, int)`: Gets a specific pixel at grid coordinates

#### 7. Editor Components
- **GridGeneratorEditor**: Custom inspector for GridGenerator with generation controls
- **PaintingGridObjectEditor**: Custom inspector for PaintingGridObject with painting config controls

## Coordinate System
- Grid uses center-point coordinate system with [0,0] at the center
- X-axis represents columns: positive values to the right, negative to the left
- Y-axis represents rows: positive values upward, negative downward
- Supports negative indices for both columns and rows
- Example: In a 20x40 grid centered at [0,0]:
  - Far left point is [-10, 0]
  - Far right point is [10, 0]
  - Top point is [0, 20]
  - Bottom point is [0, -20]

## Performance Features
- Uses MaterialPropertyBlock for efficient color changes without material duplication
- Supports per-pixel color changes without significant performance impact
- Organizes pixels using parent-child relationships for efficient management

## Integration Points
- Works with the "UnlitReceiveShadow" shader
- Supports runtime grid modifications
- Provides edit-time configuration tools
- Compatible with Unity's GameObject hierarchy
- Integrates with the Pipe Object System through PaintingConfig (List<PipeObjectSetup>)
- Maintains separate lists for grid pixels and pipe pixels (paintingPixels vs pipeObjectsPixels)
- Supports pipe object instantiation with ApplyPipeConfigurations() method

## File Locations
- Scripts: `Assets/_Game/Scripts/`
- Editor: `Assets/_Game/Editor/`
- Materials: `Assets/_Game/Materials/`

## Common Operations
1. **Grid Creation**: Use GridGenerator to initialize and generate a grid
2. **Color Updates**: Use `UpdatePixelColor()` to change individual pixels
3. **Pixel Access**: Use `GetPixelAt()` to retrieve specific pixel data
4. **Configuration**: Apply PaintingConfig assets using editor tools
5. **Pipe Color Updates**: Pipe colors are managed through `PipePartVisualHandle.SetColor()` method using MaterialPropertyBlock