using System.Runtime.CompilerServices;

namespace AcOpenServer.Core.Buffers
{
    internal static class BufferReadHelper
    {
        public static T Read<T>(byte[] buffer) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref buffer[0]);

        public static T Read<T>(byte[] buffer, int offset) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref buffer[offset]);
    }
}
