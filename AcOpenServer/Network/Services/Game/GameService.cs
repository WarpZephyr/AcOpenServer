using AcOpenServer.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services.Game
{
    public class GameService : IService
    {
        private readonly ScopeLog Log;
        private readonly Dictionary<ulong, AuthToken> AuthTokens;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public GameService(ScopeLog log)
        {
            Log = log;
            AuthTokens = [];
        }

        #region IO

        public Task ListenAsync()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        #endregion
    }
}
