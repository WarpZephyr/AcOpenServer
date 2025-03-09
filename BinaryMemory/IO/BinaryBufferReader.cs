using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static BinaryMemory.Helpers.StringHelper;

namespace BinaryMemory.IO
{
    /// <summary>
    /// Static methods for reading binary data.
    /// </summary>
    public static class BinaryBufferReader
    {
        #region Generic

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(ReadOnlySpan<byte> buffer) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(buffer));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(ReadOnlySpan<byte> buffer, int offset) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset));

        public static T Read<T>(ReadOnlySpan<byte> buffer, ref int offset) where T : unmanaged
        {
            var value = Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset));
            offset += Unsafe.SizeOf<T>();
            return value;
        }

        #endregion

        #region SByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte PeekSByte(byte[] buffer)
            => (sbyte)buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte GetSByte(byte[] buffer, int offset)
            => (sbyte)buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte[] buffer, ref int offset)
            => (sbyte)buffer[offset++];

        #endregion

        #region Byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PeekByte(byte[] buffer)
            => buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetByte(byte[] buffer, int offset)
            => buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte[] buffer, ref int offset)
            => buffer[offset++];

        #endregion

        #region Int16

        public static Int16 PeekInt16(ReadOnlySpan<byte> buffer)
            => Peek<Int16>(buffer);

        public static Int16 PeekInt16LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekInt16(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekInt16(buffer));

        public static Int16 PeekInt16BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekInt16(buffer))
            : PeekInt16(buffer);

        public static Int16 GetInt16(ReadOnlySpan<byte> buffer, int offset)
            => Get<Int16>(buffer, offset);

        public static Int16 GetInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetInt16(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetInt16(buffer, offset));

        public static Int16 GetInt16BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetInt16(buffer, offset))
            : GetInt16(buffer, offset);

        public static Int16 ReadInt16(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Int16>(buffer, ref offset);

        public static Int16 ReadInt16LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadInt16(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadInt16(buffer, ref offset));

        public static Int16 ReadInt16BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadInt16(buffer, ref offset))
            : ReadInt16(buffer, ref offset);

        #endregion

        #region UInt16

        public static UInt16 PeekUInt16(ReadOnlySpan<byte> buffer)
            => Peek<UInt16>(buffer);

        public static UInt16 PeekUInt16LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekUInt16(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekUInt16(buffer));

        public static UInt16 PeekUInt16BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekUInt16(buffer))
            : PeekUInt16(buffer);

        public static UInt16 GetUInt16(ReadOnlySpan<byte> buffer, int offset)
            => Get<UInt16>(buffer, offset);

        public static UInt16 GetUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetUInt16(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetUInt16(buffer, offset));

        public static UInt16 GetUInt16BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetUInt16(buffer, offset))
            : GetUInt16(buffer, offset);

        public static UInt16 ReadUInt16(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<UInt16>(buffer, ref offset);

        public static UInt16 ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadUInt16(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadUInt16(buffer, ref offset));

        public static UInt16 ReadUInt16BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadUInt16(buffer, ref offset))
            : ReadUInt16(buffer, ref offset);

        #endregion

        #region Int32

        public static Int32 PeekInt32(ReadOnlySpan<byte> buffer)
            => Peek<Int32>(buffer);

        public static Int32 PeekInt32LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekInt32(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekInt32(buffer));

        public static Int32 PeekInt32BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekInt32(buffer))
            : PeekInt32(buffer);

        public static Int32 GetInt32(ReadOnlySpan<byte> buffer, int offset)
            => Get<Int32>(buffer, offset);

        public static Int32 GetInt32LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetInt32(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetInt32(buffer, offset));

        public static Int32 GetInt32BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetInt32(buffer, offset))
            : GetInt32(buffer, offset);

        public static Int32 ReadInt32(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Int32>(buffer, ref offset);

        public static Int32 ReadInt32LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadInt32(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadInt32(buffer, ref offset));

        public static Int32 ReadInt32BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadInt32(buffer, ref offset))
            : ReadInt32(buffer, ref offset);

        #endregion

        #region UInt32

        public static UInt32 PeekUInt32(ReadOnlySpan<byte> buffer)
            => Peek<UInt32>(buffer);

        public static UInt32 PeekUInt32LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekUInt32(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekUInt32(buffer));

        public static UInt32 PeekUInt32BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekUInt32(buffer))
            : PeekUInt32(buffer);

        public static UInt32 GetUInt32(ReadOnlySpan<byte> buffer, int offset)
            => Get<UInt32>(buffer, offset);

        public static UInt32 GetUInt32LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetUInt32(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetUInt32(buffer, offset));

        public static UInt32 GetUInt32BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetUInt32(buffer, offset))
            : GetUInt32(buffer, offset);

        public static UInt32 ReadUInt32(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<UInt32>(buffer, ref offset);

        public static UInt32 ReadUInt32LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadUInt32(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadUInt32(buffer, ref offset));

        public static UInt32 ReadUInt32BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadUInt32(buffer, ref offset))
            : ReadUInt32(buffer, ref offset);

        #endregion

        #region Int64

        public static Int64 PeekInt64(ReadOnlySpan<byte> buffer)
            => Peek<Int64>(buffer);

        public static Int64 PeekInt64LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekInt64(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekInt64(buffer));

        public static Int64 PeekInt64BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekInt64(buffer))
            : PeekInt64(buffer);

        public static Int64 GetInt64(ReadOnlySpan<byte> buffer, int offset)
            => Get<Int64>(buffer, offset);

        public static Int64 GetInt64LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetInt64(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetInt64(buffer, offset));

        public static Int64 GetInt64BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetInt64(buffer, offset))
            : GetInt64(buffer, offset);

        public static Int64 ReadInt64(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Int64>(buffer, ref offset);

        public static Int64 ReadInt64LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadInt64(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadInt64(buffer, ref offset));

        public static Int64 ReadInt64BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadInt64(buffer, ref offset))
            : ReadInt64(buffer, ref offset);

        #endregion

        #region UInt64

        public static UInt64 PeekUInt64(ReadOnlySpan<byte> buffer)
            => Peek<UInt64>(buffer);

        public static UInt64 PeekUInt64LittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekUInt64(buffer)
            : BinaryPrimitives.ReverseEndianness(PeekUInt64(buffer));

        public static UInt64 PeekUInt64BigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(PeekUInt64(buffer))
            : PeekUInt64(buffer);

        public static UInt64 GetUInt64(ReadOnlySpan<byte> buffer, int offset)
            => Get<UInt64>(buffer, offset);

        public static UInt64 GetUInt64LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetUInt64(buffer, offset)
            : BinaryPrimitives.ReverseEndianness(GetUInt64(buffer, offset));

        public static UInt64 GetUInt64BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(GetUInt64(buffer, offset))
            : GetUInt64(buffer, offset);

        public static UInt64 ReadUInt64(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<UInt64>(buffer, ref offset);

        public static UInt64 ReadUInt64LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadUInt64(buffer, ref offset)
            : BinaryPrimitives.ReverseEndianness(ReadUInt64(buffer, ref offset));

        public static UInt64 ReadUInt64BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(ReadUInt64(buffer, ref offset))
            : ReadUInt64(buffer, ref offset);

        #endregion

        #region Half

        public static Half PeekHalf(ReadOnlySpan<byte> buffer)
            => Peek<Half>(buffer);

        public static Half PeekHalfLittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekHalf(buffer)
            : BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(PeekUInt16(buffer)));

        public static Half PeekHalfBigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(PeekUInt16(buffer)))
            : PeekHalf(buffer);

        public static Half GetHalf(ReadOnlySpan<byte> buffer, int offset)
            => Get<Half>(buffer, offset);

        public static Half GetHalfLittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetHalf(buffer, offset)
            : BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(GetUInt16(buffer, offset)));

        public static Half GetHalfBigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(GetUInt16(buffer, offset)))
            : GetHalf(buffer, offset);

        public static Half ReadHalf(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Half>(buffer, ref offset);

        public static Half ReadHalfLittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadHalf(buffer, ref offset)
            : BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(ReadUInt16(buffer, ref offset)));

        public static Half ReadHalfBigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(ReadUInt16(buffer, ref offset)))
            : ReadHalf(buffer, ref offset);

        #endregion

        #region Single

        public static Single PeekSingle(ReadOnlySpan<byte> buffer)
            => Peek<Single>(buffer);

        public static Single PeekSingleLittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekSingle(buffer)
            : BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(PeekUInt32(buffer)));

        public static Single PeekSingleBigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(PeekUInt32(buffer)))
            : PeekSingle(buffer);

        public static Single GetSingle(ReadOnlySpan<byte> buffer, int offset)
            => Get<Single>(buffer, offset);

        public static Single GetSingleLittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetSingle(buffer, offset)
            : BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(GetUInt32(buffer, offset)));

        public static Single GetSingleBigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(GetUInt32(buffer, offset)))
            : GetSingle(buffer, offset);

        public static Single ReadSingle(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Single>(buffer, ref offset);

        public static Single ReadSingleLittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadSingle(buffer, ref offset)
            : BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(ReadUInt32(buffer, ref offset)));

        public static Single ReadSingleBigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(ReadUInt32(buffer, ref offset)))
            : ReadSingle(buffer, ref offset);

        #endregion

        #region Double

        public static Double PeekDouble(ReadOnlySpan<byte> buffer)
            => Peek<Double>(buffer);

        public static Double PeekDoubleLittleEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? PeekDouble(buffer)
            : BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(PeekUInt64(buffer)));

        public static Double PeekDoubleBigEndian(ReadOnlySpan<byte> buffer)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(PeekUInt64(buffer)))
            : PeekDouble(buffer);

        public static Double GetDouble(ReadOnlySpan<byte> buffer, int offset)
            => Get<Double>(buffer, offset);

        public static Double GetDoubleLittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? GetDouble(buffer, offset)
            : BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(GetUInt64(buffer, offset)));

        public static Double GetDoubleBigEndian(ReadOnlySpan<byte> buffer, int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(GetUInt64(buffer, offset)))
            : GetDouble(buffer, offset);

        public static Double ReadDouble(ReadOnlySpan<byte> buffer, ref int offset)
            => Read<Double>(buffer, ref offset);

        public static Double ReadDoubleLittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? ReadDouble(buffer, ref offset)
            : BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(ReadUInt64(buffer, ref offset)));

        public static Double ReadDoubleBigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => BitConverter.IsLittleEndian
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(ReadUInt64(buffer, ref offset)))
            : ReadDouble(buffer, ref offset);

        #endregion

        #region Char

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char PeekChar(ReadOnlySpan<byte> buffer)
            => (char)buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetChar(ReadOnlySpan<byte> buffer, int offset)
            => (char)buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(ReadOnlySpan<byte> buffer, ref int offset)
            => (char)buffer[offset++];

        #endregion

        #region Boolean

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PeekBoolean(ReadOnlySpan<byte> buffer)
        {
            var b = buffer[0];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBoolean(ReadOnlySpan<byte> buffer, int offset)
        {
            var b = buffer[offset];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean(ReadOnlySpan<byte> buffer, ref int offset)
        {
            var b = buffer[offset++];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        #endregion

        #region String 8-Bit

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek8BitStringSpan(ReadOnlySpan<byte> buffer)
            => buffer[..Strlen(buffer)];

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek8BitStringSpan(ReadOnlySpan<byte> buffer, int length)
            => buffer[..StrlenFixed(buffer, length)];

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get8BitStringSpan(ReadOnlySpan<byte> buffer, int offset)
            => buffer.Slice(offset, StrlenOffset(buffer, offset));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get8BitStringSpan(ReadOnlySpan<byte> buffer, int offset, int length)
            => buffer.Slice(offset, StrlenOffsetFixed(buffer, offset, length));

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read8BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset)
        {
            int strLen = StrlenOffset(buffer, offset);
            var slice = buffer.Slice(offset, strLen);

            offset += strLen;
            if (strLen < buffer.Length)
                offset += 1; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read8BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset, int length)
        {
            int strLen = StrlenOffsetFixed(buffer, offset, length);
            var slice = buffer.Slice(offset, strLen);

            offset += length;
            return slice;
        }

        #endregion

        #region String 16-Bit

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek16BitStringSpan(ReadOnlySpan<byte> buffer)
            => buffer[..WStrlen(buffer)];

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek16BitStringSpan(ReadOnlySpan<byte> buffer, int length)
            => buffer[..WStrlenFixed(buffer, length)];

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get16BitStringSpan(ReadOnlySpan<byte> buffer, int offset)
            => buffer.Slice(offset, WStrlenOffset(buffer, offset));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get16BitStringSpan(ReadOnlySpan<byte> buffer, int offset, int length)
            => buffer.Slice(offset, WStrlenOffsetFixed(buffer, offset, length));

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read16BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset)
        {
            int strLen = WStrlenOffset(buffer, offset);
            var slice = buffer.Slice(offset, strLen);

            offset += strLen;
            if (strLen < buffer.Length)
                offset += 2; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read16BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset, int length)
        {
            int strLen = WStrlenOffsetFixed(buffer, offset, length);
            var slice = buffer.Slice(offset, strLen);

            offset += length * 2;
            return slice;
        }

        #endregion

        #region String 32-Bit

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek32BitStringSpan(ReadOnlySpan<byte> buffer)
            => buffer[..DWStrlen(buffer)];

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Peek32BitStringSpan(ReadOnlySpan<byte> buffer, int length)
            => buffer[..DWStrlenFixed(buffer, length)];

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get32BitStringSpan(ReadOnlySpan<byte> buffer, int offset)
            => buffer.Slice(offset, DWStrlenOffset(buffer, offset));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/> at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Get32BitStringSpan(ReadOnlySpan<byte> buffer, int offset, int length)
            => buffer.Slice(offset, DWStrlenOffsetFixed(buffer, offset, length));

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read32BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset)
        {
            int strLen = DWStrlenOffset(buffer, offset);
            var slice = buffer.Slice(offset, strLen);

            offset += strLen;
            if (strLen < buffer.Length)
                offset += 4; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private static ReadOnlySpan<byte> Read32BitStringSpan(ReadOnlySpan<byte> buffer, ref int offset, int length)
        {
            int strLen = DWStrlenOffsetFixed(buffer, offset, length);
            var slice = buffer.Slice(offset, strLen);

            offset += length * 4;
            return slice;
        }

        #endregion

        #region String UTF8

        public static string PeekUTF8(ReadOnlySpan<byte> buffer)
            => Encoding.UTF8.GetString(Peek8BitStringSpan(buffer));

        public static string PeekUTF8(ReadOnlySpan<byte> buffer, int length)
            => Encoding.UTF8.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetUTF8(ReadOnlySpan<byte> buffer, int offset)
            => Encoding.UTF8.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetUTF8(ReadOnlySpan<byte> buffer, int offset, int length)
            => Encoding.UTF8.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadUTF8(ReadOnlySpan<byte> buffer, ref int offset)
            => Encoding.UTF8.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadUTF8(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => Encoding.UTF8.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String ASCII

        public static string PeekASCII(ReadOnlySpan<byte> buffer)
            => Encoding.ASCII.GetString(Peek8BitStringSpan(buffer));

        public static string PeekASCII(ReadOnlySpan<byte> buffer, int length)
            => Encoding.ASCII.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetASCII(ReadOnlySpan<byte> buffer, int offset)
            => Encoding.ASCII.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetASCII(ReadOnlySpan<byte> buffer, int offset, int length)
            => Encoding.ASCII.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadASCII(ReadOnlySpan<byte> buffer, ref int offset)
            => Encoding.ASCII.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadASCII(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => Encoding.ASCII.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String ShiftJIS

        public static string PeekShiftJIS(ReadOnlySpan<byte> buffer)
            => EncodingHelper.ShiftJIS.GetString(Peek8BitStringSpan(buffer));

        public static string PeekShiftJIS(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.ShiftJIS.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetShiftJIS(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.ShiftJIS.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetShiftJIS(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.ShiftJIS.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadShiftJIS(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.ShiftJIS.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadShiftJIS(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.ShiftJIS.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String EucJP

        public static string PeekEucJP(ReadOnlySpan<byte> buffer)
            => EncodingHelper.EucJP.GetString(Peek8BitStringSpan(buffer));

        public static string PeekEucJP(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.EucJP.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetEucJP(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.EucJP.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetEucJP(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.EucJP.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadEucJP(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.EucJP.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadEucJP(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.EucJP.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String EucCN

        public static string PeekEucCN(ReadOnlySpan<byte> buffer)
            => EncodingHelper.EucCN.GetString(Peek8BitStringSpan(buffer));

        public static string PeekEucCN(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.EucCN.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetEucCN(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.EucCN.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetEucCN(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.EucCN.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadEucCN(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.EucCN.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadEucCN(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.EucCN.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String EucKR

        public static string PeekEucKR(ReadOnlySpan<byte> buffer)
            => EncodingHelper.EucKR.GetString(Peek8BitStringSpan(buffer));

        public static string PeekEucKR(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.EucKR.GetString(Peek8BitStringSpan(buffer, length));

        public static string GetEucKR(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.EucKR.GetString(Get8BitStringSpan(buffer, offset));

        public static string GetEucKR(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.EucKR.GetString(Get8BitStringSpan(buffer, offset, length));

        public static string ReadEucKR(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.EucKR.GetString(Read8BitStringSpan(buffer, ref offset));

        public static string ReadEucKR(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.EucKR.GetString(Read8BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String UTF16 Little Endian

        public static string PeekUTF16LittleEndian(ReadOnlySpan<byte> buffer)
            => EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan(buffer));

        public static string PeekUTF16LittleEndian(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan(buffer, length));

        public static string GetUTF16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(buffer, offset));

        public static string GetUTF16LittleEndian(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(buffer, offset, length));

        public static string ReadUTF16LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.UTF16LE.GetString(Read16BitStringSpan(buffer, ref offset));

        public static string ReadUTF16LittleEndian(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.UTF16LE.GetString(Read16BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String UTF16 Big Endian

        public static string PeekUTF16BigEndian(ReadOnlySpan<byte> buffer)
            => EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan(buffer));

        public static string PeekUTF16BigEndian(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan(buffer, length));

        public static string GetUTF16BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(buffer, offset));

        public static string GetUTF16BigEndian(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(buffer, offset, length));

        public static string ReadUTF16BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.UTF16BE.GetString(Read16BitStringSpan(buffer, ref offset));

        public static string ReadUTF16BigEndian(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.UTF16BE.GetString(Read16BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String UTF32 Little Endian

        public static string PeekUTF32LittleEndian(ReadOnlySpan<byte> buffer)
            => EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan(buffer));

        public static string PeekUTF32LittleEndian(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan(buffer, length));

        public static string GetUTF32LittleEndian(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(buffer, offset));

        public static string GetUTF32LittleEndian(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(buffer, offset, length));

        public static string ReadUTF32LittleEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.UTF32LE.GetString(Read32BitStringSpan(buffer, ref offset));

        public static string ReadUTF32LittleEndian(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.UTF32LE.GetString(Read32BitStringSpan(buffer, ref offset, length));

        #endregion

        #region String UTF32 Big Endian

        public static string PeekUTF32BigEndian(ReadOnlySpan<byte> buffer)
            => EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan(buffer));

        public static string PeekUTF32BigEndian(ReadOnlySpan<byte> buffer, int length)
            => EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan(buffer, length));

        public static string GetUTF32BigEndian(ReadOnlySpan<byte> buffer, int offset)
            => EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(buffer, offset));

        public static string GetUTF32BigEndian(ReadOnlySpan<byte> buffer, int offset, int length)
            => EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(buffer, offset, length));

        public static string ReadUTF32BigEndian(ReadOnlySpan<byte> buffer, ref int offset)
            => EncodingHelper.UTF32BE.GetString(Read32BitStringSpan(buffer, ref offset));

        public static string ReadUTF32BigEndian(ReadOnlySpan<byte> buffer, ref int offset, int length)
            => EncodingHelper.UTF32BE.GetString(Read32BitStringSpan(buffer, ref offset, length));

        #endregion
    }
}
