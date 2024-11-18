using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace RefreshedRandom.HarmonyPatches;
internal static class GameLocationForagePatch
{

    private static int counter = 0;

    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.spawnObjects)),
            transpiler: new(typeof(GameLocationForagePatch), nameof(Transpiler)));
    }

    internal static void Reset() => Interlocked.Exchange(ref counter, 0);

    private static double GetValue() => Interlocked.Increment(ref counter);

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool found = false;
        foreach (var instruction in instructions)
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
