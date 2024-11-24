// We use splitmix to divide a ulong of entropy across the 256bit state space of xoshiro, following the author's recommendations

// implementation follows one by Sebastiano Vigna (vigna@acm.org), who has kindly licensed creative commons zero.
// https://github.com/svaarala/duktape/blob/master/misc/splitmix64.c

namespace RefreshedRandom.Framework.PRNG;

internal ref struct SplitMix
{
    private ulong seed;

    internal SplitMix(ulong seed) => this.seed = seed;

    internal ulong Next()
    {
        ulong ret;
        unchecked
        {
            ret = this.seed += 0x9E3779B97F4A7C15;
            ret = (ret ^ ret >> 30) * 0xBF58476D1CE4E5B9;
            ret = (ret ^ ret >> 27) * 0x94D049BB133111EB;
        }
        return ret;
    }
}
