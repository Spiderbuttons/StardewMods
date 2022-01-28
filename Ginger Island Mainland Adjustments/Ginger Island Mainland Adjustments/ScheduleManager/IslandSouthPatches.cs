﻿using System.Diagnostics.CodeAnalysis;
using GingerIslandMainlandAdjustments.Utils;
using HarmonyLib;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Patches for the IslandSouth class.
/// </summary>
[HarmonyPatch(typeof(IslandSouth))]
internal static class IslandSouthPatches
{
    /// <summary>
    /// Dictionary of NPCs and custom exclusions.
    /// </summary>
    /// <remarks>null is cache miss: reload if ever null.</remarks>
    private static Dictionary<NPC, string[]>? exclusions = null;

    /// <summary>
    /// Gets dictionary of NPCs and custom exclusions.
    /// </summary>
    /// <remarks>Cached, will reload automatically if not currently cached.</remarks>
    private static Dictionary<NPC, string[]> Exclusions
    {
        get
        {
            if (exclusions is null)
            {
                exclusions = AssetManager.GetExclusions();
            }
            return exclusions;
        }
    }

    /// <summary>
    /// Clears/resets the Exclusions cache.
    /// </summary>
    public static void ClearCache()
    {
        exclusions = null;
    }

   /// <summary>
   /// Override the vanilla schedules if told to.
   /// </summary>
   /// <returns>False to skip vanilla function, true otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(IslandSouth.SetupIslandSchedules))]
    public static bool OverRideSetUpIslandSchedules()
    {
        if (Globals.Config.UseThisScheduler)
        {
            Globals.ModMonitor.DebugLog("GI schedules being generated by mod.");
            try
            {
                GIScheduler.GenerateAllSchedules();
                return false;
            }
            catch (Exception ex)
            {
                Globals.ModMonitor.Log($"Errors generating ginger island schedules, defaulting to vanilla code\n\n{ex}");
            }
        }
        return true;
    }

    /// <summary>
    /// Extends CanVisitIslandToday for custom exclusions as well.
    /// </summary>
    /// <param name="npc">the NPC to check.</param>
    /// <param name="__result">True if the NPC can go to the island, false otherwise.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IslandSouth.CanVisitIslandToday))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Convention used by Harmony")]
    public static void ExtendCanGoToIsland(NPC npc, ref bool __result)
    {
        try
        {
            if (!__result)
            {
                if (npc.Name.Equals("Sandy", StringComparison.OrdinalIgnoreCase))
                {
                    __result = true; // let Sandy come to the resort!
                }
                else if (npc.Name.Equals("George", StringComparison.OrdinalIgnoreCase) || npc.Name.Equals("Evelyn", StringComparison.OrdinalIgnoreCase))
                {
                    __result = true; // let George & Evelyn come too!
                }
                else
                {
                    // already false in code, ignore me for everyone else
                    return;
                }
            }
            if (!Exclusions.ContainsKey(npc))
            { // I don't have an entry for you.
                return;
            }
            string[] checkset = Exclusions[npc];
            foreach (string condition in checkset)
            {
                if (Game1.dayOfMonth.ToString().Equals(condition, StringComparison.InvariantCultureIgnoreCase))
                {
                    __result = false;
                }
                else if (Game1.currentSeason.Equals(condition, StringComparison.InvariantCultureIgnoreCase))
                {
                    __result = false;
                }
                else if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals(condition, StringComparison.InvariantCultureIgnoreCase))
                {
                    __result = false;
                }
                else if ($"{Game1.currentSeason} {Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)}".Equals(condition, StringComparison.InvariantCultureIgnoreCase))
                {
                    __result = false;
                }
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in postfix for CanVisitIslandToday for {npc.Name}: \n\n{ex}", LogLevel.Warn);
        }
        return;
    }
}
