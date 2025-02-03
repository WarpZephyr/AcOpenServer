using AcOpenServer.Core.Crypto;
using AcOpenServer.Core.Network;
using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Streams
{
    public class SVFWMessageListener : IDisposable
    {
        private readonly NetListener Listener;
        private readonly ICipher EncryptionCipher;
        private readonly ICipher DecryptionCipher;
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        public event EventHandler<SVFWMessageClient>? Accepted;

        public SVFWMessageListener(NetListener listener, ICipher encryptionCipher, ICipher decryptionCipher)
        {
            Listener = listener;
            EncryptionCipher = encryptionCipher;
            DecryptionCipher = decryptionCipher;
        }

        #region IO

        public Task ListenAsync()
        {
            Listener.Accepted += OnAccepted;
            return Listener.ListenAsync();
        }

        #endregion

        #region Callbacks

        private void OnAccepted(object? sender, NetClient client)
        {
            var packetClient = new SVFWPacketClient(client);
            var messageClient = new SVFWMessageClient(packetClient, EncryptionCipher, DecryptionCipher);
            Accepted?.Invoke(this, messageClient);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Listener.Dispose();
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
