using System;
using static AesModesNet.CwcNative;

namespace AesModesNet
{
    public class CWC : IDisposable
    {
        private cwc_ctx CwcContext;
        private bool disposedValue;

        public unsafe CWC(byte[] key)
        {
            cwc_ctx* ptr = Native.GetAlignedPtr(CwcContext, 16);
            ThrowIfNotGood(cwc_init_and_key(key, (uint)key.Length, ptr));
            Native.FreeAlignedPtr(ptr);
        }

        public unsafe bool Encrypt(byte[] iv, byte[] header, byte[] message, byte[] tag)
        {
            fixed (cwc_ctx* ptr = &CwcContext)
            {
                return cwc_encrypt_message(iv, (uint)iv.Length, header, (uint)header.Length, message, (uint)message.Length, tag, (uint)tag.Length, ptr) == RETURN_GOOD;
            }
        }

        public unsafe bool Decrypt(byte[] iv, byte[] header, byte[] message, byte[] tag)
        {
            fixed (cwc_ctx* ptr = &CwcContext)
            {
                return cwc_decrypt_message(iv, (uint)iv.Length, header, (uint)header.Length, message, (uint)message.Length, tag, (uint)tag.Length, ptr) == RETURN_GOOD;
            }
        }

        #region IDisposable

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                fixed (cwc_ctx* ptr = &CwcContext)
                {
                    ThrowIfNotGood(cwc_end(ptr));
                }

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
