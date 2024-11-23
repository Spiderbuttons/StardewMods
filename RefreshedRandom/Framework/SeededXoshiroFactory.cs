namespace RefreshedRandom.Framework;

using System.Reflection;
using System.Reflection.Emit;
using System.Data.HashFunction;
using System.Runtime.Serialization;

/* To create a seeded xoshiro, we first use xxhash64 to condense an array down to a single ulong
** the use splitmix to expand it back to 128 or 256 bits.
** xxhash 128 is not available in net 6 (and is probably too much work to backport).
** 64 bits is probably enough entropy.
** we can reconsider if it feels bad.
*/

/// <summary>
/// Generates seeded xoshiro randoms.
/// </summary>
internal static class SeededXoshiroFactory
{
    private static readonly ThreadLocal<IHashFunction> Hasher = new(() => new xxHash(64));

    /// <summary>
    /// Seeds an Xoshiro random.
    /// </summary>
    private static Func<byte[], Random> _generateSeededRandom = null!;

    /// <summary>
    /// Generates a method that creates a seeded xoshiro Random.
    /// </summary>
    internal static void GenerateRandomGenerator()
    {
        Type randomType = typeof(Random);
        Type xoshiroImpl = randomType.GetNestedType("XoshiroImpl", BindingFlags.NonPublic) ?? throw new NullReferenceException("XoshiroImpl");
        FieldInfo backing = randomType.GetField("_impl", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException("_impl");
        FieldInfo[] backingfields = (new string[] { "_s0", "_s1", "_s2", "_s3" })
                                        .Select(f => xoshiroImpl.GetField(f, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException(f))
                                        .ToArray();

        Type fieldType = backingfields[0].FieldType;
        int width;
        Type typeForSpan;
        if (fieldType == typeof(uint) || fieldType == typeof(int))
        {
            width = 4;
            typeForSpan = typeof(uint);
        }
        else if (fieldType == typeof(ulong) || fieldType == typeof(long))
        {
            width = 8;
            typeForSpan = typeof(ulong);
        }
        else
        {
            throw new InvalidOperationException($"{fieldType.FullName} is not a type we can process.");
        }

        MethodInfo uninitConstr = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject)) ?? throw new NullReferenceException("uninit constr");
        MethodInfo getTypeHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)) ?? throw new NullReferenceException("type from handle");

        DynamicMethod method = new("GenerateSeededRandom", typeof(Random), [typeof(byte[])]);
        ILGenerator il = method.GetILGenerator();

        LocalBuilder ret = il.DeclareLocal(randomType);
        LocalBuilder randomimpl = il.DeclareLocal(xoshiroImpl);

        Type spantype = typeof(Span<>).MakeGenericType(typeForSpan);
        LocalBuilder span = il.DeclareLocal(spantype);

        // create empty random instance
        il.Emit(OpCodes.Ldtoken, typeof(Random));
        il.Emit(OpCodes.Call, getTypeHandle);
        il.Emit(OpCodes.Call, uninitConstr);
        il.Emit(OpCodes.Stloc, ret);

        // create empty xoshiro
        il.Emit(OpCodes.Ldtoken, xoshiroImpl);
        il.Emit(OpCodes.Call, getTypeHandle);
        il.Emit(OpCodes.Call, uninitConstr);
        il.Emit(OpCodes.Stloc, randomimpl);

        // generate span for mixing.
        il.Emit(OpCodes.Ldc_I4, 4 * width);
        il.Emit(OpCodes.Conv_U);
        il.Emit(OpCodes.Localloc);

        il.Emit(OpCodes.Ldc_I4_4);
        il.Emit(OpCodes.Newobj, spantype.GetConstructor([typeof(void*), typeof(int)]) ?? throw new NullReferenceException("span ctor"));
        il.Emit(OpCodes.Stloc, span);

        // generate seed from the byte array
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(SeededXoshiroFactory).GetMethod(nameof(Hash), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new NullReferenceException("hasher"));

        // call mixer
        {
            MethodInfo mixerMethod = typeof(SeededXoshiroFactory).GetMethod(width == 8 ? nameof(SplitMix256) : nameof(SplitMix128), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new NullReferenceException("mixer method");
            il.Emit(OpCodes.Ldloc, span);
            il.Emit(OpCodes.Call, mixerMethod);
        }

        // assign to the xoshiro
        {
            MethodInfo span_getter = spantype.GetMethod("get_Item") ?? throw new NullReferenceException("span getter");
            for (int i = 0; i < backingfields.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, randomimpl);
                il.Emit(OpCodes.Ldloca, span);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Call, span_getter);
                il.Emit(width == 8 ? OpCodes.Ldind_I8 : OpCodes.Ldind_I4);
                il.Emit(OpCodes.Stfld, backingfields[i]);
            }
        }

        // assign the xoshiro to the Random instance.
        il.Emit(OpCodes.Ldloc, ret);
        il.Emit(OpCodes.Ldloc, randomimpl);
        il.Emit(OpCodes.Stfld, backing);

        // load the random instance and return it.
        il.Emit(OpCodes.Ldloc, ret);
        il.Emit(OpCodes.Ret);
        _generateSeededRandom = method.CreateDelegate<Func<byte[], Random>>();
    }

    /// <summary>
    /// Generates a seeded xoshiro random using the given bytes as the seed.
    /// </summary>
    /// <param name="seed">Seed to use.</param>
    /// <returns>Seeded random.</returns>
    internal static Random Generate(byte[] seed) => _generateSeededRandom.Invoke(seed);

    /// <summary>
    /// Gets the xxHash64 for the specific data.
    /// </summary>
    /// <param name="seed">Data in question.</param>
    /// <returns>Hash.</returns>
    internal static ulong Hash(byte[] seed) => BitConverter.ToUInt64(Hasher.Value!.ComputeHash(seed));

    private static void SplitMix256(ulong seed, Span<ulong> buffer)
    {
        SplitMix mixer = new(seed);

        for (int i = 0; i < buffer.Length; i++)
        {
            ulong next = mixer.Next();
            buffer[i] = next;
        }
    }

    private static void SplitMix128(ulong seed, Span<uint> buffer)
    {
        SplitMix mixer = new(seed);

        for (int i = 0; i < buffer.Length; i++)
        {
            ulong next = mixer.Next();
            buffer[i] = (uint)next;
            if (buffer.Length > i + 1)
            {
                buffer[i + 1] = (uint)(next >> 32);
            }
        }
    }
}
