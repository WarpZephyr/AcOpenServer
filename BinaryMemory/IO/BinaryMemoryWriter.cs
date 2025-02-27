using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A writer that writes to a region of memory with a fixed length.
    /// </summary>
    public class BinaryMemoryWriter : IBinaryWriter
    {
        /// <summary>
        /// The underlying memory.
        /// </summary>
        private readonly Memory<byte> _memory;

        /// <summary>
        /// Steps into positions.
        /// </summary>
        private readonly Stack<int> _steps;

        /// <summary>
        /// The reservations for values.
        /// </summary>
        private readonly Dictionary<string, int> _reservations;

        /// <summary>
        /// The current position of the writer.
        /// </summary>
        // Placed in a field so that range checking can happen in the property.
        private int _position;

        /// <summary>
        /// The current position of the writer.
        /// </summary>
        public long Position
        {
            get => _position;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)value, (uint)_length, nameof(value));

                _position = (int)value;
            }
        }

        /// <summary>
        /// Whether or not to read in big endian byte ordering.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// How many bytes to write for variable sized values.<para/>
        /// Valid sizes for integers:<br/>
        /// 1,2,4,8<br/>
        /// <br/>
        /// Valid sizes for precise numbers:<br/>
        /// 2,4,8
        /// </summary>
        public int VariableValueSize { get; set; }

        /// <summary>
        /// The backing memory.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => _memory;

        /// <summary>
        /// The length of the underlying memory.
        /// </summary>
        private int _length => _memory.Length;

        /// <summary>
        /// The length of the underlying memory.
        /// </summary>
        public long Length => _length;

        /// <summary>
        /// The remaining length starting from the current position.
        /// </summary>
        private int _remaining => _length - _position;

        /// <summary>
        /// The remaining length starting from the current position.
        /// </summary>
        public long Remaining => _remaining;

        /// <summary>
        /// The amount of positions the writer is stepped into.
        /// </summary>
        public int StepInCount => _steps.Count;

        /// <summary>
        /// Create a <see cref="BinaryMemoryWriter"/> over a region of memory.
        /// </summary>
        /// <param name="memory">A region of memory.</param>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        public BinaryMemoryWriter(Memory<byte> memory, bool bigEndian = false)
        {
            _memory = memory;
            _steps = new Stack<int>();
            _reservations = [];
            BigEndian = bigEndian;
        }

        /// <summary>
        /// Create a <see cref="BinaryMemoryWriter"/> over a new region of memory of the specified capacity.
        /// </summary>
        /// <param name="capacity">The size of the region of memory.</param>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        public BinaryMemoryWriter(int capacity, bool bigEndian = false) : this(new byte[capacity], bigEndian) { }

        #region Stream

        public Stream ToStream()
            => new MemoryStream(_memory.ToArray(), true);

        public Stream GetStream(int length)
            => new MemoryStream(_memory.Slice(_position, length).ToArray(), true);

        public Stream GetStream(int position, int length)
            => new MemoryStream(_memory.Slice(position, length).ToArray(), true);

        #endregion

        #region Position

        public void Advance()
            => _position++;

        public void Advance(int count)
            => _position += count;

        public void Rewind()
            => _position--;

        public void Rewind(int count)
            => _position -= count;

        public void GotoStart()
            => _position = 0;

        public void GotoEnd()
            => _position = _length;

        #endregion

        #region Align

        public void Align(int alignment)
        {
            int remainder = _position % alignment;
            if (remainder > 0)
            {
                _position += alignment - remainder;
            }
        }

        public void AlignFrom(long position, int alignment)
        {
            long remainder = position % alignment;
            if (remainder > 0)
            {
                position += alignment - remainder;
            }
            Position = position;
        }

        #endregion

        #region Pad

        /// <summary>
        /// Writes null bytes until the position meets the specified alignment.
        /// </summary>
        public void Pad(int alignment)
        {
            long remainder = Position % alignment;
            if (remainder > 0)
            {
                long count = alignment - remainder;
                while (count > 0)
                {
                    WriteByte(0);
                    count--;
                }
            }
        }

        #endregion

        #region Step

        public void StepIn(long position)
        {
            _steps.Push(_position);
            Position = position;
        }

        public void StepOut()
        {
            if (_steps.Count < 1)
            {
                throw new InvalidOperationException("Writer is already stepped all the way out.");
            }

            _position = _steps.Pop();
        }

        #endregion

        #region Write

        private void WriteEndian<T>(T value, Func<T, T> reverseEndianness) where T : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                value = reverseEndianness(value);
            }

            Write(value);
        }

        public unsafe void Write<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            int endPosition = _position + size;
            if (endPosition > _length)
            {
                throw new InvalidOperationException("Cannot write beyond the specified region of memory.");
            }

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(_memory.Span), _position), value);
            _position = endPosition;
        }

        public void WriteSByte(sbyte value)
            => Write(value);

        public void WriteByte(byte value)
            => Write(value);

        public void WriteInt16(short value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt16(ushort value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteInt32(int value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt32(uint value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteInt64(long value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt64(ulong value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteInt128(Int128 value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt128(UInt128 value)
            => WriteEndian(value, BinaryPrimitives.ReverseEndianness);

        public void WriteHalf(Half value)
            => WriteEndian(BitConverter.HalfToUInt16Bits(value), BinaryPrimitives.ReverseEndianness);

        public void WriteSingle(float value)
            => WriteEndian(BitConverter.SingleToUInt32Bits(value), BinaryPrimitives.ReverseEndianness);

        public void WriteDouble(double value)
            => WriteEndian(BitConverter.DoubleToUInt64Bits(value), BinaryPrimitives.ReverseEndianness);

        public void WriteBoolean(bool value)
            => Write((byte)(value ? 1 : 0));

        public void WriteChar(char value)
            => Write(value);

        public void WriteVector2(Vector2 value)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                WriteSingle(value.X);
                WriteSingle(value.Y);
            }
            else
            {
                Write(value);
            }
        }

        public void WriteVector3(Vector3 value)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                WriteSingle(value.X);
                WriteSingle(value.Y);
                WriteSingle(value.Z);
            }
            else
            {
                Write(value);
            }
        }

        public void WriteVector4(Vector4 value)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                WriteSingle(value.X);
                WriteSingle(value.Y);
                WriteSingle(value.Z);
                WriteSingle(value.W);
            }
            else
            {
                Write(value);
            }
        }

        public void WriteQuaternion(Quaternion value)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                WriteSingle(value.X);
                WriteSingle(value.Y);
                WriteSingle(value.Z);
                WriteSingle(value.W);
            }
            else
            {
                Write(value);
            }
        }

        public void WriteColor3(byte[] color)
        {
            Write(color[0]);
            Write(color[1]);
            Write(color[2]);
        }

        public void WriteColorRGB(Color color)
        {
            Write(color.R);
            Write(color.G);
            Write(color.B);
        }

        public void WriteColorBGR(Color color)
        {
            Write(color.B);
            Write(color.G);
            Write(color.R);
        }

        public void WriteColor4(byte[] color)
        {
            Write(color[0]);
            Write(color[1]);
            Write(color[2]);
            Write(color[4]);
        }

        public void WriteColorRGBA(Color color)
        {
            Write(color.R);
            Write(color.G);
            Write(color.B);
            Write(color.A);
        }

        public void WriteColorBGRA(Color color)
        {
            Write(color.B);
            Write(color.G);
            Write(color.R);
            Write(color.A);
        }

        public void WriteColorARGB(Color color)
        {
            Write(color.A);
            Write(color.R);
            Write(color.G);
            Write(color.B);
        }

        public void WriteColorABGR(Color color)
        {
            Write(color.A);
            Write(color.B);
            Write(color.G);
            Write(color.R);
        }

        public void WriteEnumSByte<TEnum>(TEnum value) where TEnum : Enum
            => WriteSByte((sbyte)(object)value);

        public void WriteEnumByte<TEnum>(TEnum value) where TEnum : Enum
            => WriteByte((byte)(object)value);

        public void WriteEnumInt16<TEnum>(TEnum value) where TEnum : Enum
            => WriteInt16((short)(object)value);

        public void WriteEnumUInt16<TEnum>(TEnum value) where TEnum : Enum
            => WriteUInt16((ushort)(object)value);

        public void WriteEnumInt32<TEnum>(TEnum value) where TEnum : Enum
            => WriteInt32((int)(object)value);

        public void WriteEnumUInt32<TEnum>(TEnum value) where TEnum : Enum
            => WriteUInt32((uint)(object)value);

        public void WriteEnumInt64<TEnum>(TEnum value) where TEnum : Enum
            => WriteInt64((long)(object)value);

        public void WriteEnumUInt64<TEnum>(TEnum value) where TEnum : Enum
            => WriteUInt64((ulong)(object)value);

        public void WriteEnum<TEnum>(TEnum value) where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(sbyte))
            {
                WriteEnumSByte(value);
            }
            else if (type == typeof(byte))
            {
                WriteEnumByte(value);
            }
            else if (type == typeof(short))
            {
                WriteEnumInt16(value);
            }
            else if (type == typeof(ushort))
            {
                WriteEnumUInt16(value);
            }
            else if (type == typeof(int))
            {
                WriteEnumInt32(value);
            }
            else if (type == typeof(uint))
            {
                WriteEnumUInt32(value);
            }
            else if (type == typeof(long))
            {
                WriteEnumInt64(value);
            }
            else if (type == typeof(ulong))
            {
                WriteEnumUInt64(value);
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an unknown underlying value type: {type.Name}");
            }
        }

        public void WriteEnum8<TEnum>(TEnum value) where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(sbyte))
            {
                WriteEnumSByte(value);
            }
            else if (type == typeof(byte))
            {
                WriteEnumByte(value);
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public void WriteEnum16<TEnum>(TEnum value) where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(short))
            {
                WriteEnumInt16(value);
            }
            else if (type == typeof(ushort))
            {
                WriteEnumUInt16(value);
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public void WriteEnum32<TEnum>(TEnum value) where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(int))
            {
                WriteEnumInt32(value);
            }
            else if (type == typeof(uint))
            {
                WriteEnumUInt32(value);
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public void WriteEnum64<TEnum>(TEnum value) where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(long))
            {
                WriteEnumInt64(value);
            }
            else if (type == typeof(ulong))
            {
                WriteEnumUInt64(value);
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public void WriteSignedVarVal(long value)
        {
            switch (VariableValueSize)
            {
                case 1: WriteSByte((sbyte)value); break;
                case 2: WriteInt16((short)value); break;
                case 4: WriteInt32((int)value); break;
                case 8: WriteInt64(value); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WriteSignedVarVal)}");
            };
        }

        public void WriteUnsignedVarVal(ulong value)
        {
            switch (VariableValueSize)
            {
                case 1: WriteByte((byte)value); break;
                case 2: WriteUInt16((ushort)value); break;
                case 4: WriteUInt32((uint)value); break;
                case 8: WriteUInt64(value); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WriteUnsignedVarVal)}");
            };
        }

        public void WritePreciseVarVal(double value)
        {
            switch (VariableValueSize)
            {
                case 2: WriteHalf((Half)value); break;
                case 4: WriteSingle((float)value); break;
                case 8: WriteDouble(value); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WritePreciseVarVal)}");
            };
        }

        public void WriteString(string value, Encoding encoding, bool terminate = false)
            => WriteBytes(encoding.GetBytes(terminate ? value + '\0' : value));

        public void WriteFixedString(string value, Encoding encoding, int byteLength, byte paddingValue = 0)
        {
            byte[] bytes = new byte[byteLength];
            if (paddingValue != 0)
            {
                for (int i = 0; i < byteLength; i++)
                {
                    bytes[i] = paddingValue;
                }
            }

            byte[] valueBytes = encoding.GetBytes(value);
            Array.Copy(valueBytes, bytes, Math.Min(byteLength, valueBytes.Length));
            WriteBytes(bytes);
        }

        public void WriteUTF8(string value, bool terminate = false)
            => WriteString(value, Encoding.UTF8, terminate);

        public void WriteASCII(string value, bool terminate = false)
            => WriteString(value, Encoding.ASCII, terminate);

        public void WriteShiftJIS(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.ShiftJIS, terminate);

        public void WriteEucJP(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.EucJP, terminate);

        public void WriteEucCN(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.EucCN, terminate);

        public void WriteEucKR(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.EucKR, terminate);

        public void WriteUTF16(string value, bool terminate = false)
            => WriteString(value, BigEndian ? EncodingHelper.UTF16BE : EncodingHelper.UTF16LE, terminate);

        public void WriteUTF16LittleEndian(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.UTF16LE, terminate);

        public void WriteUTF16BigEndian(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.UTF16BE, terminate);

        public void WriteUTF32(string value, bool terminate = false)
            => WriteString(value, BigEndian ? EncodingHelper.UTF32BE : EncodingHelper.UTF32LE, terminate);

        public void WriteUTF32LittleEndian(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.UTF32LE, terminate);

        public void WriteUTF32BigEndian(string value, bool terminate = false)
            => WriteString(value, EncodingHelper.UTF32BE, terminate);

        public void WriteFixedUTF8(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, Encoding.UTF8, length, paddingValue);

        public void WriteFixedASCII(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, Encoding.ASCII, length, paddingValue);

        public void WriteFixedShiftJIS(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.ShiftJIS, length, paddingValue);

        public void WriteFixedEucJP(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.EucJP, length, paddingValue);

        public void WriteFixedEucCN(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.EucCN, length, paddingValue);

        public void WriteFixedEucKR(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.EucKR, length, paddingValue);

        public void WriteFixedUTF16(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, BigEndian ? EncodingHelper.UTF16BE : EncodingHelper.UTF16LE, length * 2, paddingValue);

        public void WriteFixedUTF16LittleEndian(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.UTF16LE, length * 2, paddingValue);

        public void WriteFixedUTF16BigEndian(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.UTF16BE, length * 2, paddingValue);

        public void WriteFixedUTF32(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, BigEndian ? EncodingHelper.UTF32BE : EncodingHelper.UTF32LE, length * 4, paddingValue);

        public void WriteFixedUTF32LittleEndian(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.UTF32LE, length * 4, paddingValue);

        public void WriteFixedUTF32BigEndian(string value, int length, byte paddingValue = 0)
            => WriteFixedString(value, EncodingHelper.UTF32BE, length * 4, paddingValue);

        #endregion

        #region Reserve

        private void Reserve(string name, string typeName, int length)
        {
            name = $"{name}:{typeName}";
            if (_reservations.ContainsKey(name))
            {
                throw new ArgumentException($"Key already reserved: {name}", nameof(name));
            }

            _reservations[name] = _position;
            for (int i = 0; i < length; i++)
            {
                WriteByte(0xFE);
            }
        }

        public unsafe void Reserve<T>(string name) where T : unmanaged
            => Reserve(name, "unmanaged", sizeof(T));

        public void ReserveSByte(string name)
            => Reserve(name, "SByte", 1);

        public void ReserveByte(string name)
            => Reserve(name, "Byte", 1);

        public void ReserveInt16(string name)
            => Reserve(name, "Int16", 2);

        public void ReserveUInt16(string name)
            => Reserve(name, "UInt16", 2);

        public void ReserveInt32(string name)
            => Reserve(name, "Int32", 4);

        public void ReserveUInt32(string name)
            => Reserve(name, "UInt32", 4);

        public void ReserveInt64(string name)
            => Reserve(name, "Int64", 8);

        public void ReserveUInt64(string name)
            => Reserve(name, "UInt64", 8);

        public void ReserveInt128(string name)
            => Reserve(name, "Int128", 16);

        public void ReserveUInt128(string name)
            => Reserve(name, "UInt128", 16);

        public void ReserveHalf(string name)
            => Reserve(name, "Half", 2);

        public void ReserveSingle(string name)
            => Reserve(name, "Single", 4);

        public void ReserveDouble(string name)
            => Reserve(name, "Double", 8);

        public void ReserveBoolean(string name)
            => Reserve(name, "Boolean", 1);

        public void ReserveVector2(string name)
            => Reserve(name, "Vector2", 8);

        public void ReserveVector3(string name)
            => Reserve(name, "Vector3", 12);

        public void ReserveVector4(string name)
            => Reserve(name, "Vector4", 16);

        public void ReserveQuaternion(string name)
            => Reserve(name, "Quaternion", 16);

        #endregion

        #region Fill

        private int TakeReservation(string name, string typeName)
        {
            name = $"{name}:{typeName}";
            if (!_reservations.TryGetValue(name, out int jump))
            {
                throw new ArgumentException($"Key is not reserved: {name}", nameof(name));
            }

            _reservations.Remove(name);
            return jump;
        }

        private void Fill<T>(string name, string typeName, Action<T> write, T value)
        {
            int returnPosition = _position;
            _position = TakeReservation(name, typeName);
            write(value);
            _position = returnPosition;
        }

        public void Fill<T>(string name, T value) where T : unmanaged
            => Fill(name, "unmanaged", Write, value);

        public void FillSByte(string name, sbyte value)
            => Fill(name, nameof(SByte), WriteSByte, value);

        public void FillByte(string name, byte value)
            => Fill(name, nameof(Byte), WriteByte, value);

        public void FillInt16(string name, short value)
            => Fill(name, nameof(Int16), WriteInt16, value);

        public void FillUInt16(string name, ushort value)
            => Fill(name, nameof(UInt16), WriteUInt16, value);

        public void FillInt32(string name, int value)
            => Fill(name, nameof(Int32), WriteInt32, value);

        public void FillUInt32(string name, uint value)
            => Fill(name, nameof(UInt32), WriteUInt32, value);

        public void FillInt64(string name, long value)
            => Fill(name, nameof(Int64), WriteInt64, value);

        public void FillUInt64(string name, ulong value)
            => Fill(name, nameof(UInt64), WriteUInt64, value);

        public void FillInt128(string name, Int128 value)
            => Fill(name, nameof(Int64), WriteInt128, value);

        public void FillUInt128(string name, UInt128 value)
            => Fill(name, nameof(UInt128), WriteUInt128, value);

        public void FillHalf(string name, Half value)
            => Fill(name, nameof(Half), WriteHalf, value);

        public void FillSingle(string name, float value)
            => Fill(name, nameof(Single), WriteSingle, value);

        public void FillDouble(string name, double value)
            => Fill(name, nameof(Double), WriteDouble, value);

        public void FillBoolean(string name, bool value)
            => Fill(name, nameof(Boolean), WriteBoolean, value);

        public void FillVector2(string name, Vector2 value)
            => Fill(name, nameof(Vector2), WriteVector2, value);

        public void FillVector3(string name, Vector3 value)
            => Fill(name, nameof(Vector3), WriteVector3, value);

        public void FillVector4(string name, Vector4 value)
            => Fill(name, nameof(Vector4), WriteVector4, value);

        public void FillQuaternion(string name, Quaternion value)
            => Fill(name, nameof(Quaternion), WriteQuaternion, value);

        #endregion

        #region Write Array

        private void WriteArray<T>(IList<T> values) where T : unmanaged
        {
            foreach (T value in values)
            {
                Write(value);
            }
        }

        private void WriteArrayCast<TFrom, TTo>(IList<TFrom> values, Func<TFrom, TTo> cast)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            foreach (TFrom value in values)
            {
                Write(cast(value));
            }
        }

        private void WriteArrayEndian<T>(IList<T> values, Func<T, T> reverseEndianness) where T : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                foreach (T value in values)
                {
                    Write(reverseEndianness(value));
                }
            }
            else
            {
                foreach (T value in values)
                {
                    Write(value);
                }
            }
        }

        private void WriteArrayConvertEndian<TFrom, TTo>(IList<TFrom> values, Func<TFrom, TTo> convert, Func<TTo, TTo> reverseEndianness)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                foreach (TFrom value in values)
                {
                    Write(reverseEndianness(convert(value)));
                }
            }
            else
            {
                foreach (TFrom value in values)
                {
                    Write(value);
                }
            }
        }

        private void WriteArrayCastEndian<TFrom, TTo>(IList<TFrom> values, Func<TFrom, TTo> cast, Func<TTo, TTo> reverseEndianness)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                foreach (TFrom value in values)
                {
                    Write(reverseEndianness(cast(value)));
                }
            }
            else
            {
                foreach (TFrom value in values)
                {
                    Write(cast(value));
                }
            }
        }

        private void WriteArrayCastConvertEndian<TWrite, TFrom, TTo>(IList<TFrom> values, Func<TFrom, TTo> cast, Func<TTo, TWrite> convert, Func<TWrite, TWrite> reverseEndianness)
            where TWrite : unmanaged
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Write(reverseEndianness(convert(cast(values[i]))));
                }
            }
            else
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Write(convert(cast(values[i])));
                }
            }
        }

        public void WriteSBytes(IList<sbyte> values)
            => WriteArray(values);

        public void WriteBytes(IList<byte> values)
            => WriteArray(values);

        public void WriteInt16s(IList<short> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt16s(IList<ushort> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteInt32s(IList<int> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt32s(IList<uint> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteInt64s(IList<long> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt64s(IList<ulong> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteInt128s(IList<Int128> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteUInt128s(IList<UInt128> values)
            => WriteArrayEndian(values, BinaryPrimitives.ReverseEndianness);

        public void WriteHalfs(IList<Half> values)
            => WriteArrayConvertEndian(values, BitConverter.HalfToUInt16Bits, BinaryPrimitives.ReverseEndianness);

        public void WriteSingles(IList<float> values)
            => WriteArrayConvertEndian(values, BitConverter.SingleToUInt32Bits, BinaryPrimitives.ReverseEndianness);

        public void WriteDoubles(IList<double> values)
            => WriteArrayConvertEndian(values, BitConverter.DoubleToUInt64Bits, BinaryPrimitives.ReverseEndianness);

        public void WriteBooleans(IList<bool> values)
        {
            foreach (bool value in values)
            {
                WriteBoolean(value);
            }
        }

        public void WriteSignedVarVals(IList<long> values)
        {
            switch (VariableValueSize)
            {
                case 1: WriteArrayCast(values, Convert.ToSByte); break;
                case 2: WriteArrayCastEndian(values, Convert.ToInt16, BinaryPrimitives.ReverseEndianness); break;
                case 4: WriteArrayCastEndian(values, Convert.ToInt32, BinaryPrimitives.ReverseEndianness); break;
                case 8: WriteInt64s(values); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WriteSignedVarVals)}");
            };
        }

        public void WriteUnsignedVarVals(IList<ulong> values)
        {
            switch (VariableValueSize)
            {
                case 1: WriteArrayCast(values, Convert.ToByte); break;
                case 2: WriteArrayCastEndian(values, Convert.ToUInt16, BinaryPrimitives.ReverseEndianness); break;
                case 4: WriteArrayCastEndian(values, Convert.ToUInt32, BinaryPrimitives.ReverseEndianness); break;
                case 8: WriteUInt64s(values); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WriteUnsignedVarVals)}");
            };
        }

        public void WritePreciseVarVals(IList<double> values)
        {
            switch (VariableValueSize)
            {
                case 2: WriteArrayCastConvertEndian(values, (value) => (Half)value, BitConverter.HalfToUInt16Bits, BinaryPrimitives.ReverseEndianness); break;
                case 4: WriteArrayCastConvertEndian(values, Convert.ToSingle, BitConverter.SingleToUInt32Bits, BinaryPrimitives.ReverseEndianness); break;
                case 8: WriteDoubles(values); break;
                default:
                    throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(WritePreciseVarVals)}");
            };
        }

        #endregion

    }
}
