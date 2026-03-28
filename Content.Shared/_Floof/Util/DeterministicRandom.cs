namespace Content.Shared._Floof.Util;

public static class DeterministicRandom
{
    private const int LCG_MULTIPLIER = 1103515245;
    private const int LCG_INCREMENT = 12345;

    /// <summary>
    ///     Generates a deterministic unsigned pseudo-random positive number using a linear congruential generator.
    ///     Range: [0, max).
    /// </summary>
    public static int Prng(int seed, int max)
    {
        if (max == 0)
            return 0; // Avoid division by 0

        seed = (LCG_MULTIPLIER * seed + LCG_INCREMENT);
        return Math.Abs(seed % max);
    }

    /// <summary>
    ///     Generates a deterministic signed pseudo-random positive number using a linear congruential generator.
    ///     Range: [min, max).
    /// </summary>
    public static int Prng(int seed, int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max));
        var b = Prng(seed, (max - min));
        return min + b;
    }

    /// <summary>
    ///     Picks a random value from the list deterministically based on the seed.
    /// </summary>
    public static T Pick<T>(IList<T> list, int seed)
    {
        if (list.Count == 0)
            throw new IndexOutOfRangeException("List is empty");

        var idx = Prng(seed, 0, list.Count);
        return list[idx];
    }
}
