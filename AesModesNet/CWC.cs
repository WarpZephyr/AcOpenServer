using System;
using System.Runtime.InteropServices;
using static AesModesNet.CwcNative;

namespace AesModesNet
{
    public class CWC : IDisposable
    {
        private readonly unsafe void* CwcContext;
        private bool disposedValue;

        public unsafe CWC(byte[] key)
        {
            CwcContext = NativeMemory.AlignedAlloc((nuint)sizeof(cwc_ctx), 16);
            ThrowIfNotGood(cwc_init_and_key(key, (uint)key.Length, CwcContext));
        }

        public unsafe bool Encrypt(byte[] iv, byte[] header, byte[] message, byte[] tag)
        {
            return cwc_encrypt_message(iv, (uint)iv.Length, header, (uint)header.Length, message, (uint)message.Length, tag, (uint)tag.Length, CwcContext) == RETURN_GOOD;
        }

        public unsafe bool Decrypt(byte[] iv, byte[] header, byte[] message, byte[] tag)
        {
            return cwc_decrypt_message(iv, (uint)iv.Length, header, (uint)header.Length, message, (uint)message.Length, tag, (uint)tag.Length, CwcContext) == RETURN_GOOD;
        }

        #region IDisposable

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ThrowIfNotGood(cwc_end(CwcContext));
                NativeMemory.AlignedFree(CwcContext);

                disposedValue = true;
            }
        }

        ~CWC()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
