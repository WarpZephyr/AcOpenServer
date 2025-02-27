using BinaryMemory.Helpers;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static BinaryMemory.Helpers.AssertHelper;
using static BinaryMemory.Helpers.StringHelper;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A binary reader for spans supporting endianness.
    /// </summary>
    public ref struct BinarySpanReader
    {
        #region Fields

        /// <summary>
        /// The underlying span.
        /// </summary>
        private readonly ReadOnlySpan<byte> Buffer;

        /// <summary>
        /// The current position of the reader.
        /// </summary>
        private int BufferOffset;

        /// <summary>
        /// Whether or not to read in big endian.
        /// </summary>
        public bool BigEndian { get; set; }

        #endregion

        #region Property Methods

        /// <summary>
        /// Whether or not endianness is reversed.
        /// </summary>
        private readonly bool IsEndiannessReversed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BigEndian != !BitConverter.IsLittleEndian;
        }

        /// <summary>
        /// The current position of the reader.
        /// </summary>
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => BufferOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)value, (uint)Length, nameof(value));
                BufferOffset = value;
            }
        }

        /// <summary>
        /// The length of the span.
        /// </summary>
        public readonly int Length => Buffer.Length;

        /// <summary>
        /// The remaining length of the span from the current position.
        /// </summary>
        public readonly int Remaining => Buffer.Length - BufferOffset;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="BinarySpanReader"/> from an <see cref="Array"/> of <see cref="byte"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="Array"/> to read.</param>
        public BinarySpanReader(byte[] buffer)
        {
            Buffer = buffer;
            BufferOffset = 0;
            BigEndian = !BitConverter.IsLittleEndian;
        }

        /// <summary>
        /// Create a new <see cref="BinarySpanReader"/> from a <see cref="Span{T}"/> of <see cref="byte"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="Span{T}"/> to read.</param>
        public BinarySpanReader(Span<byte> buffer)
        {
            Buffer = buffer;
            BufferOffset = 0;
            BigEndian = !BitConverter.IsLittleEndian;
        }

        /// <summary>
        /// Create a new <see cref="BinarySpanReader"/> from a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="ReadOnlySpan{T}"/> to read.</param>
        public BinarySpanReader(ReadOnlySpan<byte> buffer)
        {
            Buffer = buffer;
            BufferOffset = 0;
            BigEndian = !BitConverter.IsLittleEndian;
        }

        #endregion

        #region Seek

        /// <summary>
        /// Go back the specified count of bytes.
        /// </summary>
        /// <param name="count">The amount to rewind.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(int count)
            => Position -= count;

        /// <summary>
        /// Go forward the specified count of bytes.
        /// </summary>
        /// <param name="count">The amount to skip.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count)
            => Position += count;

        /// <summary>
        /// Seek to the specified position based on the start of the span.
        /// </summary>
        /// <param name="position">The position to seek to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position)
            => Position = position;

        /// <summary>
        /// Seek to the specified offset from the specified <see cref="SeekOrigin"/>.
        /// </summary>
        /// <param name="offset">The offset to seek to.</param>
        /// <param name="origin">The origin to seek from.</param>
        /// <exception cref="NotSupportedException">The specified <see cref="SeekOrigin"/> was unknown.</exception>
        public void Seek(int offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new NotSupportedException($"Unknown {nameof(SeekOrigin)}: {origin}");
            }
        }

        #endregion

        #region Align

        /// <summary>
        /// Align the position of the reader to the specified alignment.
        /// </summary>
        /// <param name="alignment">The specified alignment.</param>
        /// <exception cref="ArgumentOutOfRangeException">The alignment argument was out of range.</exception>
        /// <exception cref="InvalidOperationException">The next alignment position was out of range.</exception>
        public void Align(int alignment)
        {
            if (alignment < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alignment), $"Alignment value must be positive and non-zero: {alignment} < {1}");
            }

            int remainder = BufferOffset % alignment;
            if (remainder > 0)
            {
                int finalPosition = checked(BufferOffset + (alignment - remainder));
                if (finalPosition > Length)
                {
                    throw new InvalidOperationException($"Next alignment position is out of range: {finalPosition} > {Length}");
                }

                BufferOffset = finalPosition;
            }
        }

        /// <summary>
        /// Align the position of the reader relative to the specified position to the specified alignment.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="alignment">The specified alignment.</param>
        /// <exception cref="InvalidOperationException">An argument or the next alignment position was out of range.</exception>
        public void AlignFrom(int position, int alignment)
        {
            if (position < 1 || position > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Position value is out of range: {position} < {1} || {position} > {Length}");
            }

            if (alignment < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alignment), $"Alignment value must be positive and non-zero: {alignment} < {1}");
            }

            int remainder = position % alignment;
            if (remainder > 0)
            {
                int finalPosition = checked(position + (alignment - remainder));
                if (finalPosition > Length)
                {
                    throw new InvalidOperationException($"Next alignment position is out of range: {finalPosition} > {Length}");
                }

                BufferOffset = finalPosition;
            }
        }

        #endregion

        #region Read

        public unsafe T Read<T>() where T : unmanaged
        {
            int size = sizeof(T);
            int endPosition = BufferOffset + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            var value = Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(Buffer), BufferOffset));
            BufferOffset = endPosition;
            return value;
        }

        public unsafe ReadOnlySpan<T> ReadSpan<T>(int count) where T : unmanaged
        {
            int size = sizeof(T) * count;
            int endPosition = BufferOffset + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            var value = MemoryMarshal.Cast<byte, T>(Buffer.Slice(BufferOffset, size));
            BufferOffset = endPosition;
            return value;
        }

        public readonly unsafe T Get<T>(int position) where T : unmanaged
        {
            int size = sizeof(T);
            int endPosition = position + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            return Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(Buffer), position));
        }

        public readonly unsafe ReadOnlySpan<T> GetSpan<T>(int position, int count) where T : unmanaged
        {
            int size = sizeof(T) * count;
            int endPosition = position + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            return MemoryMarshal.Cast<byte, T>(Buffer.Slice(position, size));
        }

        public readonly unsafe T Peek<T>() where T : unmanaged
        {
            int size = sizeof(T);
            int endPosition = BufferOffset + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            return Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(Buffer), BufferOffset));
        }

        public readonly unsafe ReadOnlySpan<T> PeekSpan<T>(int count) where T : unmanaged
        {
            int size = sizeof(T) * count;
            int endPosition = BufferOffset + size;
            if (endPosition > Length)
            {
                throw new InvalidOperationException("Cannot read beyond the specified span.");
            }

            return MemoryMarshal.Cast<byte, T>(Buffer.Slice(BufferOffset, size));
        }

        #endregion

        #region SByte

        /// <summary>
        /// Reads an <see cref="sbyte"/>.
        /// </summary>
        /// <returns>An <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
            => Read<sbyte>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<sbyte> ReadSByteSpan(int count)
            => ReadSpan<sbyte>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="sbyte"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte[] ReadSBytes(int count)
            => ReadSpan<sbyte>(count).ToArray();

        /// <summary>
        /// Gets an <see cref="sbyte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>An <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly sbyte GetSByte(int position)
            => Get<sbyte>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<sbyte> GetSByteSpan(int position, int count)
            => GetSpan<sbyte>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="sbyte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly sbyte[] GetSBytes(int position, int count)
            => GetSpan<sbyte>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="sbyte"/> without advancing.
        /// </summary>
        /// <returns>An <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly sbyte PeekSByte()
            => Peek<sbyte>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<sbyte> PeekSByteSpan(int count)
            => PeekSpan<sbyte>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="sbyte"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly sbyte[] PeekSBytes(int count)
            => PeekSpan<sbyte>(count).ToArray();

        /// <summary>
        /// Reads an <see cref="sbyte"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>An <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte AssertSByte(sbyte option)
            => Assert(ReadSByte(), nameof(SByte), option);

        /// <summary>
        /// Reads an <see cref="sbyte"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>An <see cref="sbyte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte AssertSByte(ReadOnlySpan<sbyte> options)
            => Assert(ReadSByte(), nameof(SByte), options);

        #endregion

        #region Byte

        /// <summary>
        /// Reads a <see cref="byte"/>.
        /// </summary>
        /// <returns>A <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
            => Read<byte>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadByteSpan(int count)
            => ReadSpan<byte>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="byte"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
            => ReadSpan<byte>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="byte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte GetByte(int position)
            => Get<byte>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<byte> GetByteSpan(int position, int count)
            => GetSpan<byte>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="byte"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte[] GetBytes(int position, int count)
            => GetSpan<byte>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="byte"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte PeekByte()
            => Peek<byte>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<byte> PeekByteSpan(int count)
            => PeekSpan<byte>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="byte"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte[] PeekBytes(int count)
            => PeekSpan<byte>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="byte"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AssertByte(byte option)
            => Assert(ReadByte(), nameof(Byte), option);

        /// <summary>
        /// Reads a <see cref="byte"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AssertByte(ReadOnlySpan<byte> options)
            => Assert(ReadByte(), nameof(Byte), options);

        #endregion

        #region Int16

        /// <summary>
        /// Reads a <see cref="short"/>.
        /// </summary>
        /// <returns>A <see cref="short"/>.</returns>
        public short ReadInt16()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<short>())
            : Read<short>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="short"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="short"/>.</returns>
        public ReadOnlySpan<short> ReadInt16Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<short>(count).GetEndianReversedCopy()
            : ReadSpan<short>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="short"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="short"/>.</returns>
        public short[] ReadInt16s(int count)
            => IsEndiannessReversed
            ? ReadSpan<short>(count).ToEndianReversedArray()
            : ReadSpan<short>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="short"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="short"/>.</returns>
        public readonly short GetInt16(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<short>(position))
            : Get<short>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="short"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="short"/>.</returns>
        public readonly ReadOnlySpan<short> GetInt16Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<short>(position, count).GetEndianReversedCopy()
            : GetSpan<short>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="short"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="short"/>.</returns>
        public readonly short[] GetInt16s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<short>(position, count).ToEndianReversedArray()
            : GetSpan<short>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="short"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="short"/>.</returns>
        public readonly short PeekInt16()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<short>())
            : Peek<short>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="short"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="short"/>.</returns>
        public readonly ReadOnlySpan<short> PeekInt16Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<short>(count).GetEndianReversedCopy()
            : PeekSpan<short>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="short"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="short"/>.</returns>
        public readonly short[] PeekInt16s(int count)
            => IsEndiannessReversed
            ? PeekSpan<short>(count).ToEndianReversedArray()
            : PeekSpan<short>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="short"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="short"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short AssertInt16(short option)
            => Assert(ReadInt16(), nameof(Int16), option);

        /// <summary>
        /// Reads a <see cref="short"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="short"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short AssertInt16(ReadOnlySpan<short> options)
            => Assert(ReadInt16(), nameof(Int16), options);

        #endregion

        #region UInt16

        /// <summary>
        /// Reads a <see cref="ushort"/>.
        /// </summary>
        /// <returns>A <see cref="ushort"/>.</returns>
        public ushort ReadUInt16()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<ushort>())
            : Read<ushort>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/>.</returns>
        public ReadOnlySpan<ushort> ReadUInt16Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<ushort>(count).GetEndianReversedCopy()
            : ReadSpan<ushort>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="ushort"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ushort"/>.</returns>
        public ushort[] ReadUInt16s(int count)
            => IsEndiannessReversed
            ? ReadSpan<ushort>(count).ToEndianReversedArray()
            : ReadSpan<ushort>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="ushort"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="ushort"/>.</returns>
        public readonly ushort GetUInt16(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<ushort>(position))
            : Get<ushort>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/>.</returns>
        public readonly ReadOnlySpan<ushort> GetUInt16Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<ushort>(position, count).GetEndianReversedCopy()
            : GetSpan<ushort>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="ushort"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ushort"/>.</returns>
        public readonly ushort[] GetUInt16s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<ushort>(position, count).ToEndianReversedArray()
            : GetSpan<ushort>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="ushort"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="ushort"/>.</returns>
        public readonly ushort PeekUInt16()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<ushort>())
            : Peek<ushort>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/>.</returns>
        public readonly ReadOnlySpan<ushort> PeekUInt16Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<ushort>(count).GetEndianReversedCopy()
            : PeekSpan<ushort>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="ushort"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ushort"/>.</returns>
        public readonly ushort[] PeekUInt16s(int count)
            => IsEndiannessReversed
            ? PeekSpan<ushort>(count).ToEndianReversedArray()
            : PeekSpan<ushort>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="ushort"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="ushort"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort AssertUInt16(ushort option)
            => Assert(ReadUInt16(), nameof(UInt16), option);

        /// <summary>
        /// Reads a <see cref="ushort"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="ushort"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort AssertUInt16(ReadOnlySpan<ushort> options)
            => Assert(ReadUInt16(), nameof(UInt16), options);

        #endregion

        #region Int32

        /// <summary>
        /// Reads an <see cref="int"/>.
        /// </summary>
        /// <returns>An <see cref="int"/>.</returns>
        public int ReadInt32()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<int>())
            : Read<int>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.</returns>
        public ReadOnlySpan<int> ReadInt32Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<int>(count).GetEndianReversedCopy()
            : ReadSpan<int>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="int"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="int"/>.</returns>
        public int[] ReadInt32s(int count)
            => IsEndiannessReversed
            ? ReadSpan<int>(count).ToEndianReversedArray()
            : ReadSpan<int>(count).ToArray();

        /// <summary>
        /// Gets an <see cref="int"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>An <see cref="int"/>.</returns>
        public readonly int GetInt32(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<int>(position))
            : Get<int>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.</returns>
        public readonly ReadOnlySpan<int> GetInt32Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<int>(position, count).GetEndianReversedCopy()
            : GetSpan<int>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="int"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="int"/>.</returns>
        public readonly int[] GetInt32s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<int>(position, count).ToEndianReversedArray()
            : GetSpan<int>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="int"/> without advancing.
        /// </summary>
        /// <returns>An <see cref="int"/>.</returns>
        public readonly int PeekInt32()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<int>())
            : Peek<int>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="int"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.</returns>
        public readonly ReadOnlySpan<int> PeekInt32Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<int>(count).GetEndianReversedCopy()
            : PeekSpan<int>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="int"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="int"/>.</returns>
        public readonly int[] PeekInt32s(int count)
            => IsEndiannessReversed
            ? PeekSpan<int>(count).ToEndianReversedArray()
            : PeekSpan<int>(count).ToArray();

        /// <summary>
        /// Reads an <see cref="int"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>An <see cref="int"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AssertInt32(int option)
            => Assert(ReadInt32(), nameof(Int32), option);

        /// <summary>
        /// Reads an <see cref="int"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>An <see cref="int"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AssertInt32(ReadOnlySpan<int> options)
            => Assert(ReadInt32(), nameof(Int32), options);

        #endregion

        #region UInt32

        /// <summary>
        /// Reads an <see cref="uint"/>.
        /// </summary>
        /// <returns>An <see cref="uint"/>.</returns>
        public uint ReadUInt32()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<uint>())
            : Read<uint>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/>.</returns>
        public ReadOnlySpan<uint> ReadUInt32Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<uint>(count).GetEndianReversedCopy()
            : ReadSpan<uint>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="uint"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="uint"/>.</returns>
        public uint[] ReadUInt32s(int count)
            => IsEndiannessReversed
            ? ReadSpan<uint>(count).ToEndianReversedArray()
            : ReadSpan<uint>(count).ToArray();

        /// <summary>
        /// Gets an <see cref="uint"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>An <see cref="uint"/>.</returns>
        public readonly uint GetUInt32(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<uint>(position))
            : Get<uint>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/>.</returns>
        public readonly ReadOnlySpan<uint> GetUInt32Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<uint>(position, count).GetEndianReversedCopy()
            : GetSpan<uint>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="uint"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="uint"/>.</returns>
        public readonly uint[] GetUInt32s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<uint>(position, count).ToEndianReversedArray()
            : GetSpan<uint>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="uint"/> without advancing.
        /// </summary>
        /// <returns>An <see cref="uint"/>.</returns>
        public readonly uint PeekUInt32()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<uint>())
            : Peek<uint>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="uint"/>.</returns>
        public readonly ReadOnlySpan<uint> PeekUInt32Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<uint>(count).GetEndianReversedCopy()
            : PeekSpan<uint>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="uint"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="uint"/>.</returns>
        public readonly uint[] PeekUInt32s(int count)
            => IsEndiannessReversed
            ? PeekSpan<uint>(count).ToEndianReversedArray()
            : PeekSpan<uint>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="uint"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="uint"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint AssertUInt32(uint option)
            => Assert(ReadUInt32(), nameof(UInt32), option);

        /// <summary>
        /// Reads a <see cref="uint"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="uint"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint AssertUInt32(ReadOnlySpan<uint> options)
            => Assert(ReadUInt32(), nameof(UInt32), options);

        #endregion

        #region Int64

        /// <summary>
        /// Reads a <see cref="long"/>.
        /// </summary>
        /// <returns>A <see cref="long"/>.</returns>
        public long ReadInt64()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<long>())
            : Read<long>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.</returns>
        public ReadOnlySpan<long> ReadInt64Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<long>(count).GetEndianReversedCopy()
            : ReadSpan<long>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="long"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="long"/>.</returns>
        public long[] ReadInt64s(int count)
            => IsEndiannessReversed
            ? ReadSpan<long>(count).ToEndianReversedArray()
            : ReadSpan<long>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="long"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="long"/>.</returns>
        public readonly long GetInt64(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<long>(position))
            : Get<long>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.</returns>
        public readonly ReadOnlySpan<long> GetInt64Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<long>(position, count).GetEndianReversedCopy()
            : GetSpan<long>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="long"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="long"/>.</returns>
        public readonly long[] GetInt64s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<long>(position, count).ToEndianReversedArray()
            : GetSpan<long>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="long"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="long"/>.</returns>
        public readonly long PeekInt64()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<long>())
            : Peek<long>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.</returns>
        public readonly ReadOnlySpan<long> PeekInt64Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<long>(count).GetEndianReversedCopy()
            : PeekSpan<long>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="long"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="long"/>.</returns>
        public readonly long[] PeekInt64s(int count)
            => IsEndiannessReversed
            ? PeekSpan<long>(count).ToEndianReversedArray()
            : PeekSpan<long>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="long"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="long"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long AssertInt64(long option)
            => Assert(ReadInt64(), nameof(Int64), option);

        /// <summary>
        /// Reads a <see cref="long"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="long"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long AssertInt64(ReadOnlySpan<long> options)
            => Assert(ReadInt64(), nameof(Int64), options);

        #endregion   

        #region UInt64

        /// <summary>
        /// Reads a <see cref="ulong"/>.
        /// </summary>
        /// <returns>A <see cref="ulong"/>.</returns>
        public ulong ReadUInt64()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Read<ulong>())
            : Read<ulong>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/>.</returns>
        public ReadOnlySpan<ulong> ReadUInt64Span(int count)
            => IsEndiannessReversed
            ? ReadSpan<ulong>(count).GetEndianReversedCopy()
            : ReadSpan<ulong>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="ulong"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ulong"/>.</returns>
        public ulong[] ReadUInt64s(int count)
            => IsEndiannessReversed
            ? ReadSpan<ulong>(count).ToEndianReversedArray()
            : ReadSpan<ulong>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="ulong"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="ulong"/>.</returns>
        public readonly ulong GetUInt64(int position)
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Get<ulong>(position))
            : Get<ulong>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/>.</returns>
        public readonly ReadOnlySpan<ulong> GetUInt64Span(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<ulong>(position, count).GetEndianReversedCopy()
            : GetSpan<ulong>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="ulong"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ulong"/>.</returns>
        public readonly ulong[] GetUInt64s(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<ulong>(position, count).ToEndianReversedArray()
            : GetSpan<ulong>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="ulong"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="ulong"/>.</returns>
        public readonly ulong PeekUInt64()
            => IsEndiannessReversed
            ? BinaryPrimitives.ReverseEndianness(Peek<ulong>())
            : Peek<ulong>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/>.</returns>
        public readonly ReadOnlySpan<ulong> PeekUInt64Span(int count)
            => IsEndiannessReversed
            ? PeekSpan<ulong>(count).GetEndianReversedCopy()
            : PeekSpan<ulong>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="ulong"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="ulong"/>.</returns>
        public readonly ulong[] PeekUInt64s(int count)
            => IsEndiannessReversed
            ? PeekSpan<ulong>(count).ToEndianReversedArray()
            : PeekSpan<ulong>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="ulong"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="ulong"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong AssertUInt64(ulong option)
            => Assert(ReadUInt64(), nameof(UInt64), option);

        /// <summary>
        /// Reads a <see cref="ulong"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="ulong"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong AssertUInt64(ReadOnlySpan<ulong> options)
            => Assert(ReadUInt64(), nameof(UInt64), options);

        #endregion

        #region Half

        /// <summary>
        /// Reads a <see cref="Half"/>.
        /// </summary>
        /// <returns>A <see cref="Half"/>.</returns>
        public Half ReadHalf()
            => IsEndiannessReversed
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(Read<ushort>()))
            : Read<Half>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/>.</returns>
        public ReadOnlySpan<Half> ReadHalfSpan(int count)
            => IsEndiannessReversed
            ? ReadSpan<Half>(count).GetEndianReversedCopy()
            : ReadSpan<Half>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="Half"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="Half"/>.</returns>
        public Half[] ReadHalfs(int count)
            => IsEndiannessReversed
            ? ReadSpan<Half>(count).ToEndianReversedArray()
            : ReadSpan<Half>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="Half"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="Half"/>.</returns>
        public readonly Half GetHalf(int position)
            => IsEndiannessReversed
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(Get<ushort>(position)))
            : Get<Half>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/>.</returns>
        public readonly ReadOnlySpan<Half> GetHalfSpan(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<Half>(position, count).GetEndianReversedCopy()
            : GetSpan<Half>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="Half"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="Half"/>.</returns>
        public readonly Half[] GetHalfs(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<Half>(position, count).ToEndianReversedArray()
            : GetSpan<Half>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="Half"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="Half"/>.</returns>
        public readonly Half PeekHalf()
            => IsEndiannessReversed
            ? BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(Peek<ushort>()))
            : Peek<Half>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="Half"/>.</returns>
        public readonly ReadOnlySpan<Half> PeekHalfSpan(int count)
            => IsEndiannessReversed
            ? PeekSpan<Half>(count).GetEndianReversedCopy()
            : PeekSpan<Half>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="Half"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="Half"/>.</returns>
        public readonly Half[] PeekHalfs(int count)
            => IsEndiannessReversed
            ? PeekSpan<Half>(count).ToEndianReversedArray()
            : PeekSpan<Half>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="Half"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="Half"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Half AssertHalf(Half option)
            => Assert(ReadHalf(), nameof(Half), option);

        /// <summary>
        /// Reads a <see cref="Half"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="Half"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Half AssertHalf(ReadOnlySpan<Half> options)
            => Assert(ReadHalf(), nameof(Half), options);

        #endregion

        #region Single

        /// <summary>
        /// Reads a <see cref="float"/>.
        /// </summary>
        /// <returns>A <see cref="float"/>.</returns>
        public float ReadSingle()
            => IsEndiannessReversed
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Read<uint>()))
            : Read<float>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="float"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="float"/>.</returns>
        public ReadOnlySpan<float> ReadSingleSpan(int count)
            => IsEndiannessReversed
            ? ReadSpan<float>(count).GetEndianReversedCopy()
            : ReadSpan<float>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="float"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="float"/>.</returns>
        public float[] ReadSingles(int count)
            => IsEndiannessReversed
            ? ReadSpan<float>(count).ToEndianReversedArray()
            : ReadSpan<float>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="float"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="float"/>.</returns>
        public readonly float GetSingle(int position)
            => IsEndiannessReversed
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Get<uint>(position)))
            : Get<float>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="float"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="float"/>.</returns>
        public readonly ReadOnlySpan<float> GetSingleSpan(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<float>(position, count).GetEndianReversedCopy()
            : GetSpan<float>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="float"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="float"/>.</returns>
        public readonly float[] GetSingles(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<float>(position, count).ToEndianReversedArray()
            : GetSpan<float>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="float"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="float"/>.</returns>
        public readonly float PeekSingle()
            => IsEndiannessReversed
            ? BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Peek<uint>()))
            : Peek<float>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="float"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="float"/>.</returns>
        public readonly ReadOnlySpan<float> PeekSingleSpan(int count)
            => IsEndiannessReversed
            ? PeekSpan<float>(count).GetEndianReversedCopy()
            : PeekSpan<float>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="float"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="float"/>.</returns>
        public readonly float[] PeekSingles(int count)
            => IsEndiannessReversed
            ? PeekSpan<float>(count).ToEndianReversedArray()
            : PeekSpan<float>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="float"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="float"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AssertSingle(float option)
            => Assert(ReadSingle(), nameof(Single), option);

        /// <summary>
        /// Reads a <see cref="float"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="float"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AssertSingle(ReadOnlySpan<float> options)
            => Assert(ReadSingle(), nameof(Single), options);

        #endregion

        #region Double

        /// <summary>
        /// Reads a <see cref="double"/>.
        /// </summary>
        /// <returns>A <see cref="double"/>.</returns>
        public double ReadDouble()
            => IsEndiannessReversed
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(Read<ulong>()))
            : Read<double>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="double"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="double"/>.</returns>
        public ReadOnlySpan<double> ReadDoubleSpan(int count)
            => IsEndiannessReversed
            ? ReadSpan<double>(count).GetEndianReversedCopy()
            : ReadSpan<double>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="double"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="double"/>.</returns>
        public double[] ReadDoubles(int count)
            => IsEndiannessReversed
            ? ReadSpan<double>(count).ToEndianReversedArray()
            : ReadSpan<double>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="double"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="double"/>.</returns>
        public readonly double GetDouble(int position)
            => IsEndiannessReversed
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(Get<ulong>(position)))
            : Get<double>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="double"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="double"/>.</returns>
        public readonly ReadOnlySpan<double> GetDoubleSpan(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<double>(position, count).GetEndianReversedCopy()
            : GetSpan<double>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="double"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="double"/>.</returns>
        public readonly double[] GetDoubles(int position, int count)
        => IsEndiannessReversed
            ? GetSpan<double>(position, count).ToEndianReversedArray()
            : GetSpan<double>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="double"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="double"/>.</returns>
        public readonly double PeekDouble()
            => IsEndiannessReversed
            ? BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(Peek<ulong>()))
            : Peek<double>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="double"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="double"/>.</returns>
        public readonly ReadOnlySpan<double> PeekDoubleSpan(int count)
            => IsEndiannessReversed
            ? PeekSpan<double>(count).GetEndianReversedCopy()
            : PeekSpan<double>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="double"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="double"/>.</returns>
        public readonly double[] PeekDoubles(int count)
            => IsEndiannessReversed
            ? PeekSpan<double>(count).ToEndianReversedArray()
            : PeekSpan<double>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="double"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AssertDouble(double option)
            => Assert(ReadDouble(), nameof(Double), option);

        /// <summary>
        /// Reads a <see cref="double"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AssertDouble(ReadOnlySpan<double> options)
            => Assert(ReadDouble(), nameof(Double), options);

        #endregion

        #region Char

        /// <summary>
        /// Reads a <see cref="char"/>.
        /// </summary>
        /// <returns>A <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar()
            => Read<char>();

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> ReadCharSpan(int count)
            => ReadSpan<char>(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="char"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char[] ReadChars(int count)
            => ReadSpan<char>(count).ToArray();

        /// <summary>
        /// Gets a <see cref="char"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly char GetChar(int position)
            => Get<char>(position);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<char> GetCharSpan(int position, int count)
            => GetSpan<char>(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="char"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly char[] GetChars(int position, int count)
            => GetSpan<char>(position, count).ToArray();

        /// <summary>
        /// Peeks the next <see cref="char"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly char PeekChar()
            => Peek<char>();

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<char> PeekCharSpan(int count)
            => PeekSpan<char>(count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="char"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly char[] PeekChars(int count)
            => PeekSpan<char>(count).ToArray();

        /// <summary>
        /// Reads a <see cref="char"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char AssertChar(char option)
            => Assert(ReadChar(), nameof(Char), option);

        /// <summary>
        /// Reads a <see cref="char"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char AssertChar(ReadOnlySpan<char> options)
            => Assert(ReadChar(), nameof(Char), options);

        #endregion

        #region Boolean

#if UNSTRICT_READ_BOOLEAN
        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="bool"/>.
        /// </summary>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        private bool[] ReadBooleansInteral(int count)
        {
            var array = new bool[count];
            var span = GetSpan<byte>(Position, count);
            for (int i = 0; i < count; i++)
            {
                var value = span[i];
                array[i] = value == 1;
                BufferOffset++;
            }
            return array;
        }

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        private bool[] GetBooleansInteral(int position, int count)
        {
            var array = new bool[count];
            var span = GetSpan<byte>(position, count);
            for (int i = 0; i < count; i++)
            {
                var value = span[i];
                array[i] = value == 1;
            }
            return array;
        }

        /// <summary>
        /// Reads a <see cref="bool"/>.
        /// </summary>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
            => Read<Byte>() == 1;

        /// <summary>
        /// Gets a <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool GetBoolean(int position)
            => Get<Byte>(position) == 1;

        /// <summary>
        /// Peeks the next <see cref="bool"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool PeekBoolean()
            => Peek<Byte>() == 1;
#else
        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="bool"/>.
        /// </summary>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        /// <exception cref="InvalidDataException">The read <see cref="bool"/> value was not 0 or 1.</exception>
        private bool[] ReadBooleansInteral(int count)
        {
            var array = new bool[count];
            var span = GetSpan<byte>(Position, count);
            for (int i = 0; i < count; i++)
            {
                var value = span[i];
                array[i] = value == 1 || (value == 0 ? false : throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}"));
                BufferOffset++;
            }
            return array;
        }

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        /// <exception cref="InvalidDataException">The read <see cref="bool"/> value was not 0 or 1.</exception>
        private readonly bool[] GetBooleansInteral(int position, int count)
        {
            var array = new bool[count];
            var span = GetSpan<byte>(position, count);
            for (int i = 0; i < count; i++)
            {
                var value = span[i];
                array[i] = value == 1 || (value == 0 ? false : throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}"));
            }
            return array;
        }

        /// <summary>
        /// Reads a <see cref="bool"/>.
        /// </summary>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            var value = ReadByte();
            return value == 1 || (value == 0 ? false : throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}"));
        }

        /// <summary>
        /// Gets a <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool GetBoolean(int position)
        {
            var value = GetByte(position);
            return value == 1 || (value == 0 ? false : throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}"));
        }

        /// <summary>
        /// Reads a <see cref="bool"/>.
        /// </summary>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool PeekBoolean()
        {
            var value = PeekByte();
            return value == 1 || (value == 0 ? false : throw new InvalidDataException($"{nameof(ReadBoolean)} read invalid {nameof(Boolean)} value: {value}"));
        }
#endif

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<bool> ReadBooleanSpan(int count)
            => ReadBooleansInteral(count);

        /// <summary>
        /// Reads an <see cref="Array"/> of <see cref="bool"/>.
        /// </summary>
        /// <param name="count">The amount to read.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool[] ReadBooleans(int count)
            => ReadBooleansInteral(count);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<bool> GetBooleanSpan(int position, int count)
            => GetBooleansInteral(position, count);

        /// <summary>
        /// Gets an <see cref="Array"/> of <see cref="bool"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="count">The amount to get.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool[] GetBooleans(int position, int count)
            => GetBooleansInteral(position, count);

        /// <summary>
        /// Peeks the next <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<bool> PeekBooleanSpan(int count)
            => GetBooleansInteral(BufferOffset, count);

        /// <summary>
        /// Peeks the next <see cref="Array"/> of <see cref="bool"/> without advancing.
        /// </summary>
        /// <param name="count">The amount to peek.</param>
        /// <returns>An <see cref="Array"/> of <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool[] PeekBooleans(int count)
            => GetBooleansInteral(BufferOffset, count);

        /// <summary>
        /// Reads a <see cref="bool"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the value as.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AssertBoolean(bool option)
            => Assert(ReadBoolean(), nameof(Boolean), option);

        /// <summary>
        /// Reads a <see cref="bool"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the value as.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AssertBoolean(ReadOnlySpan<bool> options)
            => Assert(ReadBoolean(), nameof(Boolean), options);

        #endregion

        #region Enum

        /// <summary>
        /// Assert a value is present in an <see cref="Enum"/> type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="valueFormat">The formatting to use on the value in errors.</param>
        /// <returns>The value.</returns>
        /// <exception cref="InvalidDataException">The value was not present in the <see cref="Enum"/>.</exception>
        private static TEnum AssertEnum<TEnum, TValue>(TValue value, string valueFormat)
            where TEnum : Enum
            where TValue : unmanaged
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new InvalidDataException($"Read value not present in enum: {string.Format(valueFormat, value)}");
            }
            return (TEnum)(object)value;
        }

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="sbyte"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumSByte<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, sbyte>(ReadSByte(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="byte"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumByte<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, byte>(ReadByte(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="short"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumInt16<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, short>(ReadInt16(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="ushort"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumUInt16<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, ushort>(ReadUInt16(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="int"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumInt32<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, int>(ReadInt32(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="uint"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumUInt32<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, uint>(ReadUInt32(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="long"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumInt64<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, long>(ReadInt64(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> using <see cref="ulong"/> as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        public TEnum ReadEnumUInt64<TEnum>() where TEnum : Enum
            => AssertEnum<TEnum, ulong>(ReadUInt64(), "0x{0:X}");

        /// <summary>
        /// Reads an <see cref="Enum"/> automatically detecting the underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        /// <exception cref="InvalidDataException">The underlying type could not be determined.</exception>
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

        /// <summary>
        /// Reads an <see cref="Enum"/> using an 8-bit type as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        /// <exception cref="InvalidDataException">The underlying type could not be determined.</exception>
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

        /// <summary>
        /// Reads an <see cref="Enum"/> using a 16-bit type as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        /// <exception cref="InvalidDataException">The underlying type could not be determined.</exception>
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

        /// <summary>
        /// Reads an <see cref="Enum"/> using a 32-bit type as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        /// <exception cref="InvalidDataException">The underlying type could not be determined.</exception>
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

        /// <summary>
        /// Reads an <see cref="Enum"/> using a 64-bit type as an underlying type.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> type.</typeparam>
        /// <returns>The <see cref="Enum"/> value.</returns>
        /// <exception cref="InvalidDataException">The underlying type could not be determined.</exception>
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

        #endregion

        #region String 8-Bit

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read8BitStringSpan()
        {
            int strLen = StrlenOffset(Buffer, BufferOffset);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += strLen;
            if (strLen < Buffer.Length)
                BufferOffset += 1; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read8BitStringSpan(int length)
        {
            int strLen = StrlenOffsetFixed(Buffer, BufferOffset, length);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += length;
            return slice;
        }

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get8BitStringSpan(int position)
            => Buffer.Slice(position, StrlenOffset(Buffer, position));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get8BitStringSpan(int position, int length)
            => Buffer.Slice(position, StrlenOffsetFixed(Buffer, position, length));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek8BitStringSpan()
            => Buffer.Slice(BufferOffset, StrlenOffset(Buffer, BufferOffset));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing an 8-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek8BitStringSpan(int length)
            => Buffer.Slice(BufferOffset, StrlenOffsetFixed(Buffer, BufferOffset, length));

        #endregion

        #region String 16-Bit

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read16BitStringSpan()
        {
            int strLen = WStrlenOffset(Buffer, BufferOffset);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += strLen;
            if (strLen < Buffer.Length)
                BufferOffset += 2; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read16BitStringSpan(int length)
        {
            int strLen = WStrlenOffsetFixed(Buffer, BufferOffset, length);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += length * 2;
            return slice;
        }

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get16BitStringSpan(int position)
            => Buffer.Slice(position, WStrlenOffset(Buffer, position));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get16BitStringSpan(int position, int length)
            => Buffer.Slice(position, WStrlenOffsetFixed(Buffer, position, length));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek16BitStringSpan()
            => Buffer.Slice(BufferOffset, WStrlenOffset(Buffer, BufferOffset));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 16-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek16BitStringSpan(int length)
            => Buffer.Slice(BufferOffset, WStrlenOffsetFixed(Buffer, BufferOffset, length));

        #endregion

        #region String 32-Bit

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read32BitStringSpan()
        {
            int strLen = DWStrlenOffset(Buffer, BufferOffset);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += strLen;
            if (strLen < Buffer.Length)
                BufferOffset += 4; // Skip terminator

            return slice;
        }

        /// <summary>
        /// Read a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private ReadOnlySpan<byte> Read32BitStringSpan(int length)
        {
            int strLen = DWStrlenOffsetFixed(Buffer, BufferOffset, length);
            var slice = Buffer.Slice(BufferOffset, strLen);

            BufferOffset += length * 4;
            return slice;
        }

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get32BitStringSpan(int position)
            => Buffer.Slice(position, DWStrlenOffset(Buffer, position));

        /// <summary>
        /// Get a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Get32BitStringSpan(int position, int length)
            => Buffer.Slice(position, DWStrlenOffsetFixed(Buffer, position, length));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit null-terminated <see cref="string"/> without advancing.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek32BitStringSpan()
            => Buffer.Slice(BufferOffset, DWStrlenOffset(Buffer, BufferOffset));

        /// <summary>
        /// Peek the next <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> representing a 32-bit fixed-length <see cref="string"/> without advancing.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.</returns>
        private readonly ReadOnlySpan<byte> Peek32BitStringSpan(int length)
            => Buffer.Slice(BufferOffset, DWStrlenOffsetFixed(Buffer, BufferOffset, length));

        #endregion

        #region String ASCII

        /// <summary>
        /// Read a null-terminated ASCII encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadASCII()
            => Encoding.ASCII.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length ASCII encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadASCII(int length)
            => Encoding.ASCII.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated ASCII encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetASCII(int position)
            => Encoding.ASCII.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length ASCII encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetASCII(int position, int length)
            => Encoding.ASCII.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated ASCII encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekASCII()
            => Encoding.ASCII.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length ASCII encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekASCII(int length)
            => Encoding.ASCII.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated ASCII encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertASCII(string option)
            => Assert(ReadASCII(), "ASCII", option);

        /// <summary>
        /// Reads a null-terminated ASCII encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertASCII(ReadOnlySpan<string> options)
            => Assert(ReadASCII(), "ASCII", options);

        /// <summary>
        /// Reads a fixed-length ASCII encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertASCII(int length, string option)
            => Assert(ReadASCII(length), "ASCII", option);

        /// <summary>
        /// Reads a fixed-length ASCII encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertASCII(int length, ReadOnlySpan<string> options)
            => Assert(ReadASCII(length), "ASCII", options);

        #endregion

        #region String UTF8

        /// <summary>
        /// Read a null-terminated UTF8 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF8()
            => Encoding.UTF8.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length UTF8 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF8(int length)
            => Encoding.UTF8.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated UTF8 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF8(int position)
            => Encoding.UTF8.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length UTF8 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF8(int position, int length)
            => Encoding.UTF8.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated UTF8 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF8()
            => Encoding.UTF8.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length UTF8 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF8(int length)
            => Encoding.UTF8.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated UTF8 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF8(string option)
            => Assert(ReadUTF8(), "UTF8", option);

        /// <summary>
        /// Reads a null-terminated UTF8 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF8(ReadOnlySpan<string> options)
            => Assert(ReadUTF8(), "UTF8", options);

        /// <summary>
        /// Reads a fixed-length UTF8 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF8(int length, string option)
            => Assert(ReadUTF8(length), "UTF8", option);

        /// <summary>
        /// Reads a fixed-length UTF8 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF8(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF8(length), "UTF8", options);

        #endregion

        #region String ShiftJIS

        /// <summary>
        /// Read a null-terminated ShiftJIS encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadShiftJIS()
            => EncodingHelper.ShiftJIS.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length ShiftJIS encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadShiftJIS(int length)
            => EncodingHelper.ShiftJIS.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated ShiftJIS encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetShiftJIS(int position)
            => EncodingHelper.ShiftJIS.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length ShiftJIS encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetShiftJIS(int position, int length)
            => EncodingHelper.ShiftJIS.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated ShiftJIS encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekShiftJIS()
            => EncodingHelper.ShiftJIS.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length ShiftJIS encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekShiftJIS(int length)
            => EncodingHelper.ShiftJIS.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated ShiftJIS encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertShiftJIS(string option)
            => Assert(ReadShiftJIS(), "ShiftJIS", option);

        /// <summary>
        /// Reads a null-terminated ShiftJIS encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertShiftJIS(ReadOnlySpan<string> options)
            => Assert(ReadShiftJIS(), "ShiftJIS", options);

        /// <summary>
        /// Reads a fixed-length ShiftJIS encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertShiftJIS(int length, string option)
            => Assert(ReadShiftJIS(length), "ShiftJIS", option);

        /// <summary>
        /// Reads a fixed-length ShiftJIS encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertShiftJIS(int length, ReadOnlySpan<string> options)
            => Assert(ReadShiftJIS(length), "ShiftJIS", options);

        #endregion

        #region String EucJP

        /// <summary>
        /// Read a null-terminated EucJP encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucJP()
            => EncodingHelper.EucJP.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length EucJP encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucJP(int length)
            => EncodingHelper.EucJP.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated EucJP encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucJP(int position)
            => EncodingHelper.EucJP.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length EucJP encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucJP(int position, int length)
            => EncodingHelper.EucJP.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated EucJP encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucJP()
            => EncodingHelper.EucJP.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length EucJP encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucJP(int length)
            => EncodingHelper.EucJP.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated EucJP encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucJP(string option)
            => Assert(ReadEucJP(), "EucJP", option);

        /// <summary>
        /// Reads a null-terminated EucJP encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucJP(ReadOnlySpan<string> options)
            => Assert(ReadEucJP(), "EucJP", options);

        /// <summary>
        /// Reads a fixed-length EucJP encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucJP(int length, string option)
            => Assert(ReadEucJP(length), "EucJP", option);

        /// <summary>
        /// Reads a fixed-length EucJP encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucJP(int length, ReadOnlySpan<string> options)
            => Assert(ReadEucJP(length), "EucJP", options);

        #endregion

        #region String EucCN

        /// <summary>
        /// Read a null-terminated EucCN encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucCN()
            => EncodingHelper.EucCN.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length EucCN encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucCN(int length)
            => EncodingHelper.EucCN.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated EucCN encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucCN(int position)
            => EncodingHelper.EucCN.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length EucCN encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucCN(int position, int length)
            => EncodingHelper.EucCN.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated EucCN encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucCN()
            => EncodingHelper.EucCN.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length EucCN encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucCN(int length)
            => EncodingHelper.EucCN.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated EucCN encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucCN(string option)
            => Assert(ReadEucCN(), "EucCN", option);

        /// <summary>
        /// Reads a null-terminated EucCN encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucCN(ReadOnlySpan<string> options)
            => Assert(ReadEucCN(), "EucCN", options);

        /// <summary>
        /// Reads a fixed-length EucCN encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucCN(int length, string option)
            => Assert(ReadEucCN(length), "EucCN", option);

        /// <summary>
        /// Reads a fixed-length EucCN encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucCN(int length, ReadOnlySpan<string> options)
            => Assert(ReadEucCN(length), "EucCN", options);

        #endregion

        #region String EucKR

        /// <summary>
        /// Read a null-terminated EucKR encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucKR()
            => EncodingHelper.EucKR.GetString(Read8BitStringSpan());

        /// <summary>
        /// Read a fixed-length EucKR encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadEucKR(int length)
            => EncodingHelper.EucKR.GetString(Read8BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated EucKR encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucKR(int position)
            => EncodingHelper.EucKR.GetString(Get8BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length EucKR encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetEucKR(int position, int length)
            => EncodingHelper.EucKR.GetString(Get8BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated EucKR encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucKR()
            => EncodingHelper.EucKR.GetString(Peek8BitStringSpan());

        /// <summary>
        /// Peek a fixed-length EucKR encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekEucKR(int length)
            => EncodingHelper.EucKR.GetString(Peek8BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated EucKR encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucKR(string option)
            => Assert(ReadEucKR(), "EucKR", option);

        /// <summary>
        /// Reads a null-terminated EucKR encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucKR(ReadOnlySpan<string> options)
            => Assert(ReadEucKR(), "EucKR", options);

        /// <summary>
        /// Reads a fixed-length EucKR encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertEucKR(int length, string option)
            => Assert(ReadEucKR(length), "EucKR", option);

        /// <summary>
        /// Reads a fixed-length EucKR encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertEucKR(int length, ReadOnlySpan<string> options)
            => Assert(ReadEucKR(length), "EucKR", options);

        #endregion

        #region String UTF16

        /// <summary>
        /// Read a null-terminated UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16()
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Read16BitStringSpan())
            : EncodingHelper.UTF16LE.GetString(Read16BitStringSpan());

        /// <summary>
        /// Read a fixed-length UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16(int length)
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Read16BitStringSpan(length))
            : EncodingHelper.UTF16LE.GetString(Read16BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16(int position)
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(position))
            : EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16(int position, int length)
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(position, length))
            : EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16()
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan())
            : EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan());

        /// <summary>
        /// Peek a fixed-length UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16(int length)
            => BigEndian
            ? EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan(length))
            : EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16(string option)
            => Assert(ReadUTF16(), "UTF16", option);

        /// <summary>
        /// Reads a null-terminated UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16(ReadOnlySpan<string> options)
            => Assert(ReadUTF16(), "UTF16", options);

        /// <summary>
        /// Reads a fixed-length UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16(int length, string option)
            => Assert(ReadUTF16(length), "UTF16", option);

        /// <summary>
        /// Reads a fixed-length UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF16(length), "UTF16", options);

        #endregion

        #region String UTF16 Big Endian

        /// <summary>
        /// Read a null-terminated big-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16BigEndian()
            => EncodingHelper.UTF16BE.GetString(Read16BitStringSpan());

        /// <summary>
        /// Read a fixed-length big-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16BigEndian(int length)
            => EncodingHelper.UTF16BE.GetString(Read16BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated big-endian UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16BigEndian(int position)
            => EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length big-endian UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16BigEndian(int position, int length)
            => EncodingHelper.UTF16BE.GetString(Get16BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated big-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16BigEndian()
            => EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan());

        /// <summary>
        /// Peek a fixed-length big-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16BigEndian(int length)
            => EncodingHelper.UTF16BE.GetString(Peek16BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated big-endian UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16BigEndian(string option)
            => Assert(ReadUTF16BigEndian(), "UTF16BE", option);

        /// <summary>
        /// Reads a null-terminated big-endian UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16BigEndian(ReadOnlySpan<string> options)
            => Assert(ReadUTF16BigEndian(), "UTF16BE", options);

        /// <summary>
        /// Reads a fixed-length big-endian UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16BigEndian(int length, string option)
            => Assert(ReadUTF16BigEndian(length), "UTF16BE", option);

        /// <summary>
        /// Reads a fixed-length big-endian UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16BigEndian(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF16BigEndian(length), "UTF16BE", options);

        #endregion

        #region String UTF16 Little Endian

        /// <summary>
        /// Read a null-terminated little-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16LittleEndian()
            => EncodingHelper.UTF16LE.GetString(Read16BitStringSpan());

        /// <summary>
        /// Read a fixed-length little-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF16LittleEndian(int length)
            => EncodingHelper.UTF16LE.GetString(Read16BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated little-endian UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16LittleEndian(int position)
            => EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length little-endian UTF16 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF16LittleEndian(int position, int length)
            => EncodingHelper.UTF16LE.GetString(Get16BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated little-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16LittleEndian()
            => EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan());

        /// <summary>
        /// Peek a fixed-length little-endian UTF16 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF16LittleEndian(int length)
            => EncodingHelper.UTF16LE.GetString(Peek16BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated little-endian UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16LittleEndian(string option)
            => Assert(ReadUTF16LittleEndian(), "UTF16LE", option);

        /// <summary>
        /// Reads a null-terminated little-endian UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16LittleEndian(ReadOnlySpan<string> options)
            => Assert(ReadUTF16LittleEndian(), "UTF16LE", options);

        /// <summary>
        /// Reads a fixed-length little-endian UTF16 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF16LittleEndian(int length, string option)
            => Assert(ReadUTF16LittleEndian(length), "UTF16LE", option);

        /// <summary>
        /// Reads a fixed-length little-endian UTF16 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 16-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF16LittleEndian(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF16LittleEndian(length), "UTF16LE", options);

        #endregion

        #region String UTF32

        /// <summary>
        /// Read a null-terminated UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32()
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Read32BitStringSpan())
            : EncodingHelper.UTF32LE.GetString(Read32BitStringSpan());

        /// <summary>
        /// Read a fixed-length UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32(int length)
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Read32BitStringSpan(length))
            : EncodingHelper.UTF32LE.GetString(Read32BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32(int position)
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(position))
            : EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32(int position, int length)
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(position, length))
            : EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32()
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan())
            : EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan());

        /// <summary>
        /// Peek a fixed-length UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32(int length)
            => BigEndian
            ? EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan(length))
            : EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32(string option)
            => Assert(ReadUTF32(), "UTF32", option);

        /// <summary>
        /// Reads a null-terminated UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32(ReadOnlySpan<string> options)
            => Assert(ReadUTF32(), "UTF32", options);

        /// <summary>
        /// Reads a fixed-length UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32(int length, string option)
            => Assert(ReadUTF32(length), "UTF32", option);

        /// <summary>
        /// Reads a fixed-length UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF32(length), "UTF32", options);

        #endregion

        #region String UTF32 Big Endian

        /// <summary>
        /// Read a null-terminated big-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32BigEndian()
            => EncodingHelper.UTF32BE.GetString(Read32BitStringSpan());

        /// <summary>
        /// Read a fixed-length big-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32BigEndian(int length)
            => EncodingHelper.UTF32BE.GetString(Read32BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated big-endian UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32BigEndian(int position)
            => EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length big-endian UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32BigEndian(int position, int length)
            => EncodingHelper.UTF32BE.GetString(Get32BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated big-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32BigEndian()
            => EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan());

        /// <summary>
        /// Peek a fixed-length big-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32BigEndian(int length)
            => EncodingHelper.UTF32BE.GetString(Peek32BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated big-endian UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32BigEndian(string option)
            => Assert(ReadUTF32BigEndian(), "UTF32BE", option);

        /// <summary>
        /// Reads a null-terminated big-endian UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32BigEndian(ReadOnlySpan<string> options)
            => Assert(ReadUTF32BigEndian(), "UTF32BE", options);

        /// <summary>
        /// Reads a fixed-length big-endian UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32BigEndian(int length, string option)
            => Assert(ReadUTF32BigEndian(length), "UTF32BE", option);

        /// <summary>
        /// Reads a fixed-length big-endian UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32BigEndian(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF32BigEndian(length), "UTF32BE", options);

        #endregion

        #region String UTF32 Little Endian

        /// <summary>
        /// Read a null-terminated little-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32LittleEndian()
            => EncodingHelper.UTF32LE.GetString(Read32BitStringSpan());

        /// <summary>
        /// Read a fixed-length little-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string ReadUTF32LittleEndian(int length)
            => EncodingHelper.UTF32LE.GetString(Read32BitStringSpan(length));

        /// <summary>
        /// Get a null-terminated little-endian UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32LittleEndian(int position)
            => EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(position));

        /// <summary>
        /// Get a fixed-length little-endian UTF32 encoded <see cref="string"/> at the specified position.
        /// </summary>
        /// <param name="position">The specified position.</param>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string GetUTF32LittleEndian(int position, int length)
            => EncodingHelper.UTF32LE.GetString(Get32BitStringSpan(position, length));

        /// <summary>
        /// Peek a null-terminated little-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32LittleEndian()
            => EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan());

        /// <summary>
        /// Peek a fixed-length little-endian UTF32 encoded <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public readonly string PeekUTF32LittleEndian(int length)
            => EncodingHelper.UTF32LE.GetString(Peek32BitStringSpan(length));

        /// <summary>
        /// Reads a null-terminated little-endian UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32LittleEndian(string option)
            => Assert(ReadUTF32LittleEndian(), "UTF32LE", option);

        /// <summary>
        /// Reads a null-terminated little-endian UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32LittleEndian(ReadOnlySpan<string> options)
            => Assert(ReadUTF32LittleEndian(), "UTF32LE", options);

        /// <summary>
        /// Reads a fixed-length little-endian UTF32 encoded <see cref="string"/> and throws if it is not the specified option.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="option">The option to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public string AssertUTF32LittleEndian(int length, string option)
            => Assert(ReadUTF32LittleEndian(length), "UTF32LE", option);

        /// <summary>
        /// Reads a fixed-length little-endian UTF32 encoded <see cref="string"/> and throws if it is not one of the specified options.
        /// </summary>
        /// <param name="length">The length of the fixed field in 32-bit chars.</param>
        /// <param name="options">The options to assert the <see cref="string"/> as.</param>
        /// <returns>A <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AssertUTF32LittleEndian(int length, ReadOnlySpan<string> options)
            => Assert(ReadUTF32LittleEndian(length), "UTF32LE", options);

        #endregion

    }
}
