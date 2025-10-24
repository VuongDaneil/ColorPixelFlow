# Spline Data Caching System Implementation

This document outlines the tasks required to implement a system that caches Curvy spline data to a ScriptableObject and uses that cached data to move objects along the spline.

## Overview

The system will allow pre-caching CurvySpline data (positions, tangents, distances, etc.) to a ScriptableObject for efficient runtime access. This will enable smooth object movement along pre-calculated paths without requiring real-time spline calculations.

## Components to Create

### 1. SplineDataContainer ScriptableObject

**Purpose**: Store cached spline data in a structured format.

**Fields to include**:
- `Vector3[] positions`: Sampled positions along the spline
- `Vector3[] tangents`: Tangent vectors at each position
- `Vector3[] upVectors`: Up vectors for orientation at each position
- `float[] distances`: Cumulative distances from spline start for each position
- `float totalDistance`: Total length of the spline
- `int sampleCount`: Number of samples taken
- `float resolution`: Distance between sample points

### 2. SplineDataCacher Component

**Purpose**: Component that caches data from a CurvySpline to the ScriptableObject.

**Functionality**:
- Accepts a CurvySpline reference
- Takes a resolution parameter (distance between samples)
- Samples the spline at regular intervals
- Populates the SplineDataContainer with position, tangent, up-vector, and distance data
- Option to cache data from editor script or via a button in the inspector
- Validates that the spline is initialized before caching

### 3. CachedSplineMover Component

**Purpose**: Component that uses cached spline data to move objects along the path.

**Functionality**:
- Accepts a SplineDataContainer reference
- Provides methods to get position, tangent, and up-vector at a given TF (0-1 value) or distance
- Implements linear interpolation between cached points for smooth movement
- Supports forward/backward movement along the spline
- Includes clamping options (loop, clamp, ping-pong) similar to the original spline controller
- Provides methods for both TF-based (0-1) and distance-based movement

## Implementation Tasks

### Phase 1: Define Data Structures

1. **Create SplineDataContainer ScriptableObject**:
   - Create a new C# class that inherits from ScriptableObject
   - Define public fields for positions, tangents, upVectors, distances, and metadata
   - Include serialization attributes for proper Inspector display

2. **Create editor script for SplineDataContainer**:
   - Add custom inspector for better visualization of cached data
   - Implement "Draw Gizmos" options to visualize the cached path in the scene view

### Phase 2: Create Caching System

3. **Create SplineDataCacher component**:
   - Create a MonoBehaviour component with public fields for CurvySpline and resolution
   - Add a "Cache Data" button in the inspector
   - Implement method to sample the spline at regular intervals based on resolution
   - Calculate cumulative distances between sample points
   - Create and save the SplineDataContainer asset

4. **Implement spline sampling logic**:
   - Loop through the spline from 0 to 1 (TF values) or 0 to spline length (distance)
   - Calculate appropriate sampling points based on resolution parameter
   - For each sample point, get position, tangent, up-vector from the CurvySpline
   - Store all data in the SplineDataContainer arrays

5. **Add editor validation**:
   - Validate that the CurvySpline reference is not null
   - Validate that the resolution is positive and reasonable
   - Show warnings if spline has zero length or is not initialized

### Phase 3: Create Playback System

6. **Create CachedSplineMover component**:
   - Create a MonoBehaviour component with a SplineDataContainer reference
   - Include fields for movement speed, current TF/distance, movement direction
   - Implement Start and Update methods for automated movement
   - Add public methods for manual control of position

7. **Implement interpolation methods**:
   - Create method to find the closest two cached points for a given TF/distance
   - Use linear interpolation between points for smooth position calculation
   - Calculate tangent and up-vector interpolation similarly

8. **Implement movement controls**:
   - Support for TF-based movement (0-1 range)
   - Support for distance-based movement
   - Direction controls (forward/backward)
   - Clamping options (loop, clamp, ping-pong)

### Phase 4: Testing and Validation

9. **Create test scenes**:
   - Create a scene with a CurvySpline and a test object
   - Cache the spline data using the SplineDataCacher
   - Use CachedSplineMover to move an object along the cached data
   - Compare movement between original CurvySpline and cached data

10. **Performance testing**:
    - Compare performance between real-time spline calculations and cached data
    - Test with different resolution settings to find optimal balance
    - Verify that cached movement matches original spline path

### Phase 5: Advanced Features (Optional)

11. **Add event system**:
    - Implement events for when the object reaches specific TF values or distances
    - Add position triggers similar to the original Curvy system

12. **Optimize data structure**:
    - Consider using custom structs for position/tangent/up data to reduce memory
    - Implement optional compression of cached data for large splines

## Technical Considerations

### Memory Usage
- The system should balance accuracy with memory usage
- Higher resolution = more accuracy but more memory consumption
- Consider using fewer points for straight segments and more for curved segments (adaptive sampling)

### Interpolation Accuracy
- For smooth movement, ensure sufficient resolution is used when caching
- Consider cubic interpolation between points for higher accuracy than linear

### Thread Safety
- If implementing in a multithreaded context, ensure proper synchronization

### Data Consistency
- When original spline changes, cached data becomes invalid
- Consider adding versioning or checksum to detect when cache needs updating

## Expected Outcomes

1. Ability to cache any CurvySpline to a ScriptableObject asset
2. Smooth movement along cached spline data
3. Performance improvement over real-time spline calculations
4. Compatibility with existing CurvySpline features (closed/open, different interpolation types)
5. Flexibility in sampling resolution for different use cases

## Files to Create

1. `SplineDataContainer.cs` - ScriptableObject to hold cached data
2. `SplineDataCacher.cs` - Component to cache data from CurvySpline
3. `CachedSplineMover.cs` - Component to move objects using cached data
4. Editor scripts for custom inspectors and gizmos