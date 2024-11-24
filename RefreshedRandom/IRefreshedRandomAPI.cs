namespace RefreshedRandom;

/// <summary>
/// The API for this mod.
/// </summary>
public interface IRefreshedRandomAPI
{
    /// <summary>
    /// Generates an xoshiro-based random using the given seed.
    /// </summary>
    /// <param name="seed">Seed to use.</param>
    /// <returns>Random instance.</returns>
    public Random Generate(byte[] seed);
}
