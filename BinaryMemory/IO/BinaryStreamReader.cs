using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A reader for data present in a stream.<br/>
    /// Expands upon <see cref="BinaryReader"/>.
    /// </summary>
    public class BinaryStreamReader : IBinaryReader, IDisposable
    {
        /// <summary>
        /// The underlying <see cref="BinaryReader"/>.
        /// </summary>
        private readonly BinaryReader _br;

        /// <summary>
        /// Steps into the stream.
        /// </summary>
        private readonly Stack<long> _steps;

        /// <summary>
        /// The underlying <see cref="Stream"/>.
        /// </summary>
        public Stream BaseStream => _br.BaseStream;

        /// <summary>
        /// The current position of the reader.
        /// </summary>
        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        /// <summary>
        /// Whether or not to read in big endian byte ordering.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// Whether or not the <see cref="BinaryStreamReader"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The length of the underlying stream.
        /// </summary>
        public long Length => BaseStream.Length;

        /// <summary>
        /// The remaining length starting from the current position.
        /// </summary>
        public long Remaining => Length - Position;

        /// <summary>
        /// The amount of positions the reader is stepped into.
        /// </summary>
        public int StepInCount => _steps.Count;

        /// <summary>
        /// Create a new <see cref="BinaryStreamReader"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="bigEndian">Whether or not to read in big endian byte ordering.</param>
        /// <param name="leaveOpen">Whether or not to leave the underlying <see cref="Stream"/> open when disposing.</param>
        public BinaryStreamReader(Stream stream, bool bigEndian = false, bool leaveOpen = false)
        {
            _br = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
            _steps = new Stack<long>();
            BigEndian = bigEndian;
        }

        /// <summary>
        /// Create a new <see cref="BinaryStreamReader"/> from an array of bytes.
        /// </summary>
        /// <param name="bytes">An array of bytes to read from.</param>
        /// <param name="bigEndian">Whether or not to read in big endian byte ordering.</param>
        public BinaryStreamReader(byte[] bytes, bool bigEndian = false) : this(new MemoryStream(bytes, false), bigEndian, false) { }

        /// <summary>
        /// Create a new <see cref="BinaryStreamReader"/> from a file.
        /// </summary>
        /// <param name="path">The path to the file to read from.</param>
        /// <param name="bigEndian">Whether or not to read in big endian byte ordering.</param>
        public BinaryStreamReader(string path, bool bigEndian = false) : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), bigEndian, false) { }

        #region Position

        public void Advance()
            => Position++;

        public void Advance(int count)
            => Position += count;

        public void Rewind()
            => Position--;

        public void Rewind(int count)
            => Position -= count;

        public void GotoStart()
            => BaseStream.Seek(0, SeekOrigin.Begin);

        public void GotoEnd()
            => BaseStream.Seek(0, SeekOrigin.End);

        #endregion

        #region Align

        public void Align(int alignment)
        {
            long remainder = Position % alignment;
            if (remainder > 0)
            {
                Position += alignment - remainder;
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
            _steps.Push(position);
            Position = position;
        }

        public void StepOut()
        {
            if (_steps.Count < 1)
            {
                throw new InvalidOperationException("Reader is already stepped all the way out.");
            }

            Position = _steps.Pop();
        }

        #endregion

        #region Read

        private TValue SetValueEndianness<TValue>(TValue value, Func<TValue, TValue> reverseEndianness)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                value = reverseEndianness(value);
            }

            return value;
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

        private string ReadFixedTerminatedString(Encoding encoding, int length)
        {
            byte[] bytes = ReadBytes(length);
            int terminatorIndex;
            for (terminatorIndex = 0; terminatorIndex < length; terminatorIndex++)
                if (bytes[terminatorIndex] == 0)
                    break;

            return encoding.GetString(bytes, 0, terminatorIndex);
        }

        private string ReadFixedTerminatedStringW(Encoding encoding, int length, int bytesPerChar)
        {
            byte[] bytes = ReadBytes(length * bytesPerChar);
            int terminatorIndex;
            for (terminatorIndex = 0; terminatorIndex < length; terminatorIndex += bytesPerChar)
            {
                bool terminate = true;

                // Cancel termination if a byte is not null
                for (int i = 0; i < bytesPerChar; i++)
                {
                    if (bytes[terminatorIndex + i] != 0)
                    {
                        terminate = false;
                        break;
                    }
                }

                if (terminate)
                    break;
            }

            return encoding.GetString(bytes, 0, terminatorIndex);
        }

        public sbyte ReadSByte()
            => _br.ReadSByte();

        public byte ReadByte()
            => _br.ReadByte();

        public short ReadInt16()
            => SetValueEndianness(_br.ReadInt16(), BinaryPrimitives.ReverseEndianness);

        public ushort ReadUInt16()
            => SetValueEndianness(_br.ReadUInt16(), BinaryPrimitives.ReverseEndianness);

        public int ReadInt32()
            => SetValueEndianness(_br.ReadInt32(), BinaryPrimitives.ReverseEndianness);

        public uint ReadUInt32()
            => SetValueEndianness(_br.ReadUInt32(), BinaryPrimitives.ReverseEndianness);

        public long ReadInt64()
            => SetValueEndianness(_br.ReadInt64(), BinaryPrimitives.ReverseEndianness);

        public ulong ReadUInt64()
            => SetValueEndianness(_br.ReadUInt64(), BinaryPrimitives.ReverseEndianness);

        public Half ReadHalf()
            => BitConverter.UInt16BitsToHalf(SetValueEndianness(_br.ReadUInt16(), BinaryPrimitives.ReverseEndianness));

        public float ReadSingle()
            => BitConverter.UInt32BitsToSingle(SetValueEndianness(_br.ReadUInt32(), BinaryPrimitives.ReverseEndianness));

        public double ReadDouble()
            => BitConverter.UInt64BitsToDouble(SetValueEndianness(_br.ReadUInt64(), BinaryPrimitives.ReverseEndianness));

        public bool ReadBoolean()
        {
            byte value = _br.ReadByte();
            if (value == 0)
                return false;
            else if (value == 1)
                return true;

            throw new InvalidDataException($"{nameof(ReadBoolean)} encountered non-boolean value: 0x{value:X2}");
        }

        public char ReadChar()
            => _br.ReadChar();

        public byte[] ReadRawColor3()
            => ReadBytes(3);

        public Color ReadColorRGB()
            => Color.FromArgb(_br.ReadByte(), _br.ReadByte(), _br.ReadByte());

        public Color ReadColorBGR()
        {
            byte[] bytes = ReadBytes(3);
            return Color.FromArgb(255, bytes[2], bytes[1], bytes[0]);
        }

        public byte[] ReadRawColor4()
            => ReadBytes(4);

        public Color ReadColorRGBA()
        {
            byte[] bytes = ReadBytes(4);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        public Color ReadColorBGRA()
        {
            byte[] bytes = ReadBytes(4);
            return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public Color ReadColorARGB()
            => Color.FromArgb(_br.ReadByte(), _br.ReadByte(), _br.ReadByte(), _br.ReadByte());

        public Color ReadColorABGR()
        {
            byte[] bytes = ReadBytes(4);
            return Color.FromArgb(bytes[0], bytes[3], bytes[2], bytes[1]);
        }

        public Vector2 ReadVector2()
            => new Vector2(ReadSingle(), ReadSingle());

        public Vector3 ReadVector3()
            => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

        public Vector4 ReadVector4()
            => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public Quaternion ReadQuaternion()
            => new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

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
            => ReadFixedTerminatedString(Encoding.UTF8, length);

        public string ReadASCII(int length)
            => ReadFixedTerminatedString(Encoding.ASCII, length);

        public string ReadShiftJIS(int length)
            => ReadFixedTerminatedString(EncodingHelper.ShiftJIS, length);

        public string ReadEucJP(int length)
            => ReadFixedTerminatedString(EncodingHelper.EucJP, length);

        public string ReadEucCN(int length)
            => ReadFixedTerminatedString(EncodingHelper.EucCN, length);

        public string ReadEucKR(int length)
            => ReadFixedTerminatedString(EncodingHelper.EucKR, length);

        public string ReadUTF16(int length)
            => BigEndian ? ReadFixedTerminatedStringW(EncodingHelper.UTF16BE, length, 2) : ReadFixedTerminatedStringW(EncodingHelper.UTF16LE, length, 2);

        public string ReadUTF16LittleEndian(int length)
            => ReadFixedTerminatedStringW(EncodingHelper.UTF16LE, length, 2);

        public string ReadUTF16BigEndian(int length)
            => ReadFixedTerminatedStringW(EncodingHelper.UTF16BE, length, 2);

        public string ReadUTF32(int length)
            => BigEndian ? ReadFixedTerminatedStringW(EncodingHelper.UTF32BE, length, 4) : ReadFixedTerminatedStringW(EncodingHelper.UTF32LE, length, 4);

        public string ReadUTF32LittleEndian(int length)
            => ReadFixedTerminatedStringW(EncodingHelper.UTF32LE, length, 4);

        public string ReadUTF32BigEndian(int length)
            => ReadFixedTerminatedStringW(EncodingHelper.UTF32BE, length, 4);

        #endregion

        #region Get

        private T Get<T>(Func<T> read, long position)
        {
            long oldpos = Position;
            Position = position;
            T value = read();
            Position = oldpos;
            return value;
        }

        private T GetFixedString<T>(Func<int, T> readFixedString, long position, int length)
        {
            long oldpos = Position;
            Position = position;
            T value = readFixedString(length);
            Position = oldpos;
            return value;
        }

        public sbyte GetSByte(long position)
            => Get(ReadSByte, position);

        public byte GetByte(long position)
            => Get(ReadByte, position);

        public short GetInt16(long position)
            => Get(ReadInt16, position);

        public ushort GetUInt16(long position)
            => Get(ReadUInt16, position);

        public int GetInt32(long position)
            => Get(ReadInt16, position);

        public uint GetUInt32(long position)
            => Get(ReadUInt32, position);

        public long GetInt64(long position)
            => Get(ReadInt64, position);

        public ulong GetUInt64(long position)
            => Get(ReadUInt64, position);

        public Half GetHalf(long position)
            => Get(ReadHalf, position);

        public float GetSingle(long position)
            => Get(ReadSingle, position);

        public double GetDouble(long position)
            => Get(ReadDouble, position);

        public bool GetBoolean(long position)
            => Get(ReadBoolean, position);

        public byte[] GetRawColor3(long position)
            => Get(ReadRawColor3, position);

        public Color GetColorRGB(long position)
            => Get(ReadColorRGB, position);

        public Color GetColorBGR(long position)
            => Get(ReadColorBGR, position);

        public byte[] GetRawColor4(long position)
            => Get(ReadRawColor4, position);

        public Color GetColorRGBA(long position)
            => Get(ReadColorRGBA, position);

        public Color GetColorBGRA(long position)
            => Get(ReadColorBGRA, position);

        public Color GetColorARGB(long position)
            => Get(ReadColorARGB, position);

        public Color GetColorABGR(long position)
            => Get(ReadColorABGR, position);

        public Vector2 GetVector2(long position)
            => Get(ReadVector2, position);

        public Vector3 GetVector3(long position)
            => Get(ReadVector3, position);

        public Vector4 GetVector4(long position)
            => Get(ReadVector4, position);

        public Quaternion GetQuaternion(long position)
            => Get(ReadQuaternion, position);

        public string GetUTF8(int position)
            => Get(ReadUTF8, position);

        public string GetASCII(int position)
            => Get(ReadASCII, position);

        public string GetShiftJIS(int position)
            => Get(ReadShiftJIS, position);

        public string GetEucJP(int position)
            => Get(ReadEucJP, position);

        public string GetEucCN(int position)
            => Get(ReadEucCN, position);

        public string GetEucKR(int position)
            => Get(ReadEucKR, position);

        public string GetUTF16(int position)
            => Get(ReadUTF16, position);

        public string GetUTF16LittleEndian(int position)
            => Get(ReadUTF16LittleEndian, position);

        public string GetUTF16BigEndian(int position)
            => Get(ReadUTF16BigEndian, position);

        public string GetUTF32(int position)
            => Get(ReadUTF32, position);

        public string GetUTF32LittleEndian(int position)
            => Get(ReadUTF32LittleEndian, position);

        public string GetUTF32BigEndian(int position)
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
            long startPosition = Position;
            T value = read();
            Position = startPosition;
            return value;
        }

        private T PeekFixedString<T>(Func<int, T> readFixedString, int length)
        {
            long returnPosition = Position;
            T value = readFixedString(length);
            Position = returnPosition;
            return value;
        }

        public sbyte PeekSByte()
            => Peek(ReadSByte);

        public byte PeekByte()
            => Peek(ReadByte);

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

        public byte[] PeekRawColor3()
            => Peek(ReadRawColor3);

        public Color PeekColorRGB()
            => Peek(ReadColorRGB);

        public Color PeekColorBGR()
            => Peek(ReadColorBGR);

        public byte[] PeekRawColor4()
            => Peek(ReadRawColor4);

        public Color PeekColorRGBA()
            => Peek(ReadColorRGBA);

        public Color PeekColorBGRA()
            => Peek(ReadColorBGRA);

        public Color PeekColorARGB()
            => Peek(ReadColorARGB);

        public Color PeekColorABGR()
            => Peek(ReadColorABGR);

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

        private TValue Assert<TValue>(TValue value, string typeName, string valueFormat, TValue option) where TValue : IEquatable<TValue>
        {
            if (value.Equals(option))
            {
                return value;
            }

            string strValue = string.Format(valueFormat, value);
            string strOption = string.Format(valueFormat, option);
            throw new InvalidDataException($"Read {typeName}: {strValue} | Expected: {strOption} | Ending position: 0x{Position:X}");
        }

        private TValue Assert<TValue>(TValue value, string typeName, string valueFormat, ReadOnlySpan<TValue> options) where TValue : IEquatable<TValue>
        {
            foreach (TValue option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strValue = string.Format(valueFormat, value);
            string strOptions = string.Join(", ", options.ToArray().Select(o => string.Format(valueFormat, o)));
            throw new InvalidDataException($"Read {typeName}: {strValue} | Expected: {strOptions} | Ending position: 0x{Position:X}");
        }

        private string AssertString(string value, string encodingName, string option)
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Read {encodingName}: {value} | Expected: {option} | Ending position: 0x{Position:X}");
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
            throw new InvalidDataException($"Read {encodingName}: {value} | Expected: {joinedOptions} | Ending position: 0x{Position:X}");
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

        public uint AssertUInt32(uint option)
            => Assert(ReadUInt32(), nameof(UInt32), "0x{0:X}", option);

        public uint AssertUInt32(ReadOnlySpan<uint> options)
            => Assert(ReadUInt32(), nameof(UInt32), "0x{0:X}", options);

        public long AssertInt64(long option)
            => Assert(ReadInt64(), nameof(Int64), "0x{0:X}", option);

        public long AssertInt64(ReadOnlySpan<long> options)
            => Assert(ReadInt64(), nameof(Int64), "0x{0:X}", options);

        public ulong AssertUInt64(ulong option)
            => Assert(ReadUInt64(), nameof(UInt64), "0x{0:X}", option);

        public ulong AssertUInt64(ReadOnlySpan<ulong> options)
            => Assert(ReadUInt64(), nameof(UInt64), "0x{0:X}", options);

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

        public bool AssertBoolean(ReadOnlySpan<bool> options)
            => Assert(ReadBoolean(), nameof(Boolean), "{0}", options);

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

        #endregion

        #region Read Array

        private static TValue[] ReadArray<TValue>(Func<TValue> read, int count)
        {
            TValue[] values = new TValue[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = read();
            }
            return values;
        }

        private TTo[] ReadArrayEndianConvert<TFrom, TTo>(Func<TFrom> readFrom, Func<TTo> readTo, Func<TFrom, TFrom> reverseEndianness, Func<TFrom, TTo> convert, int count)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                var values = new TTo[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = convert(reverseEndianness(readFrom()));
                }
                return values;
            }

            return ReadArray(readTo, count);
        }

        private ReadOnlySpan<float> ReadVectorComponents(int vectorCount, int componentCount)
            => ReadArrayEndianConvert(_br.ReadUInt32, _br.ReadSingle, BinaryPrimitives.ReverseEndianness, BitConverter.UInt32BitsToSingle, vectorCount * componentCount);

        public void ReadBytes(Span<byte> output)
        {
            if (_br.Read(output) != output.Length)
            {
                throw new Exception($"Could not read the requested number of bytes from the stream: {output.Length}");
            }
        }

        public sbyte[] ReadSBytes(int count)
            => ReadArray(ReadSByte, count);

        public byte[] ReadBytes(int count)
            => ReadArray(ReadByte, count);

        public short[] ReadInt16s(int count)
            => ReadArray(ReadInt16, count);

        public ushort[] ReadUInt16s(int count)
            => ReadArray(ReadUInt16, count);

        public int[] ReadInt32s(int count)
            => ReadArray(ReadInt32, count);

        public uint[] ReadUInt32s(int count)
            => ReadArray(ReadUInt32, count);

        public long[] ReadInt64s(int count)
            => ReadArray(ReadInt64, count);

        public ulong[] ReadUInt64s(int count)
            => ReadArray(ReadUInt64, count);

        public Half[] ReadHalfs(int count)
            => ReadArray(ReadHalf, count);

        public float[] ReadSingles(int count)
            => ReadArray(ReadSingle, count);

        public double[] ReadDoubles(int count)
            => ReadArray(ReadDouble, count);

        public bool[] ReadBooleans(int count)
            => ReadArray(ReadBoolean, count);

        public Vector2[] ReadVector2s(int count)
            => MemoryMarshal.Cast<float, Vector2>(ReadVectorComponents(count, 2)).ToArray(); // Reinterpret components as values

        public Vector3[] ReadVector3s(int count)
            => MemoryMarshal.Cast<float, Vector3>(ReadVectorComponents(count, 3)).ToArray(); // Reinterpret components as values

        public Vector4[] ReadVector4s(int count)
            => MemoryMarshal.Cast<float, Vector4>(ReadVectorComponents(count, 4)).ToArray(); // Reinterpret components as values

        public Quaternion[] ReadQuaternions(int count)
            => MemoryMarshal.Cast<float, Quaternion>(ReadVectorComponents(count, 4)).ToArray(); // Reinterpret components as values

        #endregion

        #region Get Array

        private TValue[] GetArray<TValue>(Func<int, TValue[]> readArray, long position, int count)
        {
            long returnPosition = Position;
            Position = position;
            TValue[] values = readArray(count);
            Position = returnPosition;
            return values;
        }

        public sbyte[] GetSBytes(long position, int count)
            => GetArray(ReadSBytes, position, count);

        public byte[] GetBytes(long position, int count)
            => GetArray(ReadBytes, position, count);

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

        public Half[] GetHalfs(long position, int count)
            => GetArray(ReadHalfs, position, count);

        public float[] GetSingles(long position, int count)
            => GetArray(ReadSingles, position, count);

        public double[] GetDoubles(long position, int count)
            => GetArray(ReadDoubles, position, count);

        public bool[] GetBooleans(long position, int count)
            => GetArray(ReadBooleans, position, count);

        public Vector2[] GetVector2s(long posiiton, int count)
            => GetArray(ReadVector2s, posiiton, count);

        public Vector3[] GetVector3s(long posiiton, int count)
            => GetArray(ReadVector3s, posiiton, count);

        public Vector4[] GetVector4s(long posiiton, int count)
            => GetArray(ReadVector4s, posiiton, count);

        public Quaternion[] GetQuaternions(long posiiton, int count)
            => GetArray(ReadQuaternions, posiiton, count);

        #endregion

        #region Peek Array

        private T[] PeekArray<T>(Func<int, T[]> readArray, int count)
        {
            long startPosition = Position;
            T[] values = readArray(count);
            Position = startPosition;
            return values;
        }

        public sbyte[] PeekSBytes(int count)
            => PeekArray(ReadSBytes, count);

        public byte[] PeekBytes(int count)
            => PeekArray(ReadBytes, count);

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

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _br.Dispose();
                    _steps.Clear();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
