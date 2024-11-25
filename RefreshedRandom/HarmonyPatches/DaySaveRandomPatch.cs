namespace RefreshedRandom.HarmonyPatches;
using HarmonyLib;
using RefreshedRandom.Framework.PRNG;

/// <summary>
/// Patches CreateDaySaveRandom to increase entropy.
/// </summary>
internal static class DaySaveRandomPatch
{
    private static readonly ThreadLocal<byte[]> Block = new(() => new byte[48]);

    /// <summary>
    /// Applies patches for this class.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Utility), nameof(Utility.CreateDaySaveRandom)),
            prefix: new(typeof(DaySaveRandomPatch), nameof(Prefix)));
    }

    private static bool Prefix(double seedA, double seedB, double seedC, ref Random __result)
    {
        if (ModEntry.Data is not { } data)
        {
            return true;
        }

        if (Game1.UseLegacyRandom)
        {
            return true;
        }

        try
        {
            byte[] buff = Block.Value!;
            Span<byte> span = new Span<byte>(buff);

            BitConverter.TryWriteBytes(span, Game1.stats.DaysPlayed);
            span = span[4..];

            BitConverter.TryWriteBytes(span, Game1.uniqueIDForThisGame);
            span = span[8..];

            BitConverter.TryWriteBytes(span, seedA);
            span = span[8..];

            BitConverter.TryWriteBytes(span, seedB);
            span = span[8..];

            BitConverter.TryWriteBytes(span, seedC);
            span = span[8..];

            BitConverter.TryWriteBytes(span, data.LastMilliseconds);
            span = span[4..];

            BitConverter.TryWriteBytes(span, data.LastSteps);
            span = span[4..];

            BitConverter.TryWriteBytes(span, data.LastSeed);

            ModEntry.ModMonitor.VerboseLog($"Requested day save random with seed {string.Join('-', buff.Select(a => a.ToString("X2")))}");

            __result = SeededXoshiroFactory.Generate(buff);
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while attempting to override a day save random: {ex}.", LogLevel.Error);
            return true;
        }
    }
}