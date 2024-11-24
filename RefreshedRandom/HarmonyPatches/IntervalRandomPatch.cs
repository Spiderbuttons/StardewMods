using HarmonyLib;

using StardewValley.Extensions;
using System.Text;
using RefreshedRandom.Framework.PRNG;

namespace RefreshedRandom.HarmonyPatches;
internal static class IntervalRandomPatch
{
    private static readonly ThreadLocal<byte[]> Buffer = new(() => new byte[32]);

    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Utility), nameof(Utility.TryCreateIntervalRandom)),
            prefix: new(typeof(IntervalRandomPatch), nameof(Prefix)));
    }

    private static bool Prefix(string interval, string key, ref Random? random, ref string? error, ref bool __result)
    {
        if (interval is null)
        {
            random = Random.Shared;
            error = "interval cannot be null";
            return false;
        }

        if (Game1.UseLegacyRandom)
        {
            return true;
        }

        try
        {

            if (interval.EqualsIgnoreCase("day") && ModEntry.Data is { } data)
            {
                byte[] buffer = Buffer.Value!;
                Span<byte> span = new(buffer);

                if (key is not null)
                {
                    BitConverter.TryWriteBytes(span, SeededXoshiroFactory.Hash(Encoding.UTF8.GetBytes(key)));
                }
                span = span[8..];

                BitConverter.TryWriteBytes(span, Game1.uniqueIDForThisGame);
                span = span[8..];

                BitConverter.TryWriteBytes(span, Game1.stats.DaysPlayed);
                span = span[4..];

                BitConverter.TryWriteBytes(span, data.LastSteps);
                span = span[4..];

                BitConverter.TryWriteBytes(span, data.LastMilliseconds);
                span = span[4..];

                BitConverter.TryWriteBytes(span, data.LastSeed);

                error = null;
                random = SeededXoshiroFactory.Generate(buffer);
                ModEntry.ModMonitor.VerboseLog($"Interval random requested: {string.Join('-', buffer.Select(a => a.ToString("X2")))}");
                __result = true;
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while trying to override the interval random generator: {ex}", LogLevel.Error);
        }

        return true;
    }
}
