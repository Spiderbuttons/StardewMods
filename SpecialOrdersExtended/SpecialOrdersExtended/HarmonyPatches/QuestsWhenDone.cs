﻿using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using StardewValley.Menus;

namespace SpecialOrdersExtended.HarmonyPatches;

/// <summary>
/// Adds a patch to allow players to pick the other quest XD.
/// </summary>
internal static class QuestsWhenDone
{
    private const string Omega = "\u03A9"; // using this to mark which one was picked.
    private const string left = $"{Omega}left";
    private const string right = $"{Omega}right";

    [MethodImpl(TKConstants.Hot)]
    private static bool FinishedAllQuestsOfType(SpecialOrdersBoard board)
        => ModEntry.Config.AllowNewQuestWhenFinished && Game1.player.team.specialOrders.All((quest) => quest.orderType.Value != board.GetOrderType());

    // inject saving which quest was picked.
    // need to avoid StopRugRemoval's safety feature.
    [MethodImpl(TKConstants.Hot)]
    private static void TrackQuestOfType(SpecialOrdersBoard board, string str)
        => Game1.player.team.acceptedSpecialOrderTypes.Add(board.GetOrderType() + str);


}