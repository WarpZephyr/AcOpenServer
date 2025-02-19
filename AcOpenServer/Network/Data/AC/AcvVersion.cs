using AcOpenServer.Utilities;
using System;

namespace AcOpenServer.Network.Data.AC
{
    /// <summary>
    /// A version value for 5th generation Armored Core.<br/>
    /// Names of each field are assumed, if they considered fields to begin with.
    /// </summary>
    public struct AcvVersion
    {
        /// <summary>
        /// The game release; Seen as 1.
        /// </summary>
        public byte Release;

        /// <summary>
        /// The major version maybe? Seen as 0.
        /// </summary>
        public byte Major;

        /// <summary>
        /// The minor version maybe? Seen as 0.
        /// </summary>
        public byte Minor;

        /// <summary>
        /// The patch version; Seen as 0 or 2.
        /// </summary>
        public byte Patch;

        /// <summary>
        /// Create a new <see cref="AcvVersion"/> from bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public AcvVersion(Span<byte> bytes)
        {
            Release = bytes[0];
            Major = bytes[1];
            Minor = bytes[2];
            Patch = bytes[3];
        }

        #region Operators

        public static bool operator <(AcvVersion a, AcvVersion b)
            => (int)a < (int)b;

        public static bool operator >(AcvVersion a, AcvVersion b)
            => (int)a > (int)b;

        public static bool operator ==(AcvVersion a, AcvVersion b)
            => a.Equals(b);

        public static bool operator !=(AcvVersion a, AcvVersion b)
            => !a.Equals(b);

        #endregion

        #region Explicit

        public static explicit operator int(AcvVersion value)
            => ByteHelper.ToInt32(value.Release, value.Major, value.Minor, value.Patch);

        public static explicit operator uint(AcvVersion value)
            => ByteHelper.ToUInt32(value.Release, value.Major, value.Minor, value.Patch);

        #endregion

        #region Equals

        public readonly bool Equals(AcvVersion other)
            => Release == other.Release
            && Major == other.Major
            && Minor == other.Minor
            && Patch == other.Patch;

        public override readonly bool Equals(object? obj)
            => obj is AcvVersion other
            && Equals(other);

        #endregion

        #region GetHashCode

        public override readonly int GetHashCode()
            => HashCode.Combine(Release, Major, Minor, Patch);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"{Release}.{Major}.{Minor}.{Patch}";

        #endregion
    }
}
