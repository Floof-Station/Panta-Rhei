using System.Runtime.CompilerServices;

namespace Content.Shared._Floof.Util;

// Seriously, why isn't this shit part of RobustToolbox?
public static class StringHelpers
{
    /// <summary>
    ///     Takes up to n first chars from the string if it's longer than n chars. Returns the original stirng if it is shorter or equal to n characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TakeChars(this string s, int n)
    {
        if (s.Length <= n)
            return s;

        return s.Substring(0, n);
    }
}
