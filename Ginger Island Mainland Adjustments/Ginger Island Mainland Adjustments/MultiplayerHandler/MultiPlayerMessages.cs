﻿using AtraShared.Caching;
using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.AssetManagers;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.MultiplayerHandler;

/// <summary>
/// Class to handle multiplayer shared state.
/// </summary>
[HarmonyPatch(typeof(NPC))]
public static class MultiplayerSharedState
{
    private const string SCHEDULEMESSAGE = "GIMAScheduleUpdateMessage";

    private static bool hasPlayerSeenEvent;
    private static int lastCheckedTicks;

    /// <summary>
    /// Gets Pam's current schedule string.
    /// </summary>
    internal static string? PamsSchedule { get; private set; }

    private static TickCache<bool> HasSeenEvent = new(static () => Game1.player.eventsSeen.Contains(AssetEditor.PAMEVENT));

    /// <summary>
    /// Updates entry for Pam's schedule whenever a person joins in multiplayer.
    /// </summary>
    /// <param name="e">arguments.</param>
    internal static void ReSendMultiplayerMessage(PeerConnectedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady
            && Game1.getCharacterFromName("Pam") is NPC pam
            && pam.TryGetScheduleEntry(pam.dayScheduleName.Value, out string? rawstring)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(pam, SDate.Now(), rawstring, out string redirectedstring))
        {
            PamsSchedule = redirectedstring;
            Globals.ModMonitor.Log($"Grabbing Pam's rawSchedule for phone: {redirectedstring}");
            Globals.Helper.Multiplayer.SendMessage(redirectedstring, SCHEDULEMESSAGE, modIDs: new[] { Globals.Manifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });
        }
    }

    /// <summary>
    /// Updates entry for Pam's schedule from a multiplayer message.
    /// </summary>
    /// <param name="e">arguments.</param>
    internal static void UpdateFromMessage(ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == Globals.Manifest.UniqueID && e.Type == SCHEDULEMESSAGE)
        {
            PamsSchedule = e.ReadAs<string>();
            Globals.ModMonitor.Log($"Recieved Pam's schedule {PamsSchedule}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void PostfixGetMasterSchedule(NPC __instance)
    {
        try
        {
            if (Context.IsMainPlayer && HasSeenEvent.GetValue() && __instance?.Name.Equals("Pam", StringComparison.OrdinalIgnoreCase) == true
                && __instance.TryGetScheduleEntry(__instance.dayScheduleName.Value, out string? rawstring)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(__instance, SDate.Now(), rawstring, out string redirectedstring))
            {
                PamsSchedule = redirectedstring;
                Globals.ModMonitor.DebugOnlyLog($"Grabbing Pam's rawSchedule for phone: {redirectedstring}");
                Globals.Helper.Multiplayer.SendMessage(redirectedstring, SCHEDULEMESSAGE, modIDs: new[] { Globals.Manifest.UniqueID });
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in postfixing get master schedule to get Pam's schedule.\n\n{ex}", LogLevel.Error);
        }
    }
}