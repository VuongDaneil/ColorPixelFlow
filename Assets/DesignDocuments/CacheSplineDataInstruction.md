# Spline Data Caching System - User Instructions

This document provides step-by-step instructions on how to use the spline data caching system to improve performance and enable smooth object movement along pre-calculated paths.

## Overview

The spline data caching system consists of two main approaches:

### Array-Based Approach
- **SplineDataContainer**: ScriptableObject that stores cached spline data as arrays
- **SplineDataCacher**: Component that caches CurvySpline data to a ScriptableObject
- **CachedSplineMover**: Component that moves objects using cached spline data

### Transform-Based Approach (Recommended for Hierarchical Data)
- **CachedSplineTransformPath**: ScriptableObject that stores cached spline data as a list of Transform objects
- **CachedTransformPathMover**: Component that moves objects using cached Transform-based spline data

## Setup Instructions

### Array-Based Approach (World Space Only)

#### Step 1: Creating Cached Spline Data
1. Create or select a GameObject with a CurvySpline component
2. Add the **SplineDataCacher** component to the same GameObject
3. Adjust the **Resolution** parameter to control the distance between sample points:
   - Lower values (e.g., 0.1) = more accuracy but higher memory usage
   - Higher values (e.g., 2.0) = less accuracy but lower memory usage
4. Click the **"Cache Spline Data"** button in the inspector
5. The system will create a new SplineDataContainer asset in `Assets/Resources/SplineDataContainers/`

#### Step 2: Using Cached Data for Movement
1. Create a GameObject that you want to move along the spline (e.g., a character, vehicle, or camera)
2. Add the **CachedSplineMover** component to the GameObject
3. Assign the SplineDataContainer asset created in Step 1 to the **Spline Data Container** field
4. Configure movement parameters:
   - **Speed**: Movement speed in units per second
   - **Current TF**: Starting position along the spline (0 = start, 1 = end)
   - **Movement Type**: Choose between Clamp, Loop, or PingPong behavior
   - **Auto Move**: Enable for automatic movement, disable for manual control
   - **Orient to Path**: Enable to align the object to the spline's direction

### Transform-Based Approach (Supports Local/World Space)

#### Step 1: Creating Cached Transform Path Data
1. Create an empty GameObject that will serve as your path container
2. Add several child GameObjects to this container along your desired path
3. Create a new **CachedSplineTransformPath** asset via `Assets > Create > Curvy > Cached Spline Transform Path`
4. In the asset, assign your path transforms (the child GameObjects) to the **Path Transforms** list
5. Set the **Total Distance**, **Sample Count**, and **Resolution** as needed
6. If using local space, check **Is Local Space** and assign a **Space Reference** transform

#### Step 2: Using Cached Transform Path for Movement
1. Create a GameObject that you want to move along the path
2. Add the **CachedTransformPathMover** component to the GameObject
3. Assign the CachedSplineTransformPath asset to the **Transform Path** field
4. Configure movement parameters (same as Array-based approach)

## Movement Options

### TF-Based Movement (0-1 range)
- Use the **Current TF** field to specify position from 0 (start) to 1 (end)
- Movement continues along this 0-1 normalized range
- Good for percentage-based positioning

### Distance-Based Movement
- Check the **Use Distance Based Movement** option
- Use the **Current Distance** field to specify position in world units from the spline start
- Movement continues based on distance traveled
- Good for physics-based interactions

### Controlling Movement Programmatically

Both systems provide similar public methods for runtime control:

```csharp
// For CachedSplineMover
// Get or set position using TF (0-1 value)
mover.SetPositionByTF(0.5f); // Move to middle of spline
Vector3 position = mover.GetPositionAtTF(0.75f); // Get position at 75% along spline

// Get or set position using distance
mover.SetPositionByDistance(10f); // Move to 10 units along spline
Vector3 tangent = mover.GetTangentAtDistance(5f); // Get tangent at 5 units along spline

// Movement control
mover.StartMovement(); // Start automatic movement
mover.StopMovement(); // Stop automatic movement
mover.SetMovementDirection(-1); // Change direction to backward

// For CachedTransformPathMover (same interface)
transformMover.SetPositionByTF(0.5f); // Move to middle of path
Vector3 position = transformMover.GetPositionAtTF(0.75f); // Get position at 75% along path
```

