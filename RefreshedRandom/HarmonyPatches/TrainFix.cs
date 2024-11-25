using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Netcode;

using StardewValley.Extensions;
using StardewValley.Locations;

namespace RefreshedRandom.HarmonyPatches;

/// <summary>
/// Patches to fix up the train.
/// </summary>
internal static class TrainFix
{
    private const string MOD_DATA_KEY = "atravita.RefreshedRandom.Train";

    /// <summary>
    /// Applies patches for this class.
    /// </summary>
    /// <param name="harmony">harmony instance.</param>
    internal static void ApplyPatches(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Railroad), "ResetTrainForNewDay"),
            prefix: new(typeof(TrainFix).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic), priority: Priority.Last)
            );
    }

    /*
     * Because we break how daily randoms work, rewrite the train code to actually just remember if a train happened yesterday
     */

    private static bool Prefix(Railroad __instance, NetBool ___hasTrainPassed, ref int ___trainTime, double ___DailyTrainChance)
    {
        try
        {
            ___hasTrainPassed.Value = false;
            ___trainTime = -1;

            if (__instance.modData.ContainsKey(MOD_DATA_KEY))
            {
                __instance.modData.Remove(MOD_DATA_KEY);
                return false;
            }

            if (!Game1.isLocationAccessible("Railroad"))
            {
                return false;
            }

            Random random = Utility.CreateDaySaveRandom();
            if (random.NextBool(___DailyTrainChance))
            {
                int minutes = random.Next(54, 108) * 10;
                ___trainTime = Utility.ConvertMinutesToTime(minutes);
                __instance.modData[MOD_DATA_KEY] = "1";
            }

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while trying to override train logic: {ex}", LogLevel.Error);
        }

        return true;
    }
}
