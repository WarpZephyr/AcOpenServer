#define AES_128     // if a fast 128 bit key scheduler is needed
#define AES_192     // if a fast 192 bit key scheduler is needed
#define AES_256     // if a fast 256 bit key scheduler is needed
#define AES_VAR     // if variable key size scheduler is needed

using System.Runtime.InteropServices;

namespace AesModesNet
{
    internal static class AesNative
    {
        public const int AES_BLOCK_SIZE_P2 = 4;  // AES block size as a power of 2
        public const int AES_BLOCK_SIZE = (1 << AES_BLOCK_SIZE_P2); // AES block size

#if AES_VAR || AES_256
        const int KS_LENGTH = 60;
#elif AES_192
const int KS_LENGTH = 52;
#else
        const int KS_LENGTH = 44;
#endif

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct aes_inf
        {
            [FieldOffset(0)]
            public uint l;

            [FieldOffset(0)]
            public fixed byte b[4];
        }

        public unsafe struct aes_crypt_ctx
        {
            public fixed uint ks[KS_LENGTH];
            public aes_inf inf;
        }
    }
}
