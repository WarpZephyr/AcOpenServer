using AcOpenServer.Crypto;
using AcOpenServer.Network.Communication.Udp;
using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.Fsdp
{
    public class FsdpPacketClient : IDisposable
    {
        private readonly UdpChannelClient Client;
        private readonly ICipher DecryptionCipher;
        private readonly ICipher EncryptionCipher;
        private bool disposedValue;

        public bool CipherEnabled { get; set; }
        public bool IsDisposed => disposedValue;
        public event EventHandler<byte[]>? Received;

        public FsdpPacketClient(UdpChannelClient client, ICipher decryptionCipher, ICipher encryptionCipher)
        {
            Client = client;
            DecryptionCipher = decryptionCipher;
            EncryptionCipher = encryptionCipher;
        }

        #region IO

        public void StartReceive()
        {
            Client.Received += OnReceived;
        }

        public Task SendAsync(byte[] buffer)
        {
            if (CipherEnabled)
            {
                buffer = EncryptionCipher.Encrypt(buffer);
            }

            return Client.SendAsync(buffer);
        }

        #endregion

        #region Callbacks

        private void OnReceived(object? sender, int count)
        {
            var buffer = Client.Buffer;
            if (buffer != null)
            {
                if (CipherEnabled)
                {
                    buffer = DecryptionCipher.Decrypt(buffer);
                }

                Received?.Invoke(this, buffer);
            }
        }

        #endregion

        #region Client

        public void Disconnect()
            => Client.Disconnect();

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Received = null;
                    Client.Dispose();
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
