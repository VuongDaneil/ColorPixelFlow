# Color Pixels Collector System

## Overview
The Color Pixels Collector System is a gameplay component that moves around a grid and destroys pixels matching its color. It integrates with the existing grid system and uses a spline-based movement system to navigate around the grid perimeter.

## System Components

### 1. ColorPixelsCollector.cs
- **Type**: MonoBehaviour
- **Purpose**: Controls the collector gameplay object that moves around the grid and destroys matching colored pixels
- **Key Properties**:
  - `MovementHandle` (CachedTransformPathMover): Movement handler for spline-based navigation
  - `CurrentGrid` (PaintingGridObject): Reference to the target grid object
  - `BulletCapacity` (int): Maximum number of bullets/shots available
  - `BulletLeft` (int): Number of bullets remaining (read-only in inspector)
  - `ColorCodeToDestroy` (string): Color code of pixels that can be destroyed
  - `CollectorColor` (string): Color of the collector based on pixel color
  - `DetectionRadius` (float): Distance threshold for detecting and shooting nearby pixels
  - `IsCollectorActive` (bool): Flag indicating if the collector is currently active

- **Key Methods**:
  - `InitializeCollector()`: Sets up the collector with initial values and event subscription
  - `UpdatePossibleTargets()`: Updates the list of possible target pixels based on grid state
  - `CheckAndDestroyNearbyPixels()`: Detects and destroys pixels within detection radius
  - `DestroyPixel(PaintingPixel)`: Destroys a specific pixel and updates bullet count
  - `ResetCollector()`: Resets collector to initial state
  - `SetCollectorActive(bool)`: Activates/deactivates the collector
  - `OnGridPixelsChanged()`: Callback for when grid pixels change (updates possible targets)
  - `CheckForObstaclesInRow(PaintingPixel, Vector3)`: Checks for obstacle pixels (different color) between collector and target in the same row during horizontal movement
  - `CheckForObstaclesInColumn(PaintingPixel, Vector3)`: Checks for obstacle pixels (different color) between collector and target in the same column during vertical movement
  - `CheckMovementDirectionChange()`: Detects when movement direction changes and resets appropriate processed position tracking

### 2. Integration Points

#### Grid System Integration
- Uses `PaintingGridObject.SelectOutlinePixelsWithColor()` to find destroyable pixels
- Calls `PaintingGridObject.RemovePixel()` to remove pixels from the grid
- Subscribes to `GameplayEventsManager.OnGridPixelsChanged` to update targets when grid changes

#### Movement System Integration
- Uses `CachedTransformPathMover` for spline-based movement around the grid
- Controls movement via `StartMovement()` and `StopMovement()` methods

#### Event System Integration
- Subscribes to `GameplayEventsManager.OnGridPixelsChanged` for real-time grid updates
- Automatically updates possible targets when grid state changes

## Core Gameplay Mechanics

### Movement
- The collector follows a spline path around the grid perimeter
- Movement is controlled by the CachedTransformPathMover component
- Supports clockwise and counterclockwise directions via movement parameters
- Movement direction is tracked to determine horizontal vs vertical movement patterns

### Pixel Detection and Destruction
- Uses directional detection based on movement vector (X-axis for horizontal movement, Z-axis for vertical movement) with a configurable `DetectionRadius`
- Only destroys pixels that match the collector's `ColorCodeToDestroy`
- Limited by bullet capacity system (`BulletCapacity` and `BulletLeft`)
- Destroys only outline pixels (perimeter of the grid) of matching color
- Implements obstacle detection to prevent destruction when different-colored pixels exist between collector and target
- Implements one-pixel-per-row/column limitation to restrict destruction to one pixel per row/column in current movement direction

### Color Matching
- Uses both string-based color codes and Unity Color objects for matching
- Supports basic color strings: red, green, blue, yellow, cyan, magenta, white, black, gray/grey
- Uses `PaintingGridObject.SelectOutlinePixelsWithColor()` to filter by color

### Advanced Destruction Logic
- **Obstacle Check**: Verifies that no pixels with different colors exist between collector and target pixel before destruction
  - When moving horizontally, checks for obstacle pixels in the same row between collector's and target's X coordinates
  - When moving vertically, checks for obstacle pixels in the same column between collector's and target's Z coordinates
- **One-Pixel-Per-Row/Column Limitation**: Tracks which columns/rows have been processed in the current movement direction
  - `processedHorizontalPositions`: HashSet tracking columns processed during horizontal movement
  - `processedVerticalPositions`: HashSet tracking rows processed during vertical movement
  - Resets appropriate tracking when movement direction changes from horizontal to vertical or vice versa
- **Direction-Based Position Tracking**: Tracks movement direction changes to properly reset processed position counters

## Configuration Requirements

### GameObject Setup
To use the ColorPixelsCollector system:
1. Create a GameObject with the `ColorPixelsCollector` component
2. Assign a `CachedTransformPathMover` component for movement
3. Set the `CurrentGrid` to target a `PaintingGridObject`
4. Configure `ColorCodeToDestroy` to match target pixel colors
5. Set up the spline path using `CachedSplineTransformPath` asset

### Spline Path Setup
- Create a `CachedSplineTransformPath` asset
- Define waypoints that form a loop around the target grid
- Assign this asset to the `CachedTransformPathMover` component
- The spline should follow the perimeter of the grid for proper functionality

## Event Handling
- The system subscribes to `GameplayEventsManager.OnGridPixelsChanged` 
- When pixels are destroyed (either by collector or other means), the event is triggered
- The collector updates its possible targets list automatically when this event fires
- This ensures the collector always knows which pixels are still available for destruction

## Visual Feedback
- Gizmo visualization in editor showing detection radius when selected
- Uses bullet counter to show remaining shots
- Movement path visualization via CachedTransformPathMover

## Dependencies
- `PaintingGridObject.cs` - Grid management and pixel access
- `CachedTransformPathMover.cs` - Spline-based movement
- `GameplayEventsManager.cs` - Event system for grid updates
- `PaintingPixel.cs` - Individual pixel data structure
- `PaintingPixelComponent.cs` - Pixel visual representation

## File Locations
- Script: `Assets/_Game/Scripts/ColorPixelsCollector.cs`
- Design Document: `Assets/DesignDocuments/ColorCollectorObjectSystem.md`
- Dependencies: `Assets/_Game/Scripts/`

## Known Limitations
- Only destroys outline pixels (perimeter of grid), not internal pixels
- Requires pre-defined spline path around the grid
- Bullet capacity system limits total destruction
- Uses distance-based detection which may need tuning based on grid scale
- Obstacle detection and one-pixel-per-row/column logic may need fine-tuning based on grid density and movement speed
- Direction change detection relies on movement tangent vector which may not work well with all spline configurations

## Expansion Opportunities
- Add different collector types with varied behaviors
- Implement power-ups to increase bullet capacity
- Add visual effects for shooting and destruction
- Support multiple simultaneous collectors with different colors
- Add different movement patterns beyond spline paths