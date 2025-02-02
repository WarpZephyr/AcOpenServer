using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AesModesNet
{
    internal static class Native
    {
        public static unsafe T* GetAlignedPtr<T>(T value, int alignment) where T : unmanaged
        {
            void* ptr = NativeMemory.AlignedAlloc((nuint)sizeof(T), (nuint)alignment);
            Unsafe.WriteUnaligned(ptr, value);
            return (T*)ptr;
        }

        public static unsafe void FreeAlignedPtr<T>(T* ptr) where T : unmanaged
        {
            NativeMemory.AlignedFree(ptr);
        }
    }
}
