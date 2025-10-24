PaintingGridObject COLOR PIXEL SELECTOR.

in PaintingGridObject.cs, i need those methods:
1. List<PaintingPixel> SelectOutlinePixels(): return all the outer margin PaintingPixel that not destroyed yet.
2. List<PaintingPixel> SelectOutlinePixelsWithColor(string colorCode): return all the outer margin PaintingPixel that has this color code and not destroyed yet.
3. int GetRemainingPixels(): return number of PaintingPixel that not destroyed.

* The goal is to archive good performance so you can suggest changes in they way data store.