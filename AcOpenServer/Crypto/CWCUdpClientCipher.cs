using AcOpenServer.Exceptions;
using AcOpenServer.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Crypto
{
    public class CWCUdpClientCipher : ICipher
    {
        private const int AuthTokenLength = sizeof(ulong);
        private const int IVLength = 11;
        private const int TagLength = 16;
        private const int PacketTypeLength = 1;
        private const int HeaderLength = 20;
        private readonly CWCKey Key;
        private readonly byte[] AuthTokenBytes;
        private byte PacketType;
        private bool IsConnectionPrefixed;
        private bool disposedValue;

        public bool ConnectionPrefixed 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsConnectionPrefixed;
            set
            {
                IsConnectionPrefixed = value;
                if (value)
                {
                    PacketType = 1;
                }
                else
                {
                    PacketType = 0;
                }
            }
        }

        public CWCUdpClientCipher(CWCKey key, ulong authToken)
        {
            Key = key;
            AuthTokenBytes = BitConverter.GetBytes(authToken);
        }

        public byte[] Decrypt(byte[] input)
        {
            // Get initialized
            var cwc = Key.GetCWC();
            byte[] authTokenBytes = new byte[AuthTokenLength];
            byte[] iv = new byte[IVLength];
            byte[] tag = new byte[TagLength];
            byte packetType;

            // Ensure there is anything to decrypt
            int messageOffset = AuthTokenLength + IVLength + TagLength + PacketTypeLength;
            if (messageOffset >= input.Length)
            {
                throw new CwcCipherException("Input to decrypt is too small to contain any data.");
            }

            // Initialize output
            int length = input.Length - messageOffset;
            byte[] output = new byte[length];

            // Extract information
            int authTokenOffset = 0;
            int ivOffset = AuthTokenLength;
            int tagOffset = ivOffset + IVLength;
            int packetTypeOffset = tagOffset + TagLength;
            Array.Copy(input, authTokenOffset, authTokenBytes, 0, authTokenBytes.Length);
            Array.Copy(input, ivOffset, iv, 0, iv.Length);
            Array.Copy(input, tagOffset, tag, 0, tag.Length);
            packetType = input[packetTypeOffset];
            Array.Copy(input, messageOffset, output, 0, output.Length);

            // Create header
            int ivHeaderOffset = 0;
            int authTokenHeaderOffset = IVLength;
            int packetTypeHeaderOffset = authTokenHeaderOffset + AuthTokenLength;
            byte[] header = new byte[HeaderLength];
            Array.Copy(iv, 0, header, ivHeaderOffset, iv.Length);
            Array.Copy(authTokenBytes, 0, header, authTokenHeaderOffset, authTokenBytes.Length);
            header[packetTypeHeaderOffset] = packetType;

            // Decrypt
            if (!cwc.Decrypt(iv, header, output, tag))
            {
                throw new CwcCipherException("Failed to decrypt input.");
            }

            return output;
        }

        public byte[] Encrypt(byte[] input)
        {
            // Get initialized
            var cwc = Key.GetCWC();
            byte[] authTokenBytes = AuthTokenBytes;
            byte[] iv = new byte[IVLength];
            byte[] tag = new byte[TagLength];
            byte[] message = input;
            byte packetType = PacketType;

            // Randomize IV
            RandomHelper.NextBytes(iv);

            // Create header
            int ivHeaderOffset = 0;
            int authTokenHeaderOffset = IVLength;
            int packetTypeHeaderOffset = authTokenHeaderOffset + AuthTokenLength;
            byte[] header = new byte[HeaderLength];
            Array.Copy(iv, 0, header, ivHeaderOffset, iv.Length);
            Array.Copy(authTokenBytes, 0, header, authTokenHeaderOffset, iv.Length);
            header[packetTypeHeaderOffset] = packetType;

            // Encrypt
            if (!cwc.Encrypt(iv, iv, message, tag))
            {
                throw new CwcCipherException("Failed to encrypt input.");
            }

            // Build information
            int ivOffset = 0;
            int tagOffset = iv.Length;
            int messageOffset = tagOffset + tag.Length;
            int length = messageOffset + message.Length;
            byte[] output = new byte[length];
            Array.Copy(iv, 0, output, ivOffset, iv.Length);
            Array.Copy(tag, 0, output, tagOffset, tag.Length);
            Array.Copy(message, 0, output, messageOffset, message.Length);
            return output;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Key.Dispose();
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
