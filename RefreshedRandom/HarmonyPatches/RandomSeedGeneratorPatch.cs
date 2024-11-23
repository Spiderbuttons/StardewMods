using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RefreshedRandom.Framework;

using StardewValley;

namespace RefreshedRandom.HarmonyPatches;
internal static class RandomSeedGeneratorPatch
{
    private static ThreadLocal<byte[]> buffer = new(() => new byte[40]);

    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Utility), nameof(Utility.CreateRandomSeed)),
            prefix: new(typeof(RandomSeedGeneratorPatch), nameof(PrefixCreateRandomSeed))
            );
    }

    private static bool PrefixCreateRandomSeed(double seedA, double seedB, double seedC, double seedD, double seedE, ref int __result)
    {
        try
        {
            ulong seed = SeededXoshiroFactory.Hash(GetSeed(seedA, seedB, seedC, seedD, seedE));
            unchecked
            {
                __result = (int)(seed ^ (seed >> 32));
            }
            ModEntry.ModMonitor.VerboseLog($"Seed generation requested, was {__result}");

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while attempting to override random seed generation: {ex}", LogLevel.Error);
            return true;
        }
    }

    // write the whole seed to a byte array.
    private static byte[] GetSeed(double seedA, double seedB, double seedC, double seedD, double seedE)
    {
        var buff = buffer.Value!;
        var span = new Span<byte>(buff);

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
