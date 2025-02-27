using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A stream that reads and writes bytes on aligned sectors.
    /// </summary>
    public class SectorStream : Stream, IDisposable, IAsyncDisposable
    {
        #region Members

        /// <summary>
        /// The underlying <see cref="Stream"/>.
        /// </summary>
        public Stream BaseStream { get; set; }

        /// <summary>
        /// The size of each sector.
        /// </summary>
        public int SectorSize { get; set; }

        /// <summary>
        /// The byte value of padding between sectors.
        /// </summary>
        public byte Padding { get; set; }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        /// <summary>
        /// Gets the current sector length in the stream.
        /// </summary>
        public override long Length => (BaseStream.Length - 1) / SectorSize + 1;

        /// <summary>
        /// Gets or sets the current sector position in the stream.
        /// </summary>
        public override long Position
        {
            get => BaseStream.Position / SectorSize;
            set => BaseStream.Position = value * SectorSize;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new expandable <see cref="SectorStream"/> and default sector size.
        /// </summary>
        public SectorStream() : this(new MemoryStream()) { }

        /// <summary>
        /// Create a new expandable <see cref="SectorStream"/> with the given sector size.
        /// </summary>
        /// <param name="sectorSize">The size of each sector.</param>
        public SectorStream(int sectorSize) : this(new MemoryStream(), sectorSize) { }

        /// <summary>
        /// Create a <see cref="SectorStream"/> over the given <see cref="Stream"/> with the default sector size.
        /// </summary>
        /// <param name="baseStream">The <see cref="Stream"/> to create a <see cref="SectorStream"/> over.</param>
        public SectorStream(Stream baseStream) : this(baseStream, 0x800) { }

        /// <summary>
        /// Create a <see cref="SectorStream"/> over the given <see cref="Stream"/> with the given sector size.
        /// </summary>
        /// <param name="baseStream">The <see cref="Stream"/> to create a <see cref="SectorStream"/> over.</param>
        /// <param name="sectorSize">The size of each sector.</param>
        public SectorStream(Stream baseStream, int sectorSize)
        {
            BaseStream = baseStream;
            SectorSize = sectorSize;
            Padding = 0;
        }

        #endregion

        #region Methods

        public override void Flush()
            => BaseStream.Flush();

        /// <summary>
        /// Reads the specified number of bytes into the specified buffer at the specified offset unaligned.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The byte offset to read into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The total number of read bytes.</returns>
        public override int Read(byte[] buffer, int offset, int count)
            => BaseStream.Read(buffer, offset, count);

        /// <summary>
        /// Reads into the specified span of bytes unaligned.
        /// </summary>
        /// <param name="bytes">The bytes to read into.</param>
        /// <returns>The total number of read bytes.</returns>
        public override int Read(Span<byte> bytes)
            => BaseStream.Read(bytes);

        /// <summary>
        /// Reads the specified number of bytes into the specified buffer at the specified offset aligned.<br/>
        /// Do not use for reading bit-by-bit, only for entire sectors at a time.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The byte offset to read into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The total number of read bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadAligned(byte[] buffer, int offset, int count)
        {
            AlignSector();
            return BaseStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads into the specified span of bytes aligned.<br/>
        /// Do not use for reading bit-by-bit, only for entire sectors at a time.
        /// </summary>
        /// <param name="bytes">The span of bytes to read into.</param>
        /// <returns>The total number of read bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadAligned(Span<byte> bytes)
        {
            AlignSector();
            return BaseStream.Read(bytes);
        }

        /// <summary>
        /// Aligns the stream, then reads the specified number of sectors into the specified buffer at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The byte offset to read into the buffer.</param>
        /// <param name="sectorCount">The number of sectors to read.</param>
        /// <returns>The total number of read sectors.</returns>
        public int ReadSectors(byte[] buffer, int offset, int sectorCount)
        {
            AlignSector();
            int read = BaseStream.Read(buffer, offset, sectorCount * SectorSize);
            return (read - 1) / SectorSize + 1;
        }

        /// <summary>
        /// Seek to the given sector offset.
        /// </summary>
        /// <param name="sectorOffset">The desired sector offset.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new sector position within the stream.</returns>
        public override long Seek(long sectorOffset, SeekOrigin origin)
        {
            BaseStream.Seek(sectorOffset * SectorSize, origin);
            return Position;
        }

        /// <summary>
        /// Seek to the given offset.
        /// </summary>
        /// <param name="offset">The desired offset.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long SeekUnaligned(long offset, SeekOrigin origin)
            => BaseStream.Seek(offset * SectorSize, origin);

        public override void SetLength(long value)
            => BaseStream.SetLength(value * SectorSize);

        /// <summary>
        /// Writes the specified number of bytes at the specified offset in the buffer into the stream unaligned.
        /// </summary>
        /// <param name="buffer">The buffer to write into the stream.</param>
        /// <param name="offset">The offset in the buffer to write from.</param>
        /// <param name="count">The number of bytes in the buffer to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
            => BaseStream.Write(buffer, offset, count);

        /// <summary>
        /// Writes the specified bytes into the stream unaligned.
        /// </summary>
        /// <param name="bytes">The bytes to write into the stream.</param>
        public override void Write(ReadOnlySpan<byte> bytes)
            => BaseStream.Write(bytes);

        /// <summary>
        /// Writes the specified number of bytes at the specified offset in the buffer into the stream aligned.<br/>
        /// Do not use for writing bit-by-bit, only for entire sectors at a time.
        /// </summary>
        /// <param name="buffer">The buffer to write into the stream.</param>
        /// <param name="offset">The offset in the buffer to write from.</param>
        /// <param name="count">The number of bytes in the buffer to write.</param>
        public void WriteAligned(byte[] buffer, int offset, int count)
        {
            PadSector();
            BaseStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes the specified bytes into the stream aligned.<br/>
        /// Do not use for writing bit-by-bit, only for entire sectors at a time.
        /// </summary>
        /// <param name="bytes">The bytes to write into the stream.</param>
        public void WriteAligned(ReadOnlySpan<byte> bytes)
        {
            PadSector();
            BaseStream.Write(bytes);
        }

        /// <summary>
        /// Aligns the stream, then writes the specified number of sectors at the specified offset in the buffer into the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write into the stream.</param>
        /// <param name="offset">The offset in the buffer to write from.</param>
        /// <param name="sectorCount">The number of sectors in the buffer to write.</param>
        public void WriteSectors(byte[] buffer, int offset, int sectorCount)
        {
            PadSector();
            BaseStream.Write(buffer, offset, sectorCount * SectorSize);
        }

        /// <summary>
        /// Align to the sector size.<br/>
        /// Only changes position, does not pad to sector size.<br/>
        /// To pad to sector size see: <see cref="PadSector"/>.
        /// </summary>
        public void AlignSector()
        {
            long remainder = BaseStream.Position % SectorSize;
            BaseStream.Position = remainder + (SectorSize - remainder);
        }

        /// <summary>
        /// Align to the sector size, writing padding if necessary.
        /// </summary>
        public void PadSector()
        {
            // Get the next sector position from the current position
            long remainder = BaseStream.Position % SectorSize;
            int remaining = (int)(SectorSize - remainder);
            long nextPosition = BaseStream.Position + remaining;

            // If the next sector position is longer than the current length, pad out the stream.
            if (BaseStream.Length <= nextPosition)
            {
                byte[] padding = new byte[remaining];
                if (Padding != 0)
                {
                    for (int i = 0; i < remaining; i++)
                    {
                        padding[i] = Padding;
                    }
                }

                BaseStream.Write(padding, 0, remaining);
            }

            // Set the position of the stream to the next sector position.
            BaseStream.Position = nextPosition;
        }

        #endregion
    }
}
