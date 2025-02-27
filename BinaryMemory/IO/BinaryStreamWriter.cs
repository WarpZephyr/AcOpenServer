using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A writer that writes to a <see cref="Stream"/>.<br/>
    /// Expands upon <see cref="BinaryWriter"/>.
    /// </summary>
    public class BinaryStreamWriter : IBinaryWriter, IDisposable
    {
        /// <summary>
        /// The underlying <see cref="BinaryWriter"/>.
        /// </summary>
        private readonly BinaryWriter _bw;

        /// <summary>
        /// Steps into the stream.
        /// </summary>
        private readonly Stack<long> _steps;

        /// <summary>
        /// Reservations on the stream to be filled later.
        /// </summary>
        private readonly Dictionary<string, long> _reservations;

        /// <summary>
        /// The underlying <see cref="Stream"/>.
        /// </summary>
        public Stream BaseStream => _bw.BaseStream;

        /// <summary>
        /// The current position of the writer.
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
        /// Whether or not the <see cref="BinaryStreamWriter"/> has been disposed.
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
        /// The amount of positions the writer is stepped into.
        /// </summary>
        public int StepInCount => _steps.Count;

        /// <summary>
        /// Create a new <see cref="BinaryStreamWriter"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        /// <param name="leaveOpen">Whether or not to leave the underlying <see cref="Stream"/> open when disposing.</param>
        public BinaryStreamWriter(Stream stream, bool bigEndian = false, bool leaveOpen = false)
        {
            _bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
            _steps = new Stack<long>();
            _reservations = new Dictionary<string, long>();
            BigEndian = bigEndian;
        }

        /// <summary>
        /// Create a new <see cref="BinaryStreamWriter"/> from an array of bytes.
        /// </summary>
        /// <param name="bytes">An array of bytes to write to.</param>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        public BinaryStreamWriter(byte[] bytes, bool bigEndian = false) : this(new MemoryStream(bytes, true), bigEndian, false) { }

        /// <summary>
        /// Create a new <see cref="BinaryStreamWriter"/> from a file.
        /// </summary>
        /// <param name="path">The path to the file to write to.</param>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        public BinaryStreamWriter(string path, bool bigEndian = false) : this(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), bigEndian, false) { }

        /// <summary>
        /// Create a new <see cref="BinaryStreamWriter"/>.
        /// </summary>
        /// <param name="bigEndian">Whether or not to write in big endian byte ordering.</param>
        public BinaryStreamWriter(bool bigEndian = false) : this(new MemoryStream(), bigEndian) { }

        #region Array

        /// <summary>
        /// Return the currently written data as an array of bytes.
        /// </summary>
        public byte[] ToArray()
        {
            return ((MemoryStream)BaseStream).ToArray();
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
                throw new InvalidOperationException("Writer is already stepped all the way out.");
            }

            Position = _steps.Pop();
        }

        #endregion

        #region Pad

        /// <summary>
        /// Writes null bytes until the stream position meets the specified alignment.
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

        #region Pattern

        public void WriteBytePattern(int length, byte pattern)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = pattern;
            }
            BaseStream.Write(bytes, 0, length);
        }

        #endregion

        #region Write

        private TValue SetValueEndianness<TValue>(TValue value, Func<TValue, TValue> reverseEndianness)
        {
            if (BigEndian != !BitConverter.IsLittleEndian)
            {
                value = reverseEndianness(value);
            }

            return value;
        }

        public void WriteSByte(sbyte value)
           => _bw.Write(value);

        public void WriteByte(byte value)
            => _bw.Write(value);

        public void WriteInt16(short value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteUInt16(ushort value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteInt32(int value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteUInt32(uint value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteInt64(long value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteUInt64(ulong value)
            => _bw.Write(SetValueEndianness(value, BinaryPrimitives.ReverseEndianness));

        public void WriteHalf(Half value)
            => _bw.Write(SetValueEndianness(BitConverter.HalfToUInt16Bits(value), BinaryPrimitives.ReverseEndianness));

        public void WriteSingle(float value)
            => _bw.Write(SetValueEndianness(BitConverter.SingleToUInt32Bits(value), BinaryPrimitives.ReverseEndianness));

        public void WriteDouble(double value)
            => _bw.Write(SetValueEndianness(BitConverter.DoubleToUInt64Bits(value), BinaryPrimitives.ReverseEndianness));

        public void WriteBoolean(bool value)
            => _bw.Write(value);

        public void WriteChar(char value)
            => _bw.Write(value);

        public void WriteVector2(Vector2 value)
        {
            WriteSingle(value.X);
            WriteSingle(value.Y);
        }

        public void WriteVector3(Vector3 value)
        {
            WriteSingle(value.X);
            WriteSingle(value.Y);
            WriteSingle(value.Z);
        }

        public void WriteVector4(Vector4 value)
        {
            WriteSingle(value.X);
            WriteSingle(value.Y);
            WriteSingle(value.Z);
            WriteSingle(value.W);
        }

        public void WriteQuaternion(Quaternion value)
        {
            WriteSingle(value.X);
            WriteSingle(value.Y);
            WriteSingle(value.Z);
            WriteSingle(value.W);
        }

        public void WriteColor3(byte[] color)
        {
            WriteByte(color[0]);
            WriteByte(color[1]);
            WriteByte(color[2]);
        }

        public void WriteColorRGB(Color color)
        {
            WriteByte(color.R);
            WriteByte(color.G);
            WriteByte(color.B);
        }

        public void WriteColorBGR(Color color)
        {
            WriteByte(color.B);
            WriteByte(color.G);
            WriteByte(color.R);
        }

        public void WriteColor4(byte[] color)
        {
            WriteByte(color[0]);
            WriteByte(color[1]);
            WriteByte(color[2]);
            WriteByte(color[3]);
        }

        public void WriteColorRGBA(Color color)
        {
            WriteByte(color.R);
            WriteByte(color.G);
            WriteByte(color.B);
            WriteByte(color.A);
        }

        public void WriteColorBGRA(Color color)
        {
            WriteByte(color.B);
            WriteByte(color.G);
            WriteByte(color.R);
            WriteByte(color.A);
        }

        public void WriteColorARGB(Color color)
        {
            WriteByte(color.A);
            WriteByte(color.R);
            WriteByte(color.G);
            WriteByte(color.B);
        }

        public void WriteColorABGR(Color color)
        {
            WriteByte(color.A);
            WriteByte(color.B);
            WriteByte(color.G);
            WriteByte(color.R);
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
                throw new ArgumentException("Key already reserved: " + name);
            }

            _reservations[name] = BaseStream.Position;
            for (int i = 0; i < length; i++)
            {
                WriteByte(0xFE);
            }
        }

        public void ReserveSByte(string name)
            => Reserve(name, nameof(SByte), sizeof(sbyte));

        public void ReserveByte(string name)
            => Reserve(name, nameof(Byte), sizeof(byte));

        public void ReserveInt16(string name)
            => Reserve(name, nameof(Int16), sizeof(short));

        public void ReserveUInt16(string name)
            => Reserve(name, nameof(UInt16), sizeof(ushort));

        public void ReserveInt32(string name)
            => Reserve(name, nameof(Int32), sizeof(int));

        public void ReserveUInt32(string name)
            => Reserve(name, nameof(UInt32), sizeof(uint));

        public void ReserveInt64(string name)
            => Reserve(name, nameof(Int64), sizeof(long));

        public void ReserveUInt64(string name)
            => Reserve(name, nameof(UInt64), sizeof(ulong));

        public void ReserveHalf(string name)
            => Reserve(name, nameof(Half), sizeof(ushort));

        public void ReserveSingle(string name)
            => Reserve(name, nameof(Single), sizeof(float));

        public void ReserveDouble(string name)
            => Reserve(name, nameof(Double), sizeof(double));

        #endregion

        #region Fill

        private long TakeReservation(string name, string typeName)
        {
            name = $"{name}:{typeName}";
            if (!_reservations.TryGetValue(name, out long jump))
            {
                throw new ArgumentException("Key is not reserved: " + name);
            }

            _reservations.Remove(name);
            return jump;
        }

        private void Fill<TValue>(string name, string typeName, Action<TValue> write, TValue value)
        {
            var oldpos = Position;
            Position = TakeReservation(name, typeName);
            write(value);
            Position = oldpos;
        }

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

        public void FillHalf(string name, Half value)
            => Fill(name, nameof(Half), WriteHalf, value);

        public void FillSingle(string name, float value)
            => Fill(name, nameof(Single), WriteSingle, value);

        public void FillDouble(string name, double value)
            => Fill(name, nameof(Double), WriteDouble, value);

        #endregion

        #region Write Array

        private static void WriteArray<TValue>(Action<TValue> write, IList<TValue> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                write(values[i]);
            }
        }

        public void WriteSBytes(IList<sbyte> values)
            => WriteArray(WriteSByte, values);

        public void WriteBytes(IList<byte> values)
            => WriteArray(WriteByte, values);

        public void WriteInt16s(IList<short> values)
            => WriteArray(WriteInt16, values);

        public void WriteUInt16s(IList<ushort> values)
            => WriteArray(WriteUInt16, values);

        public void WriteInt32s(IList<int> values)
            => WriteArray(WriteInt32, values);

        public void WriteUInt32s(IList<uint> values)
            => WriteArray(WriteUInt32, values);

        public void WriteInt64s(IList<long> values)
            => WriteArray(WriteInt64, values);

        public void WriteUInt64s(IList<ulong> values)
            => WriteArray(WriteUInt64, values);

        public void WriteHalfs(IList<Half> values)
            => WriteArray(WriteHalf, values);

        public void WriteSingles(IList<float> values)
            => WriteArray(WriteSingle, values);

        public void WriteDoubles(IList<double> values)
            => WriteArray(WriteDouble, values);

        public void WriteBooleans(IList<bool> values)
            => WriteArray(WriteBoolean, values);

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// <para>Verifies that all reservations are filled.</para>
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _bw.Dispose();
                    _steps.Clear();

                    if (_reservations.Count > 0)
                    {
                        throw new InvalidOperationException("Not all reservations filled: " + string.Join(", ", _reservations.Keys));
                    }
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// <para>Verifies that all reservations are filled.</para>
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
