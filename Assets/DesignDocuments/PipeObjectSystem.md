# Pipe Object System Overview

## Introduction

The Pipe Object System is an extension to the existing Grid System that allows for the creation and management of pipe objects on a 2D grid. Pipes can be placed either horizontally (in the same row) or vertically (in the same column) and span multiple grid cells. This system is designed to work seamlessly with the existing grid system and provides tools for both designers and developers to create pipe-based gameplay elements.

## System Architecture

### Core Components

#### 1. PipeObjectSetup
- **Type**: Serializable class
- **Purpose**: Represents the configuration data for a single pipe
- **Key Properties**:
  - `PixelCovered` (List<PaintingPixel>): Grid pixels that the pipe lies on (sorted from head to tail)
  - `ColorCode` (string): The color code associated with the pipe
  - `Scale` (Vector3): Scale applied to pipe parts (direct scale values)

#### 2. PipeObject
- **Type**: MonoBehaviour
- **Purpose**: Represents an actual pipe instance in the scene
- **Key Properties**:
  - `PipeHead` (Transform): Transform of the pipe's head component
  - `PipeBodyParts` (List<Transform>): Transforms of all pipe body parts and tail (sorted from head to tail)
  - `PaintingPixelsCovered` (List<PaintingPixel>): List of PaintingPixel objects that the pipe covers (preserved during destruction)
  - `IsHorizontal` (bool): True if pipe is horizontal (in same row), false if vertical (in same column)
- **Key Methods**:
  - `Initialize(Transform head, List<Transform> bodyParts, List<PaintingPixel> pipePixels, bool isHorizontal)`: Initialize the pipe structure with head, body parts, pixels and orientation
  - `HandlePipeShortening(PaintingPixel destroyedPixel)`: Handles the animation when a PaintingPixel in the pipe is destroyed. Moves all parts after the destroyed pixel toward the head, removing parts that reach the head position. Uses DOTween for animations.
  - `ApplyOrientationRotation()`: Applies rotation based on orientation (90° on Y-axis for horizontal pipes)
  - `SelfDestroy()`: Destroys the entire pipe object and all associated PaintingPixel components
  - `AddBodyPart(Transform bodyPart)`: Adds a body part to the pipe
  - `RemoveBodyPart(Transform bodyPart)`: Removes a body part from the pipe
  - `GetTotalPartsCount()`: Returns the total number of parts in the pipe (head + body parts)
  - `GetAllParts()`: Returns a list containing head and all body parts in order

#### 3. PipePartVisualHandle
- **Type**: MonoBehaviour
- **Purpose**: Handles visual representation and color changes for pipe parts
- **Key Properties**:
  - `pipeRenderers` (List<Renderer>): List of renderers to update for visual changes
- **Key Methods**:
  - `SetColor(Color)`: Changes the color of the pipe part using MaterialPropertyBlock

#### 4. PipeObjectConfigSetup
- **Type**: MonoBehaviour
- **Purpose**: Setup tool that creates pipe configurations and instantiates pipe objects in the scene
- **Key Properties**:
  - `pipeObjectSetups` (List<PipeObjectSetup>): List of pipe configurations being set up
  - `gridObject` (PaintingGridObject): Reference to the grid the pipes are placed on
  - `PipeHeadPrefab`, `PipeBodyPrefab`, `PipeTailPrefab`: Prefabs for pipe components
  - `ColorCode` (string): Color code for the pipe
  - `colorPalette` (ColorPalleteData): Color palette reference
  - `StartPixelComponent`, `EndPixelComponent` (PaintingPixelComponent): Start and end points of the pipe (assignable in inspector)
  - `Scale` (Vector3): Scale applied to pipe parts (direct scale values)
  - `PipeSpaceFromGrid` (int): Space from grid for placing pipes outside the grid (default: 1)
- **Key Methods**:
  - `CreatePipe()`: Creates a pipe between start and end points
  - `AddPipeSetup()`, `RemovePipeSetup()`: Manage pipe configuration list
  - `ImportPipesToPaintingConfig()`: Import pipe configurations to a PaintingConfig asset
  - `ApplyPaintingConfigPipes()`: Apply pipe configurations from a PaintingConfig asset

## Design Rules

### Pipe Orientation
- Pipes must be either straight horizontal (same row) or straight vertical (same column)
- Diagonal or curved pipes are not supported
- Validation ensures pipes follow the grid alignment

### Pipe Positioning
- Pipes are now positioned outside the main grid at a configurable distance
- `PipeSpaceFromGrid` parameter controls how far outside the grid the pipes are placed
- Horizontal pipes are offset in the positive Z-axis direction relative to the grid
- Vertical pipes are offset in the positive X-axis direction relative to the grid
- New `PaintingPixel` objects are created for each pipe part but stored separately from grid pixels

### Coordinate System
- Inherits the grid's coordinate system (center-point with [0,0] at center)
- X-axis represents columns: positive values to the right, negative to the left
- Y-axis represents rows: positive values upward, negative downward
- Supports negative indices for both columns and rows

## Integration Points

### With PaintingConfig
- `PaintingConfig` contains `List<PipeObjectSetup>` to store pipe configurations
- Pipe configurations can be imported/exported between `PipeObjectConfigSetup` and `PaintingConfig`

