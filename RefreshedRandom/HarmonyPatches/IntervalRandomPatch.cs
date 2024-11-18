using HarmonyLib;

using StardewValley.Extensions;

namespace RefreshedRandom.HarmonyPatches;
internal static class IntervalRandomPatch
{
    private static readonly int[] block = new int[6];

    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(Utility), nameof(Utility.TryCreateIntervalRandom)),
            prefix: new(typeof(IntervalRandomPatch), nameof(Prefix)));
    }

    private static bool Prefix(string interval, string key, ref Random? random, ref string? error)
    {
        if (interval is null)
        {
            random = Random.Shared;
            error = "interval cannot be null";
            return false;
        }

        if (interval.EqualsIgnoreCase("day") && ModEntry.Data is { } data)
        {
            block[0] = key is null ? 0 : Game1.hash.GetDeterministicHashCode(key);
            block[1] = (int)Game1.uniqueIDForThisGame;
            block[2] = (int)(Game1.uniqueIDForThisGame << 32);
            block[3] = (int)Game1.stats.DaysPlayed;
            block[4] = data.LastDayMilliseconds;
            block[5] = data.LastDaySteps;

            int seed = Game1.hash.GetDeterministicHashCode(block);
            error = null;
            random = new Random(seed);

            return false;
        }

        return true;
    }
}
