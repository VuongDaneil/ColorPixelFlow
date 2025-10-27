using System;

public static class GameplayEventsManager
{
    public static Action<PaintingPixel> OnAPixelDestroyed;
    public static Action<PaintingGridObject> OnGridObjectChanged;
    public static Action<PaintingGridObject> OnPaintingInitializeDone;

    #region _mechanic
    public static Action OnCollectAKey;
    #endregion
}