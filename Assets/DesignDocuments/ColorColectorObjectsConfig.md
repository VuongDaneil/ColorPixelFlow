# Color Collector Objects Configuration System

## Overview

The Color Collector Objects Configuration System allows for the management and setup of color collector objects (ColorPixelsCollector) in a level. Each level can have a squad of collectors arranged in a formation with multiple columns. This system provides flexible configuration of collector properties and automatic spawning based on configuration assets.

## Components

### SingleColorCollectorObject

A serializable class that stores configuration for an individual collector:

- **OriginalIndex** (int): The original index of the collector in its squad
- **ColorCode** (string): The color code that this collector can collect
- **Bullets** (int): Number of bullets available for collection (each bullet can collect one color)
- **Locked** (bool): Whether the collector is initially locked (inactive)
- **Hidden** (bool): Whether to hide information about color and bullets from the player
- **ConnectedCollectorsIndex** (List<int>): List of original indices of other collectors connected to this collector

### LevelColorCollectorsConfig

A ScriptableObject that stores the configuration for an entire squad of collectors:

- **CollectorSetups** (List<SingleColorCollectorObject>): List containing configuration for each collector in the level
- **NumberOfColumns** (int): Number of columns in the formation

### CollectorColumn

A class that groups collectors by column:

- **CollectorsInColumn** (List<ColorPixelsCollector>): List of collectors in a single column

### Modified ColorPixelsCollector

The existing collector script was enhanced with:

- **OriginalIndex** (int): The collector's original position in the squad
- **Available** (bool, read-only): Returns true if the collector still has bullets remaining, false if out of bullets

### LevelCollectorsSystem

The system responsible for spawning and organizing collectors based on configuration:

#### Input Properties:
- **ColorPalette**: The color source that collectors will get color from using color codes
- **CurrentLevelCollectorsConfig**: The configuration asset defining the squad layout
- **FormationCenter**: Transform that serves as the center of the formation
- **SpaceBetweenColumns**: Distance between each column
- **SpaceBetweenCollectors**: Distance between each collector in a column
- **CollectorPrefab**: The prefab to instantiate for each collector
- **CollectorContainer**: The parent transform to contain all spawned collectors
- **CollectorRotation**: Rotation vector to apply to spawned collectors

#### Runtime Properties:
- **CurrentCollectors**: List of currently spawned collectors
- **CollectorColumns**: List of column groups

#### Functionality:
- Automatically spawns collectors based on the configuration asset
- Arranges collectors in a grid formation with specified columns (row-major order)
- Assigns properties from the configuration to each spawned collector
- Organizes collectors into column groups for further processing

### LevelCollectorsConfigSetup

An editor utility script with methods to:

- Generate default configurations with specified parameters
- Update existing configurations to ensure proper indexing
- Provide editor buttons for quick setup

## Usage

### Creating a Configuration Asset

1. In the Unity Editor, right-click in the Project window
2. Navigate to Create > ColorPixelFlow > Level Color Collectors Config
3. Name your new configuration asset

### Setting up Collectors

1. Create a LevelColorCollectorsConfig asset with your collector configurations
2. Add a LevelCollectorsSystem component to a GameObject in your scene
3. Assign the configuration asset and other required parameters
4. The system will automatically spawn collectors when the scene starts

### Editor Setup

Use the LevelCollectorsConfigSetup component to:

- Generate default configurations with the "Generate Default Config" button
- Update existing configurations with the "Update Config From Existing" button

## Formation Layout

The collectors are arranged in a grid pattern using row-major order, meaning collectors are filled horizontally first. The FormationCenter represents the highest point (first row) of the formation, with subsequent rows positioned in the opposite direction of the FormationCenter's forward vector. The collectors are positioned relative to the FormationCenter's transform orientation:

- The first row is positioned at the FormationCenter's position
- Columns are positioned along the FormationCenter's right direction
- Subsequent rows are positioned along the negative of the FormationCenter's forward direction

For example, with 9 collectors arranged in 3 columns:

```
1 2 3  <- FormationCenter position (highest row)
4 5 6
7 8 9
```

Where each column would be: Column 1: [1,4,7], Column 2: [2,5,8], Column 3: [3,6,9]

## Available Property

The `Available` property on ColorPixelsCollector returns true when the collector has bullets remaining and can still collect colors, false when it has run out of bullets.

## Connections

The `ConnectedCollectorsIndex` property allows defining relationships between collectors to enable advanced gameplay mechanics.