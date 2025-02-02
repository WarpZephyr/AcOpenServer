using AesModesNet;
using System;

namespace AcOpenServer.Core.Crypto
{
    public class CWCKey : IDisposable
    {
        private readonly CWC CwcContext;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public CWCKey(byte[] key)
        {
            CwcContext = new CWC(key);
        }

        public CWC GetCWC()
            => CwcContext;

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CwcContext.Dispose();
                }

                disposedValue = true;
            }
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
