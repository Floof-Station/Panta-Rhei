namespace Content.Shared._Floof.Ropes;

public enum RopeSide
{
    Start,
    End,
}

public static class RopeSideExt
{
    public static RopeSide Opposite(this RopeSide side) =>
        side switch
        {
            RopeSide.End => RopeSide.Start,
            RopeSide.Start => RopeSide.End,
            _ => throw new ArgumentOutOfRangeException()
        };
}
