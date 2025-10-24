COLOR COLECTOR SQUAD IN A LEVEL.

For color colector object, read: ColorCollectorObjectSystem.md and CollectorObjectGameplay_Design.md.

## Overview:
- A level will have a number of color collector objects (ColorPixelsCollector.cs) called a squad, each of them can collect one color from PaintingPixel. In scene, a squad will be formed in a formation, formation contains many columns, each column contains number of collectors.
## Technical:
* Already had:
	- Collector object prefab.
	- ColorPixelsCollector.cs: a script control a collector in scene, this existed in collector prefab.
* To do:
	- A class called SingleColorCollectorObject: to contain infomation about the collector:
		+ OriginalIndex (int): Original index of the collector in its squad.
		+ ColorCode (string): Color code that this collector can collect.
		+ Bullets (int): number of bullets, each of them can collect one color.
		+ Locked (bool): locked or not.
		+ Hidden (bool): hide informations about color and bullet from player.
		+ ConnectedCollectorsIndex (List<int>): original index of other collectors that connected to this collector.

	- A ScriptableObject called LevelColorCollectorsConfig.cs to store SingleColorCollectorObject squad information:
		+ CollectorSetups (List<SingleColorCollectorObject>): list of SingleColorCollectorObject in a level.
		+ NumberOfColumns (int): this squad's number of column.

	- Modify ColorPixelsCollector.cs to have a int field for OriginalIndex when it spawned using infomations in a SingleColorCollectorObject. And a bool name Available, this bool return true if the collector still has bullets left or false when run ut of bullet.

	- A class named CollectorColumn: contains list ColorPixelsCollector in a row.

	- A script call LevelCollectorsSystem.cs to setup collectors from a LevelColorCollectorsConfig.cs asset.

	- A script called LevelCollectorsConfigSetup.cs to setup a LevelColorCollectorsConfig.cs asset.

 ## LevelCollectorsSystem.cs:
 	- This script purpose is to setup collectors squad from a LevelColorCollectorsConfig.cs asset to game's scene.

 	- Input:
 		+ ColorPalette (ColorPalleteData): The color source that collector will get color from by using colorCode.
 		+ CurrentLevelCollectorsConfig (LevelColorCollectorsConfig).
 		+ FormationCenter (Transform): center of the formation, formation's columns will aligns to this center.
 		+ SpaceBetweenColumns (float): space between each column.
 		+ SpaceBetweenCollectors (float): space between each collector in a column.
 		+ CollectorPrefab (GameObject): collector prefab to spawn.
 		+ CollectorContainer (Transform): collector's parent to contain collector gameobject.
 	- Output:
 		+ Formation of ColorPixelsCollector, stand in columns. I want to split CollectorSetups into multiple columns in a row-major order, so that the elements are filled horizontally first.
 			- Example: Given a list like [1, 2, 3, 4, 5, 6, 7, 8, 9], I want to arrange it into 3 columns so that the items are distributed horizontally:
							1 2 3
							4 5 6
							7 8 9
				Each row contains 3 elements. Column 1: 1-4-7; Column 2: 2-5-8; Column 3: 7-8-9.
 		+ CurrentCollectors (List<ColorPixelsCollector>): spawned collectors in current config.
 		+ CollectorColumns (List<CollectorColumn>): List of columns created.

 ## LevelCollectorsConfigSetup.cs:
 	- Has editor class to interact.
 	- This script purpose is to setup new LevelColorCollectorsConfig.cs asset or modify existed one.
 	- Use LevelCollectorsSystem gameobject to preview the config file.
 	- Can create, update, load LevelColorCollectorsConfig asset. Can choose name, path to save.
 	- Can load from input LevelColorCollectorsConfig asset, preview using LevelCollectorsSystem.
 	- Can import from LevelColorCollectorsConfig into a LevelColorCollectorsConfig config by import LevelColorCollectorsConfig.CurrentCollectors.

 ## Config from a PaintingConfig.cs:
 	- In LevelCollectorsConfigSetup.cs.
 	- In a PaintingConfig asset, we have all the PaintingPixel that in a level, include: Pixels and PaintingPixel from PixelCovered in PipeSetups. Our job is:
 		1. Because PaintingConfig.Pixels and PipeSetups.PixelCovered has deffirent type, we have to convert PaintingConfig.Pixels type (PaintingPixelConfig) into same type as PipeSetups.PixelCovered type (PaintingPixel). This is temporary step.
 		2. Get all pixels from each outline, example: size is 20 x 30 (20 column x 30 row), we get all pixels from all four edge based on row and column number, first time we get all pixel in column -10 and column 9, row -15 and row 14 - that a outline, repeat it until we got number of list outline pixels.
 		3. With each of outline, we category them based on PaintingPixel. With each color we count number of pixels that has that color (for PaintingPixel that has Hearts > 1 then we count them as that number of pixel instead of just one). We save those categories along with theirs outline.
 		4. With each outline and its categories of <ColorCode, number of pixels>, we create SingleColorCollectorObjects based on that, with number of pixels equals to SingleColorCollectorObjects.Bullets, but we need to clamp max bullet/pixel can a SingleColorCollectorObjects has (can change in inspector).
 			- Example: first outline has 30 Red pixels, 25 Green pixels and 20 Blue pixel, one SingleColorCollectorObjects can has max 20 bullets. So we have:
 				-> 	1 Collector with 20 bullet and ColorCode = Red
 					1 Collector with 10 bullet and ColorCode = Red
 					1 Collector with 20 bullet and ColorCode = Green
 					1 Collector with 05 bullet and ColorCode = Green
 					1 Collector with 20 bullet and ColorCode = Blue
 		5. Save all SingleColorCollectorObjects to configAsset.

 	- Has editor function to execute this function, display popup warning before confirm to execute.
 	- Implementation: The LevelCollectorsConfigSetup component now has a GenerateCollectorsFromPaintingConfig method that combines pixels from both PaintingConfig.Pixels and PipeSetups.PixelCovered, converts PaintingPixelConfig to PaintingPixel with default Hearts=1, extracts outline pixels by moving from outermost to innermost bounds, categorizes pixels by color code with hearts counted as multipliers, creates collectors with bullets clamped by MaxBulletPerCollector, and replaces the entire CollectorSetups list in the config asset.
