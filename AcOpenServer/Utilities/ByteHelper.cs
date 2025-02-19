using System;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Utilities
{
    internal static class ByteHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(Span<byte> bytes)
            => bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte b1, byte b2, byte b3, byte b4)
            => b1 << 24 | b2 << 16 | b3 << 8 | b4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(Span<byte> bytes)
            => (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte b1, byte b2, byte b3, byte b4)
            => (uint)(b1 << 24 | b2 << 16 | b3 << 8 | b4);
    }
}
