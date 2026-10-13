namespace Content.Shared._Floof.Paint;

/// <summary>
///     Raised on an entity when its color paint changes.
/// </summary>
public sealed class ColorPaintChangedEvent : EntityEventArgs
{
    [DataField]
    public Color OldColor;

    [DataField]
    public Color? NewColor;

    public ColorPaintChangedEvent(Color oldColor, Color? newColor)
    {
        OldColor = oldColor;
        NewColor = newColor;
    }
}