### With PaintingGridObject
- `PaintingGridObject` contains `List<PipeObject>` to store actual pipe instances
- `PaintingGridObject` contains `List<PaintingPixel> pipeObjectsPixels` to store new pipe painting pixels (separate from main grid pixels)
- The grid's `ApplyPaintingConfig()` method also applies pipe configurations
- Pipe objects are parented to the grid object
- Requires default pipe prefabs (`DefaultPipeHeadPrefab`, `DefaultPipeBodyPrefab`, `DefaultPipeTailPrefab`) to apply configurations from PaintingConfig

## Workflow

### Setup Phase
1. Assign a `PaintingGridObject` reference to `PipeObjectConfigSetup`
2. Set up prefabs for pipe head, body, and tail components
3. Assign start and end pixels using `PaintingPixelComponent` references (assignable in inspector)
4. Call `CreatePipe()` to generate the pipe in the scene and create the configuration

### Runtime Phase
1. Pipe configurations are stored in `PaintingConfig`
2. When `PaintingConfig` is applied to `PaintingGridObject`, pipe objects are instantiated
3. Pipe objects are managed alongside the grid's pixel objects

### Content Creation Phase
1. Level designers can select start and end `PaintingPixelComponent` objects in the inspector
2. Use the `CreatePipe()` button in the inspector to generate pipes
3. Export configurations to `PaintingConfig` assets using `ImportPipesToPaintingConfig()`

## Implementation Details

### Validation
- Each pipe creation validates that the start and end pixels are either in the same row or same column
- This ensures only horizontal or vertical pipes are created

### Prefab Structure
- Pipe GameObjects are parented with the head as the root
- Body parts and tail are children of the head
- This allows for easy management and transformation of the entire pipe

### Performance Considerations
- Pipe objects are created as GameObjects in the scene
- Each pipe part uses the same optimization techniques as standard grid pixels
- The system maintains references efficiently to avoid performance issues

### Visual Handling
- Pipe part colors are managed using MaterialPropertyBlock for performance
- The `PipePartVisualHandle` component controls color changes for each pipe part
- Colors are applied to the renderers in the `pipeRenderers` list using the `SetColor()` method
- This approach avoids material duplication for efficient rendering

### Orientation and Rotation
- Horizontal pipes (in same row) are automatically rotated 90 degrees on the Y-axis
- Vertical pipes (in same column) maintain default orientation
- Rotation is handled automatically by the PipeObject class based on the IsHorizontal property

### Editor Features
- Custom inspector for PipeObjectConfigSetup with "Create Pipe" button
- Additional buttons for clearing pipes, importing to PaintingConfig
- Visual feedback showing pipe counts and validation warnings
- Help boxes providing usage guidance

## File Locations
- Scripts: `Assets/_Game/Scripts/`
  - Core: `PipeObject.cs`, `PipeObjectSetup.cs`
  - Setup: `PipeObjectConfigSetup.cs`
  - Example: `PipeSetupExample.cs`
- Editor: `Assets/_Game/Editor/PipeObjectConfigSetupEditor.cs`
- Design Documents: `Assets/DesignDocuments/PipeObjectSystem.md`
- Integration: `PaintingConfig.cs`, `PaintingGridObject.cs`

## Common Operations

1. **Create a Pipe**: Use `PipeObjectConfigSetup.CreatePipe()` with assigned start/end components
2. **Import Configurations**: Use `PipeObjectConfigSetup.ImportPipesToPaintingConfig()` to save to asset
3. **Apply Configurations**: `PaintingGridObject.ApplyPaintingConfig()` automatically applies pipe configs
4. **Clear All Pipes**: `PaintingGridObject.DestroyAllPixelsObjects()` also destroys all pipe objects

## Editor Operations

1. **Create Pipe from Inspector**: Click "Create Pipe Between Selected Pixels" button in inspector
2. **Clear All Pipes**: Click "Clear All Pipe Setups" to remove all pipe configurations and objects
3. **Import to PaintingConfig**: Click "Import Pipes to PaintingConfig" to save configurations to asset
4. **View Pipe Counts**: Monitor pipe setup and object counts directly in the inspector

## Interaction Behavior

### Pipe Destruction and Shortening
- When a `PaintingPixel` in a pipe is destroyed, the pipe shortens by moving remaining parts toward the head
- Parts that reach the head position (when their position matches the head's position) disappear
- The `PaintingPixelsCovered` list remains unchanged to preserve the original pipe structure data
- Animations are handled using DOTween for smooth movement of pipe parts
- The head is always the last component to disappear if it's the only one remaining

## Design Considerations

- **Inspector usability**: Using `PaintingPixelComponent` allows assignment in the Unity inspector
- **Validation**: Built-in validation prevents invalid pipe orientations
- **Scalability**: System supports multiple pipes with different colors and configurations
- **Integration**: Seamless integration with existing grid and painting systems
- **Persistence**: Configurations can be saved into `PaintingConfig` assets for use in levels
- **Customization**: Includes parameters for scaling pipe parts (with Vector3 for independent axis scaling)
- **Interactive gameplay**: Pipes can be dynamically modified during gameplay with animated responses to pixel destruction

This pipe system provides a flexible and user-friendly approach to creating pipe-based gameplay on a grid system while maintaining compatibility with the existing architecture.