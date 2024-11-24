using RefreshedRandom.Framework.PRNG;

namespace RefreshedRandom.Framework;

/// <inheritdoc />
public sealed class API : IRefreshedRandomAPI
{
    /// <inheritdoc />
    public Random Generate(byte[] seed) => SeededXoshiroFactory.Generate(seed);
}
