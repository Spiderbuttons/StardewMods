
namespace RefreshedRandom.HarmonyPatches;
using HarmonyLib;

internal static class DaySaveRandomPatch
{
    private static readonly int[] block = new int[8];

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

        block[0] = (int)Game1.stats.DaysPlayed;
        block[1] = (int)Game1.uniqueIDForThisGame;
        block[2] = (int)(Game1.uniqueIDForThisGame << 32);
        block[3] = (int)seedA;
        block[4] = (int)seedB;
        block[5] = (int)seedC;
        block[6] = data.LastDayMilliseconds;
        block[7] = data.LastDaySteps;

        var seed = Game1.hash.GetDeterministicHashCode(block);
        __result = new Random(seed); // todo find better random.
        return false;
    }
}