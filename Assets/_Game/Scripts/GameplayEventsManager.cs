using System;

public static class GameplayEventsManager
{
    // Event that fires when pixels in a grid change (created, destroyed, color changed)
    public static Action<PaintingGridObject> OnGridPixelsChanged;
    public static Action<PaintingPixel> OnAPixelDestroyed;

    #region _mechanic
    public static Action OnCollectAKey;
    #endregion
}