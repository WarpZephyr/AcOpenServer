using System;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Utilities
{
    internal static class RandomHelper
    {
        [ThreadStatic] static Random? Random;

        internal static Random GetRandom()
        {
            Random ??= new Random(Guid.NewGuid().GetHashCode());
            return Random;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NextBytes(byte[] buffer)
            => GetRandom().NextBytes(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NextBytes(Span<byte> buffer)
            => GetRandom().NextBytes(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int NextInt32()
            => GetRandom().Next();
    }
}
