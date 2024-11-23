using System.Reflection.Emit;

using HarmonyLib;

namespace RefreshedRandom.HarmonyPatches;

/// <summary>
/// A patch on game location, to make forage more random.
/// </summary>
internal static class GameLocationForagePatch
{
    private static int counter = 0;

    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">the mod's harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(GameLocation), nameof(GameLocation.spawnObjects)),
            transpiler: new(typeof(GameLocationForagePatch), nameof(Transpiler)));
    }

    /// <summary>
    /// Resets the counter.
    /// </summary>
    internal static void Reset() => Interlocked.Exchange(ref counter, 0);

    private static double GetValue() => Interlocked.Increment(ref counter);

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool found = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!found && instruction.opcode == OpCodes.Ldc_R8 && instruction.operand is double v && v == 0.0)
            {
                found = true;
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameLocationForagePatch), nameof(GetValue)))
                                        .WithLabels(instruction.labels)
                                        .WithBlocks(instruction.blocks);
            }
            else
            {
                yield return instruction;
            }
        }

        if (!found)
        {
            ModEntry.ModMonitor.Log($"Could not find target to edit. The mod may not work as intended.", LogLevel.Error);
        }
    }
}
