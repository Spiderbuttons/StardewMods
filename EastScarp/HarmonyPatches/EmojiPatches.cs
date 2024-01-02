﻿namespace EastScarp.HarmonyPatches;

using System.Runtime.CompilerServices;

using EastScarp.Models;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.Menus;

/// <summary>
/// Handles loading custom emoji.
/// </summary>
[HarmonyPatch(typeof(SpecialOrdersBoard))]
internal static class CustomEmoji
{
    private static readonly HashSet<string> Failed = []; // hashset of failed loads.
    private static readonly Dictionary<string, KeyValuePair<Texture2D, Rectangle>> Cache = [];

    private static IGameContentHelper parser = null!;

    /// <summary>
    /// Initializes the asset cache.
    /// </summary>
    /// <param name="gameContentHelper">Game content helper.</param>
    internal static void Init(IGameContentHelper gameContentHelper) => parser = gameContentHelper;

    /// <summary>
    /// Handles invalidations.
    /// </summary>
    /// <param name="assets">IReadOnly set of assetnames.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(AssetManager.EmojiOverride))
        {
            Cache.Clear();
        }
        if (assets is not null && Failed.Count > 0)
        {
            foreach (IAssetName a in assets)
            {
                Failed.Remove(a.BaseName);
            }
        }
    }

    /// <summary>
    /// Removes paths from the failed texture load cache if someone readies them.
    /// </summary>
    /// <param name="e">Asset event args.</param>
    internal static void Ready(AssetReadyEventArgs e)
    {
        Failed.Remove(e.NameWithoutLocale.BaseName);
    }

    [UsedImplicitly]
    [HarmonyPatch("GetPortraitForRequester")]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Postfix(ref KeyValuePair<Texture2D, Rectangle>? __result, string requester_name)
    {
        if (requester_name is null)
        {
            return;
        }
        try
        {
            __result ??= GetEntry(requester_name);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding emoji", ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static KeyValuePair<Texture2D, Rectangle>? GetEntry(string requesterName)
    {
        if (Cache.TryGetValue(requesterName, out KeyValuePair<Texture2D, Rectangle> entry))
        {
            if (!entry.Key.IsDisposed)
            {
                return entry;
            }
            else
            {
                Cache.Remove(requesterName);
            }
        }

        Dictionary<string, EmojiData>? asset = Game1.temporaryContent.Load<Dictionary<string, EmojiData>>(AssetManager.EmojiOverride.BaseName);

        if (asset.TryGetValue(requesterName, out EmojiData? data))
        {
            IAssetName texLoc = parser.ParseAssetName(data.AssetName);
            if (Failed.Contains(texLoc.BaseName))
            {
                return null;
            }

            try
            {
                Texture2D? tex = Game1.content.Load<Texture2D>(texLoc.BaseName);
                Rectangle loc = new (data.Location, new (9, 9));
                if (tex.Bounds.Contains(loc))
                {
                    KeyValuePair<Texture2D, Rectangle> kvp = new (tex, loc);
                    Cache[requesterName] = kvp;
                    return kvp;
                }
                else
                {
                    ModEntry.ModMonitor.Log($"{data} appears to be requesting an out of bounds rectangle.", LogLevel.Warn);
                }
            }
            catch (ContentLoadException)
            {
                Failed.Add(texLoc.BaseName);
                ModEntry.ModMonitor.Log($"'{data.AssetName}' could not be found.", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Failed.Add(texLoc.BaseName);
                ModEntry.ModMonitor.LogError($"loading {data.AssetName}", ex);
            }
        }

        return null;
    }
}
