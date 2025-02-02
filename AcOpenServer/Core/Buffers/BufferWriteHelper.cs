using System.Runtime.CompilerServices;

namespace AcOpenServer.Core.Buffers
{
    internal class BufferWriteHelper
    {
        public static void Write<T>(T value, byte[] buffer, int offset) where T : unmanaged
            => Unsafe.WriteUnaligned(ref buffer[offset], value);

        public static void Write<T>(T value, byte[] buffer) where T : unmanaged
            => Unsafe.WriteUnaligned(ref buffer[0], value);

        public static byte[] Write<T>(T value) where T : unmanaged
        {
            byte[] buffer = new byte[Unsafe.SizeOf<T>()];
            Write(value, buffer);
            return buffer;
        }
    }
}
