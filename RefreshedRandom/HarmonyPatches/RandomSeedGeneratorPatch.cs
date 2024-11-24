using HarmonyLib;
using RefreshedRandom.Framework.PRNG;

namespace RefreshedRandom.HarmonyPatches;

/// <summary>
/// Patches on <see cref="Utility"/>'s Random generator.
/// </summary>
internal static class RandomSeedGeneratorPatch
{
    private static readonly ThreadLocal<byte[]> Buffer = new(() => new byte[40]);

    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Utility), nameof(Utility.CreateRandomSeed)),
            prefix: new(typeof(RandomSeedGeneratorPatch), nameof(PrefixCreateRandomSeed))
            );

        harmony.Patch(
            AccessTools.Method(typeof(Utility), nameof(Utility.CreateRandom)),
            prefix: new(typeof(RandomSeedGeneratorPatch), nameof(PrefixCreateRandom))
            );
    }

    private static bool PrefixCreateRandomSeed(double seedA, double seedB, double seedC, double seedD, double seedE, ref int __result)
    {
        if (Game1.UseLegacyRandom)
        {
            return true;
        }

        try
        {
            ulong seed = SeededXoshiroFactory.Hash(GetSeed(seedA, seedB, seedC, seedD, seedE));
            unchecked
            {
                __result = (int)(seed ^ (seed >> 32));
            }
            ModEntry.ModMonitor.VerboseLog($"Seed generation requested: {__result}");

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while attempting to override random seed generation: {ex}", LogLevel.Error);
            return true;
        }
    }

    private static bool PrefixCreateRandom(double seedA, double seedB, double seedC, double seedD, double seedE, ref Random __result)
    {
        if (Game1.UseLegacyRandom)
        {
            return true;
        }

        try
        {
            byte[] buff = GetSeed(seedA, seedB, seedC, seedD, seedE);
            __result = SeededXoshiroFactory.Generate(buff);
            ModEntry.ModMonitor.VerboseLog($"Random generation requested: {string.Join('-', buff.Select(a => a.ToString("X2")))}");

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while attempting to override random generation: {ex}", LogLevel.Error);
            return true;
        }
    }

    // write the whole seed to a byte array.
    private static byte[] GetSeed(double seedA, double seedB, double seedC, double seedD, double seedE)
    {
        byte[] buff = Buffer.Value!;
        Span<byte> span = new(buff);

        BitConverter.TryWriteBytes(span, seedA);
        span = span[8..];
        BitConverter.TryWriteBytes(span, seedB);
        span = span[8..];
        BitConverter.TryWriteBytes(span, seedC);
        span = span[8..];
        BitConverter.TryWriteBytes(span, seedD);
        span = span[8..];
        BitConverter.TryWriteBytes(span, seedE);

        return buff;
    }
}
