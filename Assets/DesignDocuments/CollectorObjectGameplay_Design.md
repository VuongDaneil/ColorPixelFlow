PaintingGridObject.cs METHODS SUPPORT GAMEPLAY.

With grid object (PaintingGridObject) existed in scene, i have an object that run around it, this object call ColorPixelsCollector.cs.
* ColorPixelsCollector.cs: Object that run around grid object and destroy pixels with specific color by shooting the pixel:
	- MovementHandle (CachedTransformPathMover.cs): movement handler script.
	- CurrentGrid (PaintingGridObject.cs): current target grid object.
	- BulletCapacity (int): max number of bullet.
	- BulletLeft (int): bullet left.
	- ColorCodeToDestroy (string): pixel's color that this object can shoot.
	- CollectorColor (string): color of this object based on pixel's color.
	- DetectionRadius (float): Distance threshold for detecting and shooting nearby pixels.
	- IsCollectorActive (bool): Flag indicating if the collector is currently active.

* Methods that already existed in PaintingGridObject.cs:
1. List<PaintingPixel> SelectOutlinePixels(): return all the outer margin PaintingPixel that not destroyed yet.
2. List<PaintingPixel> SelectOutlinePixelsWithColor(String): return all the outer margin PaintingPixel that has this color and not destroyed yet.
3. int GetRemainingPixels(): return number of PaintingPixel that not destroyed.

How this work:
	- The collector (ColorPixelsCollector) object will go around the grid (clockwise or counterclockwise direction) and destroy all the pixel (PaintingPixel) that at outer of the grid and has the same color code (PaintingPixel.colorCode) as the colector's. Everytime it go pass a pixel (using pixel's worldPos for calculate) that not destroyed yet and has the same color as it's - it will shoot and destroy the pixel.
	- My idea: the collector when start moving will calculate the pixel that it will be able to destroy by using SelectOutlinePixelsWithColor(), when the grid changes (it's pixels got destroyed) it will invoke an event (GameplayEventsManager.onGridPixelsChanged) for the collector to re-calculate the pixel it can destroy.

* How to check pass a pixel: we use collector's position compare to pixel's pixtion, if the collector moving horizontaly we just need to check it's X axis position, when moving verticaly we just need to check Z axis position value. Shouldnt use Vector3.distance.

* Requirement for a pixel to be able to be destroyed: 
	1. Between target pixel and collector must be no obstacle - no pixel with different color:
		+ Example 1: when moving horizontal at below line (Colectior's position Z < 0): Pixel at (0, -14)  (column 0 - row -14) has the same color with the collector but the pixel between them (0, -15) has different color so collector cannot destroy the target pixel at row -14. The same logic applied for vertical moving.
		+ Example 2: when moving vertically at right side line (Colectior's position X > 0): Pixel at (12, -4) has the same color with the collector but the pixel between them (14, -4) has different color so collector cannot destroy the target pixel at column 12.
		- This obstacle check is now implemented to prevent destruction through different-colored pixels.

		- The collector moves in a cube-like path with:
			+ Horizontal paths: below and above:
				- Below: move left to right.
				- Above: move right to left.
			+ Vertical paths: right and left:
				- Right: move bottom to top.
				- Above: move top to bottom.

	2. Only destroy one pixel at one row, column at once.
		- Example: when moving horizontaly pass column 1, it destroy one of its pixel, after that it will ignore this column until it revisit this column later by switch to verticaly moving. This mean when moving in one direction the collector only able to destroy one pixel of one row/column - applied for both moving vertial and horizontal.
		- This limitation is now implemented by tracking which columns/rows have been processed in the current movement direction.
	3. Pixel destruction conditions:
		- Pixel must match the collector's ColorCodeToDestroy
		- No obstacle pixels (with different color) between collector and target
		- The row/column hasn't been processed in the current movement direction
		- Collector has bullets remaining (BulletLeft > 0)
		- Pixel is within detection radius based on movement direction
		- Pixel is an outline pixel (perimeter of the grid)
		- Pixel is not already destroyed

* Additional Implementation Details:
	- Movement direction detection: System detects when movement changes from horizontal to vertical or vice versa
	- Processed position tracking: Tracks which rows/columns have been processed in the current movement direction
	- Direction-based reset: When movement direction changes, the system resets the appropriate processed position tracking
	- Event synchronization: Processed positions are cleared when grid changes occur or collector is reset/activated
	- Bullet capacity system: Limits the total number of pixels that can be destroyed per collector cycle