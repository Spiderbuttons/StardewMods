﻿using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using GrowableBushes.Framework;

using HarmonyLib;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.HarmonyPatches;

/// <summary>
/// Patches on bushes.
/// </summary>
[HarmonyPatch(typeof(Bush))]
internal static class BushPatches
{
    private static readonly Lazy<Func<Bush, float>> BushMaxShakeGetterLazy = new(
    () => typeof(Bush)
        .GetCachedField("maxShake", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldGetter<Bush, float>());

    private static readonly Lazy<Action<Bush, float>> BushMaxShakeSetterLazy = new(
    () => typeof(Bush)
        .GetCachedField("maxShake", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<Bush, float>());

    [HarmonyPostfix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(nameof(Bush.isDestroyable))]
    private static void PostfixIsDestroyable(Bush __instance, ref bool __result)
    {
        if (!__result)
        {
            try
            {
                if (ModEntry.Config.CanAxeAllBushes || __instance.modData?.ContainsKey(InventoryBush.BushModData) == true)
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed while attempting to override bush destroyability\n\n{ex}", LogLevel.Error);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch("shake")]
    private static bool PrefixShake(Bush __instance, bool doEvenIfStillShaking)
    {
        try
        {
            if (__instance.size.Value == Bush.walnutBush &&
                __instance.modData?.GetEnum(InventoryBush.BushModData, BushSizes.Small) == BushSizes.Walnut)
            {
                if (BushMaxShakeGetterLazy.Value(__instance) == 0 || doEvenIfStillShaking)
                {
                    BushMaxShakeSetterLazy.Value(__instance, MathF.PI / 128f);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while attempting to prevent shaking of walnut bush\n\n{ex}");
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Bush.dayUpdate))]
    private static void PostfixDayUpdate(Bush __instance, GameLocation environment)
    {
        try
        {
            if (!__instance.modData.ContainsKey(InventoryBush.BushModData))
            {
                return;
            }

            BushSizes size = __instance.modData.GetEnum(InventoryBush.BushModData, BushSizes.Small);
            switch (size)
            {
                case BushSizes.SmallAlt:
                {
                    __instance.tileSheetOffset.Value = 1;
                    __instance.SetUpSourceRectForEnvironment(environment);
                    break;
                }
                case BushSizes.Harvested:
                {
                    __instance.tileSheetOffset.Value = 0;
                    __instance.SetUpSourceRectForEnvironment(environment);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while overriding tileSheetOffset for specific bushes.\n\n{ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Bush.seasonUpdate))]
    private static void PostfixSeasonUpdate(Bush __instance, bool __result)
    {
        if (__result)
        {
            return;
        }

        try
        {
            if (!__instance.modData.ContainsKey(InventoryBush.BushModData))
            {
                return;
            }

            BushSizes size = __instance.modData.GetEnum(InventoryBush.BushModData, BushSizes.Small);
            switch (size)
            {
                case BushSizes.SmallAlt:
                {
                    __instance.tileSheetOffset.Value = 1;
                    __instance.setUpSourceRect();
                    break;
                }
                case BushSizes.Harvested:
                {
                    __instance.tileSheetOffset.Value = 0;
                    __instance.setUpSourceRect();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while overriding tileSheetOffset for specific bushes.\n\n{ex}");
        }
    }
}