## Movement Types Explained

1. **Clamp**: The object stops when it reaches the beginning or end of the spline
2. **Loop**: The object wraps around from the end to the beginning (and vice versa)
3. **PingPong**: The object reverses direction when it reaches the ends of the spline

## When to Use Each Approach

### Array-Based Approach (SplineDataContainer)
- Best for simple world-space paths
- Lower memory usage
- Good for static paths that don't change hierarchy

### Transform-Based Approach (CachedSplineTransformPath)
- Use when you need local space support
- Best when your path moves as part of a hierarchy
- More flexible for complex animation scenarios
- Easier to visualize and edit in the scene

### Converting Between Approaches

You can convert from Array-based to Transform-based using the **SplineDataContainerToTransformConverter**:

1. Create an empty GameObject in your scene
2. Add the **SplineDataContainerToTransformConverter** component to the GameObject
3. Assign your **SplineDataContainer** asset to the **Input SplineDataContainer** field
4. Optionally assign a parent transform and path point prefab
5. Click **"Convert SplineDataContainer to Transforms"**
6. The tool will create:
   - GameObjects for each cached point, parented under your specified parent (or a new GameObject)
   - A new **CachedSplineTransformPath** asset with references to the created transforms
7. You can then use the resulting **CachedSplineTransformPath** with **CachedTransformPathMover**

## Best Practices

### Performance Tips
- Use higher resolution values for straighter splines and lower resolution for more curved splines
- Balance accuracy vs. memory usage based on your requirements
- Consider using the cached system only for objects that move frequently along the same paths

### Troubleshooting
- If the spline appears to have gaps in movement, decrease the resolution value
- If movement seems choppy, ensure the SplineDataContainer or CachedSplineTransformPath has valid data
- If using Transform-based approach and movement is not as expected, check transform orientations

### Updating Cached Data
- When using Array-based approach: When the original spline changes, you must recache the data using the "Cache Spline Data" button
- When using Transform-based approach: Simply update the transforms in the path, and the changes will be reflected in real-time

## Example Use Cases

### Camera Following Path
1. Create a camera GameObject with CachedTransformPathMover
2. Set up transforms along your desired path
3. Create a CachedSplineTransformPath asset with those transforms
4. Enable "Orient to Path" to have the camera look forward along the path
5. Set appropriate speed for a cinematic experience

### Enemy Patrolling
1. Create an enemy GameObject with CachedTransformPathMover
2. Create transforms for patrol route
3. Use "PingPong" movement type for back-and-forth patrolling
4. Use the system to move multiple enemies along the same path without real-time calculations

### Moving Platforms
1. Create a platform GameObject with CachedTransformPathMover
2. Create a CachedSplineTransformPath that moves with a parent object
3. Use local space to ensure the platform follows the parent's movement
4. Enable automatic movement to make the platform move along its local path

## Technical Details

### Array-Based System
- Stores the following data points along the spline:
  - **Positions**: World positions sampled at regular intervals
  - **Tangents**: Direction vectors at each sample point
  - **Up Vectors**: Orientation vectors for proper rotation
  - **Distances**: Cumulative distances from spline start for each sample point
  - **Metadata**: Total spline distance, sample count, and resolution used

### Transform-Based System
- Stores the following data points:
  - **Path Transforms**: List of Transform objects defining the path
  - **Each Transform** provides position, rotation, and orientation data
  - **Metadata**: Total spline distance, sample count, and resolution used

This caching system is especially beneficial for:
- Mobile applications where performance is critical
- Scenarios with multiple objects following the same path
- Situations where real-time spline calculations are too expensive
- Complex spline calculations that benefit from pre-computation
- Hierarchical animations where parent transforms affect path positions