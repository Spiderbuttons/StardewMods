namespace RefreshedRandom.Framework;

using System.Reflection;
using System.Reflection.Emit;
using System.Data.HashFunction;
using System.Runtime.Serialization;

// To create a seeded xoshiro, we first use xxhash64 to condense an array down to a single ulong
// the use splitmix to expand it back to 128 or 256 bits.
// xxhash 128 is not available in net 6 (and is probably too much work to backport).
// 64 bits is probably enough entropy.
// we can reconsider if it feels bad.

internal static class SeededXoshiroFactory
{
    private static readonly ThreadLocal<IHashFunction> Hasher = new(() => new xxHash(64));

    /// <summary>
    /// Seeds an Xoshiro random.
    /// </summary>
    private static Lazy<Func<byte[], Random?>> _generateSeededRandom = new(() => {
        Type randomType = typeof(Random);
        Type xoshiroImpl = randomType.GetNestedType("XoshiroImpl", BindingFlags.NonPublic) ?? throw new NullReferenceException("XoshiroImpl");
        FieldInfo[] backingfields = (new string[] { "_s0", "_s1", "_s2", "_s3" })
                                        .Select(f => xoshiroImpl.GetField(f, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException(f))
                                        .ToArray();

        var fieldType = backingfields[0].FieldType;
        int width;
        if (fieldType == typeof(uint) || fieldType == typeof(int))
        {
            width = 4;
        }
        else if (fieldType == typeof(ulong) || fieldType == typeof(long))
        {
            width = 8;
        }
        else
        {
            throw new InvalidOperationException($"{fieldType.FullName} is not a type we can process.");
        }



        Type sCoreType = Type.GetType("StardewModdingAPI.Framework.SCore,StardewModdingAPI")!;
        Type commandQueueType = Type.GetType("StardewModdingAPI.Framework.CommandQueue,StardewModdingAPI")!;
        MethodInfo sCoreGetter = sCoreType.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static)!.GetGetMethod(true)!;
        FieldInfo rawCommandQueueField = sCoreType.GetField("RawCommandQueue", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo queueAddMethod = commandQueueType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance)!;

        DynamicMethod method = new("GenerateSeededRandom", typeof(Random), [typeof(byte[])]);
        ILGenerator il = method.GetILGenerator();
        var ret = il.DeclareLocal(randomType);
        il.Emit(OpCodes.Newobj, randomType.GetConstructor(BindingFlags.Instance| BindingFlags.Public, Type.EmptyTypes)!);
        il.Emit(OpCodes.Stloc, ret);

        il.Emit(OpCodes.Ldloc, ret);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<byte[], Random?>>();
    });

    internal static Random Generate(byte[] seed)
    {
        Random rnd = new Random();
        return rnd;
    }

    private static ulong Hash(byte[] seed) => BitConverter.ToUInt64(Hasher.Value!.ComputeHash(seed));

    private static void SplitMix256(ulong seed, Span<ulong> buffer)
    {
        SplitMix mixer = new(seed);

        for (int i = 0; i < buffer.Length; i++)
        {
            var next = mixer.Next();
            buffer[i] = next;
        }
    }

    private static void SplitMix128(ulong seed, Span<uint> buffer)
    {
        SplitMix mixer = new(seed);

        for (int i = 0; i < buffer.Length; i++)
        {
            var next = mixer.Next();
            buffer[i] = (uint)next;
            if (buffer.Length > i + 1)
            {
                buffer[i + 1] = (uint)(next >> 32);
            }
        }
    }
}
