using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.SpecialOrders;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Holds patches to make special orders less fragile.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SpecialOrderCrash
{
    [HarmonyPatch(nameof(SpecialOrder.GetSpecialOrder))]
    private static Exception? Finalizer(string key, ref SpecialOrder? __result, Exception? __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Detected invalid special order {key}.", LogLevel.Error);
            ModEntry.ModMonitor.Log(__exception.ToString());
            __result = null;
        }
        return null;
    }
}