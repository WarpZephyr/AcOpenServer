using System;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Binary
{
    public static class BinaryBufferWriter
    {
        public static void Write<T>(Span<byte> buffer, int offset, T value) where T : unmanaged
            => Unsafe.WriteUnaligned(ref buffer[offset], value);

        public static void Write<T>(Span<byte> buffer, T value) where T : unmanaged
            => Unsafe.WriteUnaligned(ref buffer[0], value);

        public static byte[] Write<T>(T value) where T : unmanaged
        {
            byte[] buffer = new byte[Unsafe.SizeOf<T>()];
            Write(buffer, value);
            return buffer;
        }
    }
}
