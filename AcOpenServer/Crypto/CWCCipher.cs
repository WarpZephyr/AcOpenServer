﻿using AcOpenServer.Exceptions;
using AcOpenServer.Utilities;
using System;

namespace AcOpenServer.Crypto
{
    public class CWCCipher : ICipher
    {
        private const int IVLength = 11;
        private const int TagLength = 16;
        private readonly CWCKey Key;
        private bool disposedValue;

        public CWCCipher(CWCKey key)
        {
            Key = key;
        }

        public byte[] Decrypt(byte[] input)
        {
            // Get initialized
            var cwc = Key.GetCWC();
            byte[] iv = new byte[IVLength];
            byte[] tag = new byte[TagLength];

            // Ensure there is anything to decrypt
            int messageOffset = IVLength + TagLength;
            if (messageOffset >= input.Length)
            {
                throw new CwcCipherException("Input to decrypt is too small to contain any data.");
            }

            // Initialize output
            int length = input.Length - messageOffset;
            byte[] output = new byte[length];

            // Extract information
            int ivOffset = 0;
            int tagOffset = iv.Length;
            Array.Copy(input, ivOffset, iv, 0, iv.Length);
            Array.Copy(input, tagOffset, tag, 0, tag.Length);
            Array.Copy(input, messageOffset, output, 0, output.Length);

            // Decrypt
            if (!cwc.Decrypt(iv, iv, output, tag))
            {
                throw new CwcCipherException("Failed to decrypt input.");
            }

            return output;
        }

        public byte[] Encrypt(byte[] input)
        {
            // Get initialized
            var cwc = Key.GetCWC();
            byte[] iv = new byte[IVLength];
            byte[] tag = new byte[TagLength];
            byte[] message = input;

            // Randomize IV
            RandomHelper.NextBytes(iv);

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
