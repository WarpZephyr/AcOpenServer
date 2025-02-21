using AcOpenServer.Utilities;
using System;
using System.Buffers.Binary;

namespace AcOpenServer.Network.Data.AC
{
    /// <summary>
    /// An app version holder for 5th generation Armored Core.
    /// </summary>
    public struct AcvAppVersion
    {
        /// <summary>
        /// Unknown, build possibly? Seen as 0x56440000 on PS3 US disc 1.02.
        /// </summary>
        public uint Unk00;

        /// <summary>
        /// The app version.
        /// </summary>
        public AcvVersion Version;

        /// <summary>
        /// Create a new <see cref="AcvAppVersion"/> from a <see cref="ulong"/> value.
        /// </summary>
        /// <param name="appVersion">The <see cref="ulong"/> value.</param>
        public AcvAppVersion(ulong appVersion)
            : this(BitConverter.IsLittleEndian ? BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(appVersion)) : BitConverter.GetBytes(appVersion))
        {
            
        }

        /// <summary>
        /// Create a new <see cref="AcvAppVersion"/> from bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public AcvAppVersion(Span<byte> bytes)
        {
            Unk00 = ByteHelper.ToUInt32(bytes);
            Version = new AcvVersion(bytes[4..]);
        }

        #region Operators

        public static bool operator <(AcvAppVersion a, AcvAppVersion b)
            => a.Unk00 < b.Unk00
            && a.Version < b.Version;

        public static bool operator >(AcvAppVersion a, AcvAppVersion b)
            => a.Unk00 > b.Unk00
            && a.Version > b.Version;

        public static bool operator ==(AcvAppVersion a, AcvAppVersion b)
            => a.Equals(b);

        public static bool operator !=(AcvAppVersion a, AcvAppVersion b)
            => !a.Equals(b);

        #endregion

        #region Equals

        public readonly bool Equals(AcvAppVersion other)
            => Unk00 == other.Unk00
            && Version == other.Version;

        public override readonly bool Equals(object? obj)
            => obj is AcvAppVersion other
            && Equals(other);

        #endregion

        #region GetHashCode

        public override readonly int GetHashCode()
            => HashCode.Combine(Unk00, Version.GetHashCode());

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"{Version}:{Unk00:X}";

        #endregion
    }
}
