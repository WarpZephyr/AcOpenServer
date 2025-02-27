using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A reader for data present in a region of memory.
    /// </summary>
    public class BinaryMemoryReader : IBinaryReader
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
        /// The current position of the reader.
        /// </summary>
        // Placed in a field so that range checking can happen in the property.
        private int _position;

        /// <summary>
        /// The current position of the reader.
        /// </summary>
        public long Position
        {
            get => _position;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)value, (uint)_memory.Length, nameof(value));

                _position = (int)value;
            }
        }

        /// <summary>
        /// Whether or not to read in big endian byte ordering.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// How many bytes to read for variable sized values.<para/>
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
        /// The amount of positions the reader is stepped into.
        /// </summary>
        public int StepInCount => _steps.Count;

        /// <summary>
        /// Create a <see cref="BinaryMemoryReader"/> over a region of memory.
        /// </summary>
        /// <param name="memory">A region of memory.</param>
        /// <param name="bigEndian">Whether or not to read in big endian byte ordering.</param>
        public BinaryMemoryReader(Memory<byte> memory, bool bigEndian = false)
        {
            _memory = memory;
            _steps = new Stack<int>();

            BigEndian = bigEndian;
        }

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
                throw new InvalidOperationException("Reader is already stepped all the way out.");
            }

            _position = _steps.Pop();
        }

        #endregion

        #region Read

        private T ReadEndian<T>(Func<T, T> reverseEndianness) where T : unmanaged
        {
            var value = Read<T>();
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                return reverseEndianness(value);
            }
            return value;
        }

        private TTo ReadEndianConvert<TFrom, TTo>(Func<TFrom, TFrom> reverseEndianness, Func<TFrom, TTo> convert)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                return convert(reverseEndianness(Read<TFrom>()));
            }
            return Read<TTo>();
        }

        private static TEnum ReadEnum<TEnum, TValue>(Func<TValue> read, string valueFormat)
            where TEnum : Enum
            where TValue : notnull
        {
            TValue value = read();
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new InvalidDataException($"Read value not present in enum: {string.Format(valueFormat, value)}");
            }
            return (TEnum)(object)value;
        }

        protected byte[] Read8BitTerminatedStringBytes()
        {
            var bytes = new List<byte>();
            byte b = ReadByte();
            while (b != 0)
            {
                bytes.Add(b);
                b = ReadByte();
            }
            return [.. bytes];
        }

        protected byte[] Read16BitTerminatedStringBytes()
        {
            var bytes = new List<byte>();
            byte a = ReadByte();
            byte b = ReadByte();
            while ((a | b) != 0)
            {
                bytes.Add(a);
                bytes.Add(b);
                a = ReadByte();
                b = ReadByte();
            }
            return [.. bytes];
        }

        protected byte[] Read32BitTerminatedStringBytes()
        {
            var bytes = new List<byte>();
            byte a = ReadByte();
            byte b = ReadByte();
            byte c = ReadByte();
            byte d = ReadByte();
            while ((a | b | c | d) != 0)
            {
                bytes.Add(a);
                bytes.Add(b);
                bytes.Add(c);
                bytes.Add(d);
                a = ReadByte();
                b = ReadByte();
                c = ReadByte();
                d = ReadByte();
            }
            return [.. bytes];
        }

        protected byte[] ReadTerminatedStringBytes(int bytesPerChar)
        {
            var bytes = new List<byte>();
            byte[] readBytes = ReadBytes(bytesPerChar);

            bool IsNull()
            {
                foreach (byte b in readBytes)
                {
                    if (b != 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            while (!IsNull())
            {
                bytes.AddRange(readBytes);
                readBytes = ReadBytes(bytesPerChar);
            }

            return [.. bytes];
        }

        public unsafe T Read<T>() where T : unmanaged
        {
            int size = sizeof(T);
            int endPosition = _position + size;
            if (endPosition > _memory.Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified region of memory.");
            }

            var value = Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(_memory.Span), _position));
            _position = endPosition;
            return value;
        }

        public sbyte ReadSByte()
            => Read<sbyte>();

        public byte ReadByte()
            => Read<byte>();

        public short ReadInt16()
            => ReadEndian<short>(BinaryPrimitives.ReverseEndianness);

        public ushort ReadUInt16()
            => ReadEndian<ushort>(BinaryPrimitives.ReverseEndianness);

        public int ReadInt32()
            => ReadEndian<int>(BinaryPrimitives.ReverseEndianness);

        public uint ReadUInt32()
            => ReadEndian<uint>(BinaryPrimitives.ReverseEndianness);

        public long ReadInt64()
            => ReadEndian<long>(BinaryPrimitives.ReverseEndianness);

        public ulong ReadUInt64()
            => ReadEndian<ulong>(BinaryPrimitives.ReverseEndianness);

        public Int128 ReadInt128()
            => ReadEndian<Int128>(BinaryPrimitives.ReverseEndianness);

        public UInt128 ReadUInt128()
            => ReadEndian<UInt128>(BinaryPrimitives.ReverseEndianness);

        public Half ReadHalf()
            => ReadEndianConvert<ushort, Half>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt16BitsToHalf);

        public float ReadSingle()
            => ReadEndianConvert<uint, float>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt32BitsToSingle);

        public double ReadDouble()
            => ReadEndianConvert<ulong, double>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt64BitsToDouble);

#if UNSTRICT_READ_BOOLEAN
        public bool ReadBoolean()
            => ReadByte() != 0;
#else
        public bool ReadBoolean()
        {
            byte value = ReadByte();
            if (value == 0)
            {
                return false;
            }
            else if (value == 1)
            {
                return true;
            }

            throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}");
        }
