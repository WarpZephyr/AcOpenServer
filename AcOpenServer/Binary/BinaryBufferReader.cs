using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AcOpenServer.Binary
{
    public static class BinaryBufferReader
    {
        #region Generic

        public static T Read<T>(Span<byte> buffer) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref buffer[0]);

        public static T Read<T>(Span<byte> buffer, int offset) where T : unmanaged
            => Unsafe.ReadUnaligned<T>(ref buffer[offset]);

        public static T Read<T>(Span<byte> buffer, ref int offset) where T : unmanaged
        {
            var value = Unsafe.ReadUnaligned<T>(ref buffer[offset]);
            offset += Unsafe.SizeOf<T>();
            return value;
        }

        #endregion

        #region SByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte[] buffer)
            => (sbyte)buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte[] buffer, int offset)
            => (sbyte)buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte[] buffer, ref int offset)
            => (sbyte)buffer[offset++];

        #endregion

        #region Byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte[] buffer)
            => buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte[] buffer, int offset)
            => buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte[] buffer, ref int offset)
            => buffer[offset++];

        #endregion

        #region Int16

        public static short ReadInt16(Span<byte> buffer)
            => Unsafe.ReadUnaligned<short>(ref buffer[0]);

        public static short ReadInt16(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<short>(ref buffer[offset]);

        public static short ReadInt16(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[offset]);
            offset += sizeof(short);
            return value;
        }

        public static short ReadInt16BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static short ReadInt16BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static short ReadInt16BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(short);
            return value;
        }

        public static short ReadInt16LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static short ReadInt16LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static short ReadInt16LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<short>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(short);
            return value;
        }

        #endregion

        #region UInt16

        public static UInt16 ReadUInt16(Span<byte> buffer)
            => Unsafe.ReadUnaligned<UInt16>(ref buffer[0]);

        public static UInt16 ReadUInt16(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);

        public static UInt16 ReadUInt16(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);
            offset += sizeof(UInt16);
            return value;
        }

        public static UInt16 ReadUInt16BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt16 ReadUInt16BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt16 ReadUInt16BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt16);
            return value;
        }

        public static UInt16 ReadUInt16LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt16 ReadUInt16LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt16 ReadUInt16LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt16>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt16);
            return value;
        }

        #endregion

        #region Int32

        public static Int32 ReadInt32(Span<byte> buffer)
            => Unsafe.ReadUnaligned<Int32>(ref buffer[0]);

        public static Int32 ReadInt32(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);

        public static Int32 ReadInt32(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);
            offset += sizeof(Int32);
            return value;
        }

        public static Int32 ReadInt32BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int32 ReadInt32BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int32 ReadInt32BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(Int32);
            return value;
        }

        public static Int32 ReadInt32LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int32 ReadInt32LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int32 ReadInt32LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int32>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(Int32);
            return value;
        }

        #endregion

        #region UInt32

        public static UInt32 ReadUInt32(Span<byte> buffer)
            => Unsafe.ReadUnaligned<UInt32>(ref buffer[0]);

        public static UInt32 ReadUInt32(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);

        public static UInt32 ReadUInt32(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);
            offset += sizeof(UInt32);
            return value;
        }

        public static UInt32 ReadUInt32BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt32 ReadUInt32BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt32 ReadUInt32BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt32);
            return value;
        }

        public static UInt32 ReadUInt32LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt32 ReadUInt32LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt32 ReadUInt32LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt32>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt32);
            return value;
        }

        #endregion

        #region Int64

        public static Int64 ReadInt64(Span<byte> buffer)
            => Unsafe.ReadUnaligned<Int64>(ref buffer[0]);

        public static Int64 ReadInt64(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);

        public static Int64 ReadInt64(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);
            offset += sizeof(Int64);
            return value;
        }

        public static Int64 ReadInt64BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int64 ReadInt64BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int64 ReadInt64BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(Int64);
            return value;
        }

        public static Int64 ReadInt64LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int64 ReadInt64LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static Int64 ReadInt64LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Int64>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(Int64);
            return value;
        }

        #endregion

        #region UInt64

        public static UInt64 ReadUInt64(Span<byte> buffer)
            => Unsafe.ReadUnaligned<UInt64>(ref buffer[0]);

        public static UInt64 ReadUInt64(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);

        public static UInt64 ReadUInt64(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);
            offset += sizeof(UInt64);
            return value;
        }

        public static UInt64 ReadUInt64BigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt64 ReadUInt64BigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt64 ReadUInt64BigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt64);
            return value;
        }

        public static UInt64 ReadUInt64LittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt64 ReadUInt64LittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return value;
        }

        public static UInt64 ReadUInt64LittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<UInt64>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(UInt64);
            return value;
        }

        #endregion

        #region Half

        public static Half ReadHalf(Span<byte> buffer)
            => Unsafe.ReadUnaligned<Half>(ref buffer[0]);

        public static Half ReadHalf(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<Half>(ref buffer[offset]);

        public static Half ReadHalf(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<Half>(ref buffer[offset]);
            offset += Unsafe.SizeOf<Half>();
            return value;
        }

        public static Half ReadHalfBigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt16BitsToHalf(value);
        }

        public static Half ReadHalfBigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt16BitsToHalf(value);
        }

        public static Half ReadHalfBigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);

            offset += sizeof(ushort);
            return BitConverter.UInt16BitsToHalf(value);
        }

        public static Half ReadHalfLittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt16BitsToHalf(value);
        }

        public static Half ReadHalfLittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt16BitsToHalf(value);
        }

        public static Half ReadHalfLittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);

            offset += sizeof(ushort);
            return BitConverter.UInt16BitsToHalf(value);
        }

        #endregion

        #region Single

        public static float ReadSingle(Span<byte> buffer)
            => Unsafe.ReadUnaligned<float>(ref buffer[0]);

        public static float ReadSingle(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<float>(ref buffer[offset]);

        public static float ReadSingle(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<float>(ref buffer[offset]);
            offset += sizeof(float);
            return value;
        }

        public static float ReadSingleBigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt32BitsToSingle(value);
        }

        public static float ReadSingleBigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt32BitsToSingle(value);
        }

        public static float ReadSingleBigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(uint);
            return BitConverter.UInt32BitsToSingle(value);
        }

        public static float ReadSingleLittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt32BitsToSingle(value);
        }

        public static float ReadSingleLittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt32BitsToSingle(value);
        }

        public static float ReadSingleLittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(uint);
            return BitConverter.UInt32BitsToSingle(value);
        }

        #endregion

        #region Double

        public static double ReadDouble(Span<byte> buffer)
            => Unsafe.ReadUnaligned<double>(ref buffer[0]);

        public static double ReadDouble(Span<byte> buffer, int offset)
            => Unsafe.ReadUnaligned<double>(ref buffer[offset]);

        public static double ReadDouble(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<double>(ref buffer[offset]);
            offset += sizeof(double);
            return value;
        }

        public static double ReadDoubleBigEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[0]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt64BitsToDouble(value);
        }

        public static double ReadDoubleBigEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt64BitsToDouble(value);
        }

        public static double ReadDoubleBigEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[offset]);
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(ulong);
            return BitConverter.UInt64BitsToDouble(value);
        }

        public static double ReadDoubleLittleEndian(Span<byte> buffer)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[0]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt64BitsToDouble(value);
        }

        public static double ReadDoubleLittleEndian(Span<byte> buffer, int offset)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            return BitConverter.UInt64BitsToDouble(value);
        }

        public static double ReadDoubleLittleEndian(Span<byte> buffer, ref int offset)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref buffer[offset]);
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);
            offset += sizeof(ulong);
            return BitConverter.UInt64BitsToDouble(value);
        }

        #endregion

        #region Char

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(Span<byte> buffer)
            => (char)buffer[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(Span<byte> buffer, int offset)
            => (char)buffer[offset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(Span<byte> buffer, ref int offset)
            => (char)buffer[offset++];

        #endregion

        #region Boolean

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean(Span<byte> buffer)
        {
            var b = buffer[0];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean(Span<byte> buffer, int offset)
        {
            var b = buffer[offset];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean(Span<byte> buffer, ref int offset)
        {
            var b = buffer[offset++];
            return b == 1 || (b == 0 ? false : throw new InvalidDataException($"Value for {nameof(Boolean)} read is not {0} or {1}: {b}"));
        }

        #endregion

        #region String UTF8

        public static string ReadUTF8(Span<byte> buffer)
        {
            int terminatorIndex;
            for (terminatorIndex = 0; terminatorIndex < buffer.Length; terminatorIndex++)
                if (buffer[terminatorIndex] == 0)
                    break;

            if (terminatorIndex == buffer.Length)
                return Encoding.UTF8.GetString(buffer);

            return Encoding.UTF8.GetString(buffer[..terminatorIndex]);
        }

        public static string ReadUTF8(Span<byte> buffer, int offset)
        {
            int terminatorIndex;
            int offsetLength = buffer.Length + offset;
            for (terminatorIndex = 0; (terminatorIndex + offset) < offsetLength; terminatorIndex++)
                if (buffer[terminatorIndex + offset] == 0)
                    break;

            return Encoding.UTF8.GetString(buffer.Slice(offset, terminatorIndex));
        }

        public static string ReadUTF8(Span<byte> buffer, ref int offset)
        {
            int terminatorIndex;
            int offsetLength = buffer.Length + offset;
            for (terminatorIndex = 0; (terminatorIndex + offset) < offsetLength; terminatorIndex++)
                if (buffer[terminatorIndex + offset] == 0)
                    break;

            offset += terminatorIndex;
            offset += 1;
            return Encoding.UTF8.GetString(buffer.Slice(offset, terminatorIndex));
        }

        public static string ReadFixedUTF8(Span<byte> buffer, int length)
        {
            int terminatorIndex;
            int safeLength = Math.Min(length, buffer.Length);
            for (terminatorIndex = 0; terminatorIndex < safeLength; terminatorIndex++)
                if (buffer[terminatorIndex] == 0)
                    break;

            if (terminatorIndex == length)
                return Encoding.UTF8.GetString(buffer);

            return Encoding.UTF8.GetString(buffer[..terminatorIndex]);
        }

        public static string ReadFixedUTF8(Span<byte> buffer, int length, int offset)
        {
            int terminatorIndex;
            int offsetLength = Math.Min(length, buffer.Length) + offset;
            for (terminatorIndex = 0; (terminatorIndex + offset) < offsetLength; terminatorIndex++)
                if (buffer[terminatorIndex + offset] == 0)
                    break;

            return Encoding.UTF8.GetString(buffer.Slice(offset, terminatorIndex));
        }

        public static string ReadFixedUTF8(Span<byte> buffer, int length, ref int offset)
        {
            int terminatorIndex;
            int offsetLength = Math.Min(length, buffer.Length) + offset;
            for (terminatorIndex = 0; (terminatorIndex + offset) < offsetLength; terminatorIndex++)
                if (buffer[terminatorIndex + offset] == 0)
                    break;

            string value = Encoding.UTF8.GetString(buffer.Slice(offset, terminatorIndex));
            offset += length;
            return value;
        }

        #endregion
    }
}
