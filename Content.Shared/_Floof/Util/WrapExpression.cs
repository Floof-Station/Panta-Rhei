using System.Runtime.CompilerServices;
using Content.Shared.Ghost;

namespace Content.Shared._Floof.Util;

public static class WrapExpression
{
    /// <summary>
    ///     Executes <paramref name="action"/> and returns <paramref name="value"/> unconditionally. Mainly for inline usage in switch expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // praying the compiler can inline the lambda
    public static T Return<T>(T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
