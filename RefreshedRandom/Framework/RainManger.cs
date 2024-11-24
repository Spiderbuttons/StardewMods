using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RefreshedRandom.Framework.PRNG;

namespace RefreshedRandom.Framework;

/// <summary>
/// Manages rain RNG.
/// </summary>
internal static class RainManger
{
    private static Dictionary<string, CachedWeather> weather = null!;

    internal static void Load(IDataHelper helper)
    {
        weather = helper.ReadSaveData<Dictionary<string, CachedWeather>>(nameof(weather)) ?? [];
    }
}

public sealed class CachedWeather
{
    public ushort rain { get; set; }

    public ushort storm { get; set; }

    public CachedWeather()
    {
        Span<byte> buffer = stackalloc byte[4];
        SeededXoshiroFactory.RNG.GetBytes(buffer);
    }
}
