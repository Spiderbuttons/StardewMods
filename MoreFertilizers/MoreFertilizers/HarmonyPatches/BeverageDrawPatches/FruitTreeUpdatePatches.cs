﻿using HarmonyLib;

using Microsoft.Xna.Framework;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BeverageDrawPatches;

/// <summary>
/// Patches fruit tree's draw method to add a TAS.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreeUpdatePatches
{
    [HarmonyPatch(nameof(FruitTree.draw))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(FruitTree __instance)
    {
        if (__instance.modData.ContainsKey(CanPlaceHandler.MiraculousBeverages) && Game1.random.Next(1024) == 0)
        {
            __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                Game1.mouseCursorsName,
                new Rectangle(372, 1956, 10, 10),
                new Vector2(
                    (__instance.currentTileLocation.X * 64f) + Game1.random.Next(-64, 96),
                    (__instance.currentTileLocation.Y * 64f) + Game1.random.Next(-256, -64)),
                flipped: false,
                0.002f,
                Color.LimeGreen)
            {
                alpha = 0.75f,
                motion = new Vector2(0f, -0.5f),
                interval = 99999f,
                layerDepth = 1f,
                scale = 2f,
                scaleChange = 0.01f,
            });
        }
    }
}
