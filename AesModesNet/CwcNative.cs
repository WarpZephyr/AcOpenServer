#if _WIN64
#  define USE_LONGS
#else
#  define USE_FLOATS
#endif

using System;
using System.Runtime.InteropServices;
using static AesModesNet.AesNative;

namespace AesModesNet
{
    internal static partial class CwcNative
    {
        const string DLLNAME = "aes_modes-x64.dll";

        #region Constants

        const int CWC_CBLK_SIZE = AES_BLOCK_SIZE;
        public const int RETURN_WARN = 1;
        public const int RETURN_GOOD = 0;
        public const int RETURN_ERROR = -1;

        #endregion

        #region Structs

        public unsafe struct cwc_ctx
        {
            /// <summary>
            /// counter (iv) buffer
            /// </summary>
            public uint ctr_val;

            /// <summary>
            /// encrypted data buffer
            /// </summary>
            public uint enc_ctr;

            /// <summary>
            /// auth data buffer
            /// </summary>
            public uint cwc_buf;

            /// <summary>
            /// AES context
            /// </summary>
            public aes_crypt_ctx enc_ctx;
#if USE_LONGS
            /// <summary>
            /// cwc z value
            /// </summary>
            public fixed uint zval[CWC_CBLK_SIZE >> 2];

            /// <summary>
            /// running hash value
            /// </summary>
            public fixed uint hash[CWC_CBLK_SIZE >> 1];
#elif USE_FLOATS
            /// <summary>
            /// cwc z value
            /// </summary>
            public fixed double zval[6];

            /// <summary>
            /// running hash value
            /// </summary>
            public fixed double hash[6];
#endif
            /// <summary>
            /// header bytes so far
            /// </summary>
            public uint hdr_cnt;

            /// <summary>
            /// text bytes so far (encrypt)
            /// </summary>
            public uint txt_ccnt;

            /// <summary>
            /// text bytes so far (auth)
            /// </summary>
            public uint txt_acnt;
        }

        #endregion

        #region Native Functions

        /// <summary>
        /// Initialize mode and set key.
        /// </summary>
        /// <param name="key">The key value.</param>
        /// <param name="key_len">The key length in bytes.</param>
        /// <param name="ctx">The mode context.</param>
        [LibraryImport(DLLNAME)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int cwc_init_and_key(byte[] key, uint key_len, void* ctx);             

        /// <summary>
        /// Clean up and end operation.
        /// </summary>
        /// <param name="ctx">The mode context.</param>
        /// <returns></returns>
        [LibraryImport(DLLNAME)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int cwc_end(void* ctx);            

        /// <summary>
        /// Encrypt an entire message.
        /// </summary>
        /// <param name="iv">The initialisation vector.</param>
        /// <param name="iv_len">The iv length in bytes.</param>
        /// <param name="hdr">The header buffer</param>
        /// <param name="hdr_len">The hdr length in bytes.</param>
        /// <param name="msg">The message buffer.</param>
        /// <param name="msg_len">The msg length in bytes.</param>
        /// <param name="tag">The buffer for the tag.</param>
        /// <param name="tag_len">The tag length in bytes.</param>
        /// <param name="ctx">The mode context.</param>
        [LibraryImport(DLLNAME)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int cwc_encrypt_message(byte[] iv, uint iv_len, byte[] hdr, uint hdr_len, byte[] msg, uint msg_len, byte[] tag, uint tag_len, void* ctx);    

        /// <summary>
        /// Decrypt an entire message.
        /// </summary>
        /// <param name="iv">The initialisation vector.</param>
        /// <param name="iv_len">The iv length in bytes.</param>
        /// <param name="hdr">The header buffer</param>
        /// <param name="hdr_len">The hdr length in bytes.</param>
        /// <param name="msg">The message buffer.</param>
        /// <param name="msg_len">The msg length in bytes.</param>
        /// <param name="tag">The buffer for the tag.</param>
        /// <param name="tag_len">The tag length in bytes.</param>
        /// <param name="ctx">The mode context.</param>
        /// <returns>RETURN_GOOD is returned if the input tag matches that for the decrypted message.</returns>
        [LibraryImport(DLLNAME)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int cwc_decrypt_message(byte[] iv, uint iv_len, byte[] hdr, uint hdr_len, byte[] msg, uint msg_len, byte[] tag, uint tag_len, void* ctx);

        #endregion

        #region Managed Functions

        public static void ThrowIfNotGood(int result)
        {
            if (result != RETURN_GOOD)
            {
                throw new CwcException("A native library error occurred.");
            }
        }

        public static void ThrowIfNotGood(int result, string message)
        {
            if (result != RETURN_GOOD)
            {
                throw new CwcException(message);
            }
        }

        public static void ThrowOnError(int result)
        {
            if (result == RETURN_ERROR)
            {
                throw new CwcException("A native library error occurred.");
            }
        }

        public static void ThrowOnError(int result, string message)
        {
            if (result == RETURN_ERROR)
            {
                throw new CwcException(message);
            }
        }

        public static void ThrowOnWarning(int result)
        {
            if (result == RETURN_WARN)
            {
                throw new CwcException("A native library warning occurred.");
            }
        }

        public static void ThrowOnWarning(int result, string message)
        {
            if (result == RETURN_WARN)
            {
                throw new CwcException(message);
            }
        }

        #endregion
    }
}