#endif

        public char ReadChar()
            => Read<char>();

        public Vector2 ReadVector2()
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                float x = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float y = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                return new Vector2(x, y);
            }

            return Read<Vector2>();
        }

        public Vector3 ReadVector3()
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                float x = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float y = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float z = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                return new Vector3(x, y, z);
            }

            return Read<Vector3>();
        }

        public Vector4 ReadVector4()
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                float x = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float y = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float z = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float w = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                return new Vector4(x, y, z, w);
            }

            return Read<Vector4>();
        }

        public Quaternion ReadQuaternion()
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                float x = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float y = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float z = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                float w = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                return new Quaternion(x, y, z, w);
            }

            return Read<Quaternion>();
        }

        public byte[] ReadColor3()
            => ReadBytes(3);

        public Color ReadColorRGB()
        {
            var bytes = ReadSpan<byte>(3);
            return Color.FromArgb(255, bytes[0], bytes[1], bytes[2]);
        }

        public Color ReadColorBGR()
        {
            var bytes = ReadSpan<byte>(3);
            return Color.FromArgb(255, bytes[2], bytes[1], bytes[0]);
        }

        public byte[] ReadColor4()
            => ReadBytes(4);

        public Color ReadColorRGBA()
        {
            var bytes = ReadSpan<byte>(4);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        public Color ReadColorBGRA()
        {
            var bytes = ReadSpan<byte>(4);
            return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public Color ReadColorARGB()
        {
            var bytes = ReadSpan<byte>(4);
            return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        public Color ReadColorABGR()
        {
            var bytes = ReadSpan<byte>(4);
            return Color.FromArgb(bytes[0], bytes[3], bytes[2], bytes[1]);
        }

        public TEnum ReadEnumSByte<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, sbyte>(ReadSByte, "0x{0:X}");

        public TEnum ReadEnumByte<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, byte>(ReadByte, "0x{0:X}");

        public TEnum ReadEnumInt16<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, short>(ReadInt16, "0x{0:X}");

        public TEnum ReadEnumUInt16<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, ushort>(ReadUInt16, "0x{0:X}");

        public TEnum ReadEnumInt32<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, int>(ReadInt32, "0x{0:X}");

        public TEnum ReadEnumUInt32<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, uint>(ReadUInt32, "0x{0:X}");

        public TEnum ReadEnumInt64<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, long>(ReadInt64, "0x{0:X}");

        public TEnum ReadEnumUInt64<TEnum>() where TEnum : Enum
            => ReadEnum<TEnum, ulong>(ReadUInt64, "0x{0:X}");

        public TEnum ReadEnum<TEnum>() where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(sbyte))
            {
                return ReadEnumSByte<TEnum>();
            }
            else if (type == typeof(byte))
            {
                return ReadEnumByte<TEnum>();
            }
            else if (type == typeof(short))
            {
                return ReadEnumInt16<TEnum>();
            }
            else if (type == typeof(ushort))
            {
                return ReadEnumUInt16<TEnum>();
            }
            else if (type == typeof(int))
            {
                return ReadEnumInt32<TEnum>();
            }
            else if (type == typeof(uint))
            {
                return ReadEnumUInt32<TEnum>();
            }
            else if (type == typeof(long))
            {
                return ReadEnumInt64<TEnum>();
            }
            else if (type == typeof(ulong))
            {
                return ReadEnumUInt64<TEnum>();
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an unknown underlying value type: {type.Name}");
            }
        }

        public TEnum ReadEnum8<TEnum>() where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(sbyte))
            {
                return ReadEnumSByte<TEnum>();
            }
            else if (type == typeof(byte))
            {
                return ReadEnumByte<TEnum>();
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public TEnum ReadEnum16<TEnum>() where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(short))
            {
                return ReadEnumInt16<TEnum>();
            }
            else if (type == typeof(ushort))
            {
                return ReadEnumUInt16<TEnum>();
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public TEnum ReadEnum32<TEnum>() where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(int))
            {
                return ReadEnumInt32<TEnum>();
            }
            else if (type == typeof(uint))
            {
                return ReadEnumUInt32<TEnum>();
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public TEnum ReadEnum64<TEnum>() where TEnum : Enum
        {
            Type type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(long))
            {
                return ReadEnumInt64<TEnum>();
            }
            else if (type == typeof(ulong))
            {
                return ReadEnumUInt64<TEnum>();
            }
            else
            {
                throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {type.Name}");
            }
        }

        public long ReadSignedVarVal()
        {
            return VariableValueSize switch
            {
                1 => ReadSByte(),
                2 => ReadInt16(),
                4 => ReadInt32(),
                8 => ReadInt64(),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(ReadSignedVarVal)}"),
            };
        }

        public ulong ReadUnsignedVarVal()
        {
            return VariableValueSize switch
            {
                1 => ReadByte(),
                2 => ReadUInt16(),
                4 => ReadUInt32(),
                8 => ReadUInt64(),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(ReadUnsignedVarVal)}"),
            };
        }

        public double ReadPreciseVarVal()
        {
            return VariableValueSize switch
            {
                2 => (double)ReadHalf(),
                4 => ReadSingle(),
                8 => ReadDouble(),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {nameof(ReadPreciseVarVal)}"),
            };
        }

        public string ReadUTF8()
            => Encoding.UTF8.GetString(Read8BitTerminatedStringBytes());

        public string ReadASCII()
            => Encoding.ASCII.GetString(Read8BitTerminatedStringBytes());

        public string ReadShiftJIS()
            => EncodingHelper.ShiftJIS.GetString(Read8BitTerminatedStringBytes());

        public string ReadEucJP()
            => EncodingHelper.EucJP.GetString(Read8BitTerminatedStringBytes());

        public string ReadEucCN()
            => EncodingHelper.EucCN.GetString(Read8BitTerminatedStringBytes());

        public string ReadEucKR()
            => EncodingHelper.EucKR.GetString(Read8BitTerminatedStringBytes());

        public string ReadUTF16()
            => BigEndian ? EncodingHelper.UTF16BE.GetString(Read16BitTerminatedStringBytes()) : EncodingHelper.UTF16LE.GetString(Read16BitTerminatedStringBytes());

        public string ReadUTF16LittleEndian()
            => EncodingHelper.UTF16LE.GetString(Read16BitTerminatedStringBytes());

        public string ReadUTF16BigEndian()
            => EncodingHelper.UTF16BE.GetString(Read16BitTerminatedStringBytes());

        public string ReadUTF32()
            => BigEndian ? EncodingHelper.UTF32BE.GetString(Read32BitTerminatedStringBytes()) : EncodingHelper.UTF32LE.GetString(Read32BitTerminatedStringBytes());

        public string ReadUTF32LittleEndian()
            => EncodingHelper.UTF32LE.GetString(Read32BitTerminatedStringBytes());

        public string ReadUTF32BigEndian()
            => EncodingHelper.UTF32BE.GetString(Read32BitTerminatedStringBytes());

        public string ReadUTF8(int length)
            => Encoding.UTF8.GetString(ReadBytes(length));

        public string ReadASCII(int length)
            => Encoding.ASCII.GetString(ReadBytes(length));

        public string ReadShiftJIS(int length)
            => EncodingHelper.ShiftJIS.GetString(ReadBytes(length));

        public string ReadEucJP(int length)
            => EncodingHelper.EucJP.GetString(ReadBytes(length));

        public string ReadEucCN(int length)
            => EncodingHelper.EucCN.GetString(ReadBytes(length));

        public string ReadEucKR(int length)
            => EncodingHelper.EucKR.GetString(ReadBytes(length));

        public string ReadUTF16(int length)
            => BigEndian ? EncodingHelper.UTF16BE.GetString(ReadBytes(length * 2)) : EncodingHelper.UTF16LE.GetString(ReadBytes(length * 2));

        public string ReadUTF16LittleEndian(int length)
            => EncodingHelper.UTF16LE.GetString(ReadBytes(length * 2));

        public string ReadUTF16BigEndian(int length)
            => EncodingHelper.UTF16BE.GetString(ReadBytes(length * 2));

        public string ReadUTF32(int length)
            => BigEndian ? EncodingHelper.UTF32BE.GetString(ReadBytes(length * 4)) : EncodingHelper.UTF32LE.GetString(ReadBytes(length * 4));

        public string ReadUTF32LittleEndian(int length)
            => EncodingHelper.UTF32LE.GetString(ReadBytes(length * 4));

        public string ReadUTF32BigEndian(int length)
            => EncodingHelper.UTF32BE.GetString(ReadBytes(length * 4));

        #endregion

        #region Get

        private T Get<T>(Func<T> read, long position)
        {
            int returnPosition = _position;
            Position = position;
            T value = read();
            _position = returnPosition;
            return value;
        }

        private T GetFixedString<T>(Func<int, T> readFixedString, long position, int length)
        {
            int returnPosition = _position;
            Position = position;
            T value = readFixedString(length);
            _position = returnPosition;
            return value;
        }

        public T Get<T>(long position) where T : unmanaged
        {
            int returnPosition = _position;
            Position = position;
            var value = Read<T>();
            _position = returnPosition;
            return value;
        }

        public sbyte GetSByte(long position)
            => Get<sbyte>(position);

        public byte GetByte(long position)
            => Get<byte>(position);

        public short GetInt16(long position)
            => Get(ReadInt16, position);

        public ushort GetUInt16(long position)
            => Get(ReadUInt16, position);

        public int GetInt32(long position)
            => Get(ReadInt32, position);

        public uint GetUInt32(long position)
            => Get(ReadUInt32, position);

        public long GetInt64(long position)
            => Get(ReadInt64, position);

        public ulong GetUInt64(long position)
            => Get(ReadUInt64, position);

        public Int128 GetInt128(long position)
            => Get(ReadInt128, position);

        public UInt128 GetUInt128(long position)
            => Get(ReadUInt128, position);

        public Half GetHalf(long position)
            => Get(ReadHalf, position);

        public float GetSingle(long position)
            => Get(ReadSingle, position);

        public double GetDouble(long position)
            => Get(ReadDouble, position);

        public bool GetBoolean(long position)
            => Get(ReadBoolean, position);

        public Vector2 GetVector2(long position)
            => Get(ReadVector2, position);

        public Vector3 GetVector3(long position)
            => Get(ReadVector3, position);

        public Vector4 GetVector4(long position)
            => Get(ReadVector4, position);

        public Quaternion GetQuaternion(long position)
            => Get(ReadQuaternion, position);

        public byte[] GetColor3(long position)
            => Get(ReadColor3, position);

        public Color GetColorRGB(long position)
            => Get(ReadColorRGB, position);

        public Color GetColorBGR(long position)
            => Get(ReadColorBGR, position);

        public byte[] GetColor4(long position)
            => Get(ReadColor4, position);

        public Color GetColorRGBA(long position)
            => Get(ReadColorRGBA, position);

        public Color GetColorBGRA(long position)
            => Get(ReadColorBGRA, position);

        public Color GetColorARGB(long position)
            => Get(ReadColorARGB, position);

        public Color GetColorABGR(long position)
            => Get(ReadColorABGR, position);

        public long GetSignedVarVal(long position)
            => Get(ReadSignedVarVal, position);

        public ulong GetUnsignedVarVal(long position)
            => Get(ReadUnsignedVarVal, position);

        public double GetPreciseVarVal(long position)
            => Get(ReadPreciseVarVal, position);

        public string GetUTF8(long position)
            => Get(ReadUTF8, position);

        public string GetASCII(long position)
            => Get(ReadASCII, position);

        public string GetShiftJIS(long position)
            => Get(ReadShiftJIS, position);

        public string GetEucJP(long position)
            => Get(ReadEucJP, position);

        public string GetEucCN(long position)
            => Get(ReadEucCN, position);

        public string GetEucKR(long position)
            => Get(ReadEucKR, position);

        public string GetUTF16(long position)
            => Get(ReadUTF16, position);

        public string GetUTF16LittleEndian(long position)
            => Get(ReadUTF16LittleEndian, position);

        public string GetUTF16BigEndian(long position)
            => Get(ReadUTF16BigEndian, position);

        public string GetUTF32(long position)
            => Get(ReadUTF32, position);

        public string GetUTF32LittleEndian(long position)
            => Get(ReadUTF32LittleEndian, position);

        public string GetUTF32BigEndian(long position)
            => Get(ReadUTF32BigEndian, position);

        public string GetUTF8(long position, int length)
            => GetFixedString(ReadUTF8, position, length);

        public string GetASCII(long position, int length)
            => GetFixedString(ReadASCII, position, length);

        public string GetShiftJIS(long position, int length)
            => GetFixedString(ReadShiftJIS, position, length);

        public string GetEucJP(long position, int length)
            => GetFixedString(ReadEucJP, position, length);

        public string GetEucCN(long position, int length)
            => GetFixedString(ReadEucCN, position, length);

        public string GetEucKR(long position, int length)
            => GetFixedString(ReadEucKR, position, length);

        public string GetUTF16(long position, int length)
            => GetFixedString(ReadUTF16, position, length);

        public string GetUTF16LittleEndian(long position, int length)
            => GetFixedString(ReadUTF16LittleEndian, position, length);

        public string GetUTF16BigEndian(long position, int length)
            => GetFixedString(ReadUTF16BigEndian, position, length);

        public string GetUTF32(long position, int length)
            => GetFixedString(ReadUTF32, position, length);

        public string GetUTF32LittleEndian(long position, int length)
            => GetFixedString(ReadUTF32LittleEndian, position, length);

        public string GetUTF32BigEndian(long position, int length)
            => GetFixedString(ReadUTF32BigEndian, position, length);

        #endregion

        #region Peek

        private T Peek<T>(Func<T> read)
        {
            int startPosition = _position;
            T value = read();
            _position = startPosition;
            return value;
        }

        private T PeekFixedString<T>(Func<int, T> readFixedString, int length)
        {
            int returnPosition = _position;
            T value = readFixedString(length);
            _position = returnPosition;
            return value;
        }

        public unsafe T Peek<T>() where T : unmanaged
        {
            int size = sizeof(T);
            if (_position + size > _length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified region of memory.");
            }

            return Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(_memory.Span), _position));
        }

        public sbyte PeekSByte()
            => Peek<sbyte>();

        public byte PeekByte()
            => Peek<byte>();

        public short PeekInt16()
            => Peek(ReadInt16);

        public ushort PeekUInt16()
            => Peek(ReadUInt16);

        public int PeekInt32()
            => Peek(ReadInt32);

        public uint PeekUInt32()
            => Peek(ReadUInt32);

        public long PeekInt64()
            => Peek(ReadInt64);

        public ulong PeekUInt64()
            => Peek(ReadUInt64);

        public Int128 PeekInt128()
            => Peek(ReadInt128);

        public UInt128 PeekUInt128()
            => Peek(ReadUInt128);

        public Half PeekHalf()
            => Peek(ReadHalf);

        public float PeekSingle()
            => Peek(ReadSingle);

        public double PeekDouble()
            => Peek(ReadDouble);

        public bool PeekBoolean()
            => Peek(ReadBoolean);

        public Vector2 PeekVector2()
            => Peek(ReadVector2);

        public Vector3 PeekVector3()
            => Peek(ReadVector3);

        public Vector4 PeekVector4()
            => Peek(ReadVector4);

        public Quaternion PeekQuaternion()
            => Peek(ReadQuaternion);

        public byte[] PeekColor3()
            => Peek(ReadColor3);

        public Color PeekColorRGB()
            => Peek(ReadColorRGB);

        public Color PeekColorBGR()
            => Peek(ReadColorBGR);

        public byte[] PeekColor4()
            => Peek(ReadColor4);

        public Color PeekColorRGBA()
            => Peek(ReadColorRGBA);

        public Color PeekColorBGRA()
            => Peek(ReadColorBGRA);

        public Color PeekColorARGB()
            => Peek(ReadColorARGB);

        public Color PeekColorABGR()
            => Peek(ReadColorABGR);

        public long PeekVarValSigned()
            => Peek(ReadSignedVarVal);

        public ulong PeekVarValUnsigned()
            => Peek(ReadUnsignedVarVal);

        public double PeekVarValPrecise()
            => Peek(ReadPreciseVarVal);

        public string PeekUTF8()
            => Peek(ReadUTF8);

        public string PeekASCII()
            => Peek(ReadASCII);

        public string PeekShiftJIS()
            => Peek(ReadShiftJIS);

        public string PeekEucJP()
            => Peek(ReadEucJP);

        public string PeekEucCN()
            => Peek(ReadEucCN);

        public string PeekEucKR()
            => Peek(ReadEucKR);

        public string PeekUTF16()
            => Peek(ReadUTF16);

        public string PeekUTF16LittleEndian()
            => Peek(ReadUTF16LittleEndian);

        public string PeekUTF16BigEndian()
            => Peek(ReadUTF16BigEndian);

        public string PeekUTF32()
            => Peek(ReadUTF32);

        public string PeekUTF32LittleEndian()
            => Peek(ReadUTF32LittleEndian);

        public string PeekUTF32BigEndian()
            => Peek(ReadUTF32BigEndian);

        public string PeekUTF8(int length)
            => PeekFixedString(ReadUTF8, length);

        public string PeekASCII(int length)
            => PeekFixedString(ReadASCII, length);

        public string PeekShiftJIS(int length)
            => PeekFixedString(ReadShiftJIS, length);

        public string PeekEucJP(int length)
            => PeekFixedString(ReadEucJP, length);

        public string PeekEucCN(int length)
            => PeekFixedString(ReadEucCN, length);

        public string PeekEucKR(int length)
            => PeekFixedString(ReadEucKR, length);

        public string PeekUTF16(int length)
            => PeekFixedString(ReadUTF16, length);

        public string PeekUTF16LittleEndian(int length)
            => PeekFixedString(ReadUTF16LittleEndian, length);

        public string PeekUTF16BigEndian(int length)
            => PeekFixedString(ReadUTF16BigEndian, length);

        public string PeekUTF32(int length)
            => PeekFixedString(ReadUTF32, length);

        public string PeekUTF32LittleEndian(int length)
            => PeekFixedString(ReadUTF32LittleEndian, length);

        public string PeekUTF32BigEndian(int length)
            => PeekFixedString(ReadUTF32BigEndian, length);

        #endregion

        #region Assert

        private T Assert<T>(T value, string typeName, string valueFormat, T option) where T : IEquatable<T>
        {
            if (value.Equals(option))
            {
                return value;
            }

            string strValue = string.Format(valueFormat, value);
            string strOption = string.Format(valueFormat, option);
            throw new InvalidDataException($"Read {typeName}: {strValue} | Expected: {strOption} | Ending position: 0x{_position:X}");
        }

        private T Assert<T>(T value, string typeName, string valueFormat, ReadOnlySpan<T> options) where T : IEquatable<T>
        {
            foreach (T option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strValue = string.Format(valueFormat, value);
            string strOptions = string.Join(", ", options.ToArray().Select(o => string.Format(valueFormat, o)));
            throw new InvalidDataException($"Read {typeName}: {strValue} | Expected: {strOptions} | Ending position: 0x{_position:X}");
        }

        private string AssertString(string value, string encodingName, string option)
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Read {encodingName}: {value} | Expected: {option} | Ending position: 0x{_position:X}");
        }

        private string AssertString(string value, string encodingName, ReadOnlySpan<string> options)
        {
            foreach (string option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string joinedOptions = string.Join(", ", options.ToArray());
            throw new InvalidDataException($"Read {encodingName}: {value} | Expected: {joinedOptions} | Ending position: 0x{_position:X}");
        }

        public void AssertBytePattern(int length, byte pattern)
        {
            byte[] bytes = ReadBytes(length);
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] != pattern)
                {
                    throw new InvalidDataException($"Read {bytes[i]:X2} at position {i} | Expected {length} 0x{pattern:X2}");
                }
            }
        }

        public sbyte AssertSByte(sbyte option)
            => Assert(ReadSByte(), nameof(SByte), "0x{0:X}", option);

        public sbyte AssertSByte(ReadOnlySpan<sbyte> options)
            => Assert(ReadSByte(), nameof(SByte), "0x{0:X}", options);

        public byte AssertByte(byte option)
            => Assert(ReadByte(), nameof(Byte), "0x{0:X}", option);

        public byte AssertByte(ReadOnlySpan<byte> options)
            => Assert(ReadByte(), nameof(Byte), "0x{0:X}", options);

        public short AssertInt16(short option)
            => Assert(ReadInt16(), nameof(Int16), "0x{0:X}", option);

        public short AssertInt16(ReadOnlySpan<short> options)
            => Assert(ReadInt16(), nameof(Int16), "0x{0:X}", options);

        public ushort AssertUInt16(ushort option)
            => Assert(ReadUInt16(), nameof(UInt16), "0x{0:X}", option);

        public ushort AssertUInt16(ReadOnlySpan<ushort> options)
            => Assert(ReadUInt16(), nameof(UInt16), "0x{0:X}", options);

        public int AssertInt32(int option)
            => Assert(ReadInt32(), nameof(Int32), "0x{0:X}", option);

        public int AssertInt32(ReadOnlySpan<int> options)
            => Assert(ReadInt32(), nameof(Int32), "0x{0:X}", options);

        public long AssertInt64(long option)
            => Assert(ReadInt64(), nameof(Int64), "0x{0:X}", option);

        public long AssertInt64(ReadOnlySpan<long> options)
            => Assert(ReadInt64(), nameof(Int64), "0x{0:X}", options);

        public uint AssertUInt32(uint option)
            => Assert(ReadUInt32(), nameof(UInt32), "0x{0:X}", option);

        public uint AssertUInt32(ReadOnlySpan<uint> options)
            => Assert(ReadUInt32(), nameof(UInt32), "0x{0:X}", options);

        public ulong AssertUInt64(ulong option)
            => Assert(ReadUInt64(), nameof(UInt64), "0x{0:X}", option);

        public ulong AssertUInt64(ReadOnlySpan<ulong> options)
            => Assert(ReadUInt64(), nameof(UInt64), "0x{0:X}", options);

        public Int128 AssertInt128(Int128 option)
            => Assert(ReadInt128(), nameof(Int128), "0x{0:X}", option);

        public Int128 AssertInt128(ReadOnlySpan<Int128> options)
            => Assert(ReadInt128(), nameof(Int128), "0x{0:X}", options);

        public UInt128 AssertUInt128(UInt128 option)
            => Assert(ReadUInt128(), nameof(UInt128), "0x{0:X}", option);

        public UInt128 AssertUInt128(ReadOnlySpan<UInt128> options)
            => Assert(ReadUInt128(), nameof(UInt128), "0x{0:X}", options);

        public Half AssertHalf(Half option)
            => Assert(ReadHalf(), nameof(Half), "{0}", option);

        public Half AssertHalf(ReadOnlySpan<Half> options)
            => Assert(ReadHalf(), nameof(Half), "{0}", options);

        public float AssertSingle(float option)
            => Assert(ReadSingle(), nameof(Single), "{0}", option);

        public float AssertSingle(ReadOnlySpan<float> options)
            => Assert(ReadSingle(), nameof(Single), "{0}", options);

        public double AssertDouble(double option)
            => Assert(ReadDouble(), nameof(Double), "{0}", option);

        public double AssertDouble(ReadOnlySpan<double> options)
            => Assert(ReadDouble(), nameof(Double), "{0}", options);

        public bool AssertBoolean(bool option)
            => Assert(ReadBoolean(), nameof(Boolean), "{0}", option);

        public string AssertUTF8(string option)
            => AssertString(ReadUTF8(option.Length), "UTF8", option);

        public string AssertUTF8(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF8(length), "UTF8", options);

        public string AssertASCII(string option)
            => AssertString(ReadASCII(option.Length), "ASCII", option);

        public string AssertASCII(int length, ReadOnlySpan<string> options)
            => AssertString(ReadASCII(length), "ASCII", options);

        public string AssertShiftJIS(string option)
            => AssertString(ReadShiftJIS(option.Length), "ShiftJIS", option);

        public string AssertShiftJIS(int length, ReadOnlySpan<string> options)
            => AssertString(ReadShiftJIS(length), "ShiftJIS", options);

        public string AssertEucJP(string option)
            => AssertString(ReadEucJP(option.Length), "EucJP", option);

        public string AssertEucJP(int length, ReadOnlySpan<string> options)
            => AssertString(ReadEucJP(length), "EucJP", options);

        public string AssertEucCN(string option)
            => AssertString(ReadEucCN(option.Length), "EucCN", option);

        public string AssertEucCN(int length, ReadOnlySpan<string> options)
            => AssertString(ReadEucCN(length), "EucCN", options);

        public string AssertEucKR(string option)
            => AssertString(ReadEucKR(option.Length), "EucKR", option);

        public string AssertEucKR(int length, ReadOnlySpan<string> options)
            => AssertString(ReadEucKR(length), "EucKR", options);

        public string AssertUTF16(string option)
            => AssertString(ReadUTF16(option.Length), "UTF16", option);

        public string AssertUTF16(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF16(length), "UTF16", options);

        public string AssertUTF16LittleEndian(string option)
            => AssertString(ReadUTF16LittleEndian(option.Length), "UTF16LittleEndian", option);

        public string AssertUTF16LittleEndian(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF16LittleEndian(length), "UTF16LittleEndian", options);

        public string AssertUTF16BigEndian(string option)
            => AssertString(ReadUTF16BigEndian(option.Length), "UTF16BigEndian", option);

        public string AssertUTF16BigEndian(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF16BigEndian(length), "UTF16BigEndian", options);

        public string AssertUTF32(string option)
            => AssertString(ReadUTF32(option.Length), "UTF32", option);

        public string AssertUTF32(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF32(length), "UTF32", options);

        public string AssertUTF32LittleEndian(string option)
            => AssertString(ReadUTF32LittleEndian(option.Length), "UTF32LittleEndian", option);

        public string AssertUTF32LittleEndian(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF32LittleEndian(length), "UTF32LittleEndian", options);

        public string AssertUTF32BigEndian(string option)
            => AssertString(ReadUTF32BigEndian(option.Length), "UTF32BigEndian", option);

        public string AssertUTF32BigEndian(int length, ReadOnlySpan<string> options)
            => AssertString(ReadUTF32BigEndian(length), "UTF32BigEndian", options);

        public long AssertVarValSigned(long option)
            => Assert(ReadSignedVarVal(), $"VarValS{VariableValueSize}", "0x{0:X}", option);

        public long AssertVarValSigned(ReadOnlySpan<long> options)
            => Assert(ReadSignedVarVal(), $"VarValS{VariableValueSize}", "0x{0:X}", options);

        public ulong AssertVarValUnsigned(ulong option)
            => Assert(ReadUnsignedVarVal(), $"VarValU{VariableValueSize}", "0x{0:X}", option);

        public ulong AssertVarValUnsigned(ReadOnlySpan<ulong> options)
            => Assert(ReadUnsignedVarVal(), $"VarValU{VariableValueSize}", "0x{0:X}", options);

        public double AssertVarValPrecise(double option)
            => Assert(ReadUnsignedVarVal(), $"VarValF{VariableValueSize}", "0x{0:X}", option);

        public double AssertVarValPrecise(ReadOnlySpan<double> options)
            => Assert(ReadUnsignedVarVal(), $"VarValF{VariableValueSize}", "0x{0:X}", options);

        #endregion

        #region Read Array

        private static T[] ReadArray<T>(Func<T> read, int count) where T : unmanaged
        {
            var values = new T[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = read();
            }
            return values;
        }

        private T[] ReadArrayEndian<T>(Func<T, T> reverseEndianness, int count) where T : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                var values = new T[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = reverseEndianness(Read<T>());
                }
                return values;
            }

            return ReadArray<T>(count);
        }

        private TTo[] ReadArrayCast<TFrom, TTo>(Func<TFrom, TTo> cast, int count)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            var values = new TTo[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = cast(Read<TFrom>());
            }
            return values;
        }

        private TTo[] ReadArrayEndianConvert<TFrom, TTo>(Func<TFrom, TFrom> reverseEndianness, Func<TFrom, TTo> convert, int count)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                var values = new TTo[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = convert(reverseEndianness(Read<TFrom>()));
                }
                return values;
            }

            return ReadArray<TTo>(count);
        }

        private TTo[] ReadArrayEndianCast<TFrom, TTo>(Func<TFrom, TFrom> reverseEndianness, Func<TFrom, TTo> cast, int count)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            var values = new TTo[count];
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < count; i++)
                {
                    values[i] = cast(reverseEndianness(Read<TFrom>()));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    values[i] = cast(Read<TFrom>());
                }
            }

            return values;
        }

        private TTo[] ReadArrayEndianConvertCast<TRead, TFrom, TTo>(Func<TRead, TRead> reverseEndianness, Func<TRead, TFrom> convert, Func<TFrom, TTo> cast, int count)
            where TRead : unmanaged
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            var values = new TTo[count];
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < count; i++)
                {
                    values[i] = cast(convert(reverseEndianness(Read<TRead>())));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    values[i] = cast(convert(Read<TRead>()));
                }
            }

            return values;
        }

        public unsafe T[] ReadArray<T>(int count) where T : unmanaged
            => ReadSpan<T>(count).ToArray();

        public sbyte[] ReadSBytes(int count)
            => ReadArray<sbyte>(count);

        public byte[] ReadBytes(int count)
            => ReadArray<byte>(count);

        public short[] ReadInt16s(int count)
            => ReadArrayEndian<short>(BinaryPrimitives.ReverseEndianness, count);

        public ushort[] ReadUInt16s(int count)
            => ReadArrayEndian<ushort>(BinaryPrimitives.ReverseEndianness, count);

        public int[] ReadInt32s(int count)
            => ReadArrayEndian<int>(BinaryPrimitives.ReverseEndianness, count);

        public uint[] ReadUInt32s(int count)
            => ReadArrayEndian<uint>(BinaryPrimitives.ReverseEndianness, count);

        public long[] ReadInt64s(int count)
            => ReadArrayEndian<long>(BinaryPrimitives.ReverseEndianness, count);

        public ulong[] ReadUInt64s(int count)
            => ReadArrayEndian<ulong>(BinaryPrimitives.ReverseEndianness, count);

        public Int128[] ReadInt128s(int count)
            => ReadArrayEndian<Int128>(BinaryPrimitives.ReverseEndianness, count);

        public UInt128[] ReadUInt128s(int count)
            => ReadArrayEndian<UInt128>(BinaryPrimitives.ReverseEndianness, count);

        public Half[] ReadHalfs(int count)
            => ReadArrayEndianConvert<ushort, Half>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt16BitsToHalf, count);

        public float[] ReadSingles(int count)
            => ReadArrayEndianConvert<uint, float>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt32BitsToSingle, count);

        public double[] ReadDoubles(int count)
            => ReadArrayEndianConvert<ulong, double>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt64BitsToDouble, count);

        public bool[] ReadBooleans(int count)
            => ReadArray(ReadBoolean, count);

        public Vector2[] ReadVector2s(int count)
        {
            // Prevent checking endianness for every component that needs to be read
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                // Read all components in reversed byte order
                int componentCount = count * 2;
                var values = new float[componentCount];
                for (int i = 0; i < componentCount; i++)
                {
                    values[i] = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                }

                // Reinterpret components as values
                return MemoryMarshal.Cast<float, Vector2>(new ReadOnlySpan<float>(values)).ToArray();
            }

            return ReadArray<Vector2>(count);
        }

        public Vector3[] ReadVector3s(int count)
        {
            // Prevent checking endianness for every component that needs to be read
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                // Read all components in reversed byte order
                int componentCount = count * 3;
                var values = new float[componentCount];
                for (int i = 0; i < componentCount; i++)
                {
                    values[i] = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                }

                // Reinterpret components as values
                return MemoryMarshal.Cast<float, Vector3>(new ReadOnlySpan<float>(values)).ToArray();
            }

            return ReadArray<Vector3>(count);
        }

        public Vector4[] ReadVector4s(int count)
        {
            // Prevent checking endianness for every component that needs to be read
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                // Read all components in reversed byte order
                int componentCount = count * 4;
                var values = new float[componentCount];
                for (int i = 0; i < componentCount; i++)
                {
                    values[i] = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                }

                // Reinterpret components as values
                return MemoryMarshal.Cast<float, Vector4>(new ReadOnlySpan<float>(values)).ToArray();
            }

            return ReadArray<Vector4>(count);
        }

        public Quaternion[] ReadQuaternions(int count)
        {
            // Prevent checking endianness for every component that needs to be read
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                // Read all components in reversed byte order
                int componentCount = count * 4;
                var values = new float[componentCount];
                for (int i = 0; i < componentCount; i++)
                {
                    values[i] = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()));
                }

                // Reinterpret components as values
                return MemoryMarshal.Cast<float, Quaternion>(new ReadOnlySpan<float>(values)).ToArray();
            }

            return ReadArray<Quaternion>(count);
        }

        public long[] ReadSignedVarVals(int count)
        {
            return VariableValueSize switch
            {
                1 => ReadArrayCast<sbyte, long>(Convert.ToInt64, count),
                2 => ReadArrayEndianCast<short, long>(BinaryPrimitives.ReverseEndianness, Convert.ToInt64, count),
                4 => ReadArrayEndianCast<int, long>(BinaryPrimitives.ReverseEndianness, Convert.ToInt64, count),
                8 => ReadInt64s(count),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {ReadSignedVarVals}"),
            };
        }

        public ulong[] ReadUnsignedVarVals(int count)
        {
            return VariableValueSize switch
            {
                1 => ReadArrayCast<byte, ulong>(Convert.ToUInt64, count),
                2 => ReadArrayEndianCast<ushort, ulong>(BinaryPrimitives.ReverseEndianness, Convert.ToUInt64, count),
                4 => ReadArrayEndianCast<uint, ulong>(BinaryPrimitives.ReverseEndianness, Convert.ToUInt64, count),
                8 => ReadUInt64s(count),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {ReadUnsignedVarVals}"),
            };
        }

        public double[] ReadPreciseVarVals(int count)
        {
            return VariableValueSize switch
            {
                2 => ReadArrayEndianConvertCast<ushort, Half, double>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt16BitsToHalf, (value) => (double)value, count),
                4 => ReadArrayEndianConvertCast<uint, float, double>(BinaryPrimitives.ReverseEndianness, BitConverter.UInt32BitsToSingle, Convert.ToDouble, count),
                8 => ReadDoubles(count),
                _ => throw new NotSupportedException($"{nameof(VariableValueSize)} {VariableValueSize} is not supported for {ReadPreciseVarVals}"),
            };
        }

        #endregion

        #region Get Array

        private T[] GetArray<T>(Func<int, T[]> readArray, long position, int count)
        {
            int returnPosition = _position;
            Position = position;
            T[] values = readArray(count);
            _position = returnPosition;
            return values;
        }

        public T[] GetArray<T>(long position, int count) where T : unmanaged
        {
            int returnPosition = _position;
            Position = position;
            T[] values = ReadArray<T>(count);
            _position = returnPosition;
            return values;
        }

        public sbyte[] GetSBytes(long position, int count)
            => GetArray<sbyte>(position, count);

        public byte[] GetBytes(long position, int count)
            => GetArray<byte>(position, count);

        public short[] GetInt16s(long position, int count)
            => GetArray(ReadInt16s, position, count);

        public ushort[] GetUInt16s(long position, int count)
            => GetArray(ReadUInt16s, position, count);

        public int[] GetInt32s(long position, int count)
            => GetArray(ReadInt32s, position, count);

        public uint[] GetUInt32s(long position, int count)
            => GetArray(ReadUInt32s, position, count);

        public long[] GetInt64s(long position, int count)
            => GetArray(ReadInt64s, position, count);

        public ulong[] GetUInt64s(long position, int count)
            => GetArray(ReadUInt64s, position, count);

        public Int128[] GetInt128s(long position, int count)
            => GetArray(ReadInt128s, position, count);

        public UInt128[] GetUInt128s(long position, int count)
            => GetArray(ReadUInt128s, position, count);

        public Half[] GetHalfs(long position, int count)
            => GetArray(ReadHalfs, position, count);

        public float[] GetSingles(long position, int count)
            => GetArray(ReadSingles, position, count);

        public double[] GetDoubles(long position, int count)
            => GetArray(ReadDoubles, position, count);

        public bool[] GetBooleans(long position, int count)
            => GetArray(ReadBooleans, position, count);

        public Vector2[] GetVector2s(long position, int count)
            => GetArray(ReadVector2s, position, count);

        public Vector3[] GetVector3s(long position, int count)
            => GetArray(ReadVector3s, position, count);

        public Vector4[] GetVector4s(long position, int count)
            => GetArray(ReadVector4s, position, count);

        public Quaternion[] GetQuaternions(long position, int count)
            => GetArray(ReadQuaternions, position, count);

        public long[] GetSignedVarVals(long position, int count)
            => GetArray(ReadSignedVarVals, position, count);

        public ulong[] GetUnsignedVarVals(long position, int count)
            => GetArray(ReadUnsignedVarVals, position, count);

        public double[] GetPreciseVarVals(long position, int count)
            => GetArray(ReadPreciseVarVals, position, count);

        #endregion

        #region Peek Array

        private T[] PeekArray<T>(Func<int, T[]> readArray, int count)
        {
            int startPosition = _position;
            T[] values = readArray(count);
            _position = startPosition;
            return values;
        }

        public T[] PeekArray<T>(int count) where T : unmanaged
            => PeekSpan<T>(count).ToArray();

        public sbyte[] PeekSBytes(int count)
            => PeekArray<sbyte>(count);

        public byte[] PeekBytes(int count)
            => PeekArray<byte>(count);

        public short[] PeekInt16s(int count)
            => PeekArray(ReadInt16s, count);

        public ushort[] PeekUInt16s(int count)
            => PeekArray(ReadUInt16s, count);

        public int[] PeekInt32s(int count)
            => PeekArray(ReadInt32s, count);

        public uint[] PeekUInt32s(int count)
            => PeekArray(ReadUInt32s, count);

        public long[] PeekInt64s(int count)
            => PeekArray(ReadInt64s, count);

        public ulong[] PeekUInt64s(int count)
            => PeekArray(ReadUInt64s, count);

        public Int128[] PeekInt128s(int count)
            => PeekArray(ReadInt128s, count);

        public UInt128[] PeekUInt128s(int count)
            => PeekArray(ReadUInt128s, count);

        public Half[] PeekHalfs(int count)
            => PeekArray(ReadHalfs, count);

        public float[] PeekSingles(int count)
            => PeekArray(ReadSingles, count);

        public double[] PeekDoubles(int count)
            => PeekArray(ReadDoubles, count);

        public bool[] PeekBooleans(int count)
            => PeekArray(ReadBooleans, count);

        public Vector2[] PeekVector2s(int count)
            => PeekArray(ReadVector2s, count);

        public Vector3[] PeekVector3s(int count)
            => PeekArray(ReadVector3s, count);

        public Vector4[] PeekVector4s(int count)
            => PeekArray(ReadVector4s, count);

        public Quaternion[] PeekQuaternions(int count)
            => PeekArray(ReadQuaternions, count);

        public long[] PeekVarValsSigned(int count)
            => PeekArray(ReadSignedVarVals, count);

        public ulong[] PeekVarValsUnsigned(int count)
            => PeekArray(ReadUnsignedVarVals, count);

        public double[] PeekVarValsPrecise(int count)
            => PeekArray(ReadPreciseVarVals, count);

        #endregion

        #region Read Span

        public unsafe Span<T> ReadSpan<T>(int count) where T : unmanaged
        {
            int size = sizeof(T) * count;
            int endPosition = _position + size;
            if (endPosition > _length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified region of memory.");
            }
            var value = MemoryMarshal.Cast<byte, T>(_memory.Span.Slice(_position, size));
            _position = endPosition;
            return value;
        }

        #endregion

        #region Get Span

        public Span<T> GetSpan<T>(long position, int count) where T : unmanaged
        {
            int returnPosition = _position;
            Position = position;
            var values = ReadSpan<T>(count);
            _position = returnPosition;
            return values;
        }

        #endregion

        #region Peek Span

        public unsafe Span<T> PeekSpan<T>(int count) where T : unmanaged
        {
            int size = sizeof(T) * count;
            if (_position + size > _length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified region of memory.");
            }
            return MemoryMarshal.Cast<byte, T>(_memory.Span.Slice(_position, size));
        }

        #endregion

        #region Read Memory

        public Memory<byte> ReadByteMemory(int size)
        {
            var value = _memory.Slice(_position, size);
            _position += size;
            return value;
        }

        #endregion

    }
}
