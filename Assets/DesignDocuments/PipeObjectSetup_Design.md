Pipe special object setup script: PipeObjectConfigSetup.cs.

Pipe object spawn in grid object. This pipe can be spawn in a row or a column, if spawn in a row it will be sideway, if spanwn in column it will be vertical.

## Require:
1. Class PipeObjectSetup represent for one pipe:
	- PixelCovered (List <PaintingPixel>): Grid pixels that pipe lie on (sort from head to tail).
	- ColorCode (string): PipeColorCode.
2. Modify PaintingConfig.cs:
	- Modify to contain PipeObjectSetup objects setup as List<PipeObjectSetup>.
3. Modify PaintingGridObject.cs:
	- Modify to contain Pipe objects setup as List<PipeObject>.
4. PipeObject.cs: mono behavior reprensent a pipe in scene.
	- PipeHead (Transform): head transform;
	- PipeBodyParts (List<Transform>): all pipe body parts include tail sorted from head to tail.
## Setup Idea:
Script name: PipeObjectConfigSetup.cs has:
	- List <PipeObjectSetup>: to store all the pipe that being setup.
Input:
	- GridObject (PaintingGridObject.cs).
	- StartPixel (PaintingPixel.cs): Pixel in grid that start this pipe (head).
	- EndPixel (PaintingPixel.cs): Pixel in grid that start this pipe (tail).
	- PipeHeadPrefab (GameObject): Head of pipe, spawn at head pixel position.
	- PipeBodyPrefab (Gameobject): Body part of pipe, spawn between head and tail.
	- PipeTailPrefab (GameObject): Tail of pipe, spawn at tail pixel position.
	- ColorCode (string): Color of the pipe.
	- colorPalette (ColorPalleteData): The color source that pipe will get from.
Output:
	- A pipe GameObject with Head object as parent and childs are body part and tail, spawn in scene. The head prefab already has PipeObject.cs, just need to get it then setup.
	- PipeObjectSetup add to List <PipeObjectSetup>;

## Design changes:
	- Pipe are object that spawn outside the grid, almost as a border. Logic still like old one but some changes:
		+ When spawn, instead of spawn at PaintingPixel worldPos, it spawn at it but at outer line with an int parameter like PipeSpaceFromGrid. Example: when spawn at StartPixel and EndPixel, if it a row with row index = -15, then the pipe will spawned at row -16 if PipeSpaceFromGrid = 1, same for other directions (calculate space using pixelArrangeSpace). Create new PaintingPixelComponent and PaintingPixel for those new Pixel we created (each part of the pipe is a PaintingPixel).
		+ PixelCovered from PipeObjectSetup now contain new PaintingPixel objects that just created. In PaintingGridObject.cs, we create a new List<PaintingPixel> name PipeObjectsPixels to cointain those new PaintingPixel objects.

## Interact:
	- In a PipeObject, when a PaintingPixel in a pipe destroyed, it shorten the pipe by move the body parts and tail toward the head, once a body part inside the head it will be dissapeared. The head will be the last to dissapear.
	- Example:
		+ Before:
			Head-bodypart1-bodypart2-bodypart3-bodypart4-Tail
		+ After one PaintingPixel destroyed: bodypart1 and the rest move until bodypart1 inside the head then dissapear.
			Head-bodypart2-bodypart3-bodypart4-Tail
	- A method `HandlePipeShortening(PaintingPixel destroyedPixel)` was created in PipeObject.cs to handle the shorten pipe job using DOTween for animations.
	- The `PaintingPixelsCovered` list remains unchanged when parts are destroyed to preserve the original pipe structure data.
	- Parts are removed when their position matches the head's position exactly.