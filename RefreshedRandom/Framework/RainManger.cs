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

/// <summary>
/// A history of the weather for this locationcontext.
/// </summary>
public sealed class CachedWeather
{
    /// <summary>
    /// The rain of the past 16 days.
    /// </summary>
    public ushort rain { get; set; }

    /// <summary>
    /// The storms of the past 16 days.
    /// </summary>
    public ushort storm { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedWeather"/> class.
    /// </summary>
    public CachedWeather()
    {
        // at init, we have no data. Fill with random.

        Span<byte> buffer = stackalloc byte[4];
        SeededXoshiroFactory.RNG.GetBytes(buffer);

        BitConverter.TryWriteBytes(buffer, this.rain);
        buffer = buffer[2..];
        BitConverter.TryWriteBytes(buffer, this.storm);
    }
}
