﻿using AtraShared.Integrations.Interfaces;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.Integrations;

/// <summary>
/// Class that holds the method that generates the CP tokens for this mod.
/// </summary>
internal class GenerateCPTokens
{
    /// <summary>
    /// Adds the CP tokens for this mod.
    /// </summary>
    /// <param name="manifest">This mod's manifest.</param>
    public static void AddTokens(IManifest manifest)
    {
        if (Globals.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher") is not IContentPatcherAPI api)
        {
            return;
        }

        api.RegisterToken(manifest, "IslandOpen", () =>
        {
            if ((Context.IsWorldReady || SaveGame.loaded is not null)
                && Game1.getLocationFromName("IslandSouth") is IslandSouth island)
            {
                return new[] { island.resortOpenToday.Value.ToString() };
            }

            return null;
        });
    }
}