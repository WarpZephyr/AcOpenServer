using System;
using System.IO;

namespace BinaryMemory.IO
{
    /// <summary>
    /// A <see cref="Stream"/> going over part of another <see cref="Stream"/>.
    /// </summary>
    public class SubStream : Stream, IDisposable, IAsyncDisposable
    {
        #region Members

        /// <summary>
        /// The underlying <see cref="Stream"/>.
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// The offset into the <see cref="BaseStream"/> this <see cref="SubStream"/> begins at.
        /// </summary>
        private long _offset;

        /// <summary>
        /// The length of this <see cref="SubStream"/>.
        /// </summary>
        private long _length;

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => _length;

        public override long Position
        {
            get => BaseStream.Position - _offset;
            set
            {
                long newPosition = _offset + value;

                if (newPosition < _offset)
                    throw new IOException("Cannot seek to before the start of the stream.");
                if (newPosition > _offset + _length)
                    throw new IOException("Cannot seek beyond the end of the stream.");

                BaseStream.Position = newPosition;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="SubStream"/> from the specified <see cref="Stream"/>, offset, and length.
        /// </summary>
        /// <param name="baseStream">The underlying <see cref="Stream"/>.</param>
        public SubStream(Stream baseStream, long offset, long length)
        {
            ArgumentNullException.ThrowIfNull(baseStream, nameof(baseStream));
            ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, baseStream.Length, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + length, baseStream.Length, nameof(length));

            BaseStream = baseStream;
            _offset = offset;
            _length = length;

            if (baseStream.CanSeek)
            {
                baseStream.Seek(offset, SeekOrigin.Begin);
            }
            else if (baseStream.CanRead)
            {
                // Seek it manually with read...
                const int BUFFER_SIZE = 4096;
                byte[] buffer = new byte[BUFFER_SIZE];
                long chunkCount = offset / BUFFER_SIZE;
                int remainder = (int)(offset % BUFFER_SIZE);

                for (int i = 0; i < chunkCount; i++)
                {
                    baseStream.Read(buffer, 0, BUFFER_SIZE);
                }

                if (remainder > 0)
                {
                    baseStream.Read(buffer, 0, remainder);
                }
            }
            else
            {
                throw new IOException("Cannot seek to stream start offset.");
            }
        }

        #endregion

        #region Methods

        public override void Flush()
            => BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Not sure if this is the best implementation...
            long possible = _length - Position;
            if (count > possible)
                count = (int)possible;

            return BaseStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            // Not sure if this is the best implementation...
            int possible = (int)(_length - Position);
            if (buffer.Length > possible)
            {
                byte[] bytes = new byte[possible];
                int read = BaseStream.Read(bytes, 0, possible);
                bytes.CopyTo(buffer);
                return read;
            }

            return BaseStream.Read(buffer);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = origin switch
            {
                SeekOrigin.Begin => unchecked(_offset + offset),
                SeekOrigin.Current => unchecked(BaseStream.Position + offset),
                SeekOrigin.End => unchecked(_offset + _length + offset),
                _ => throw new IOException("Invalid seek origin."),
            };

            if (newPosition < _offset)
                throw new IOException("Cannot seek to before the start of the stream.");
            if (newPosition > _offset + _length)
                throw new IOException("Cannot seek beyond the end of the stream.");

            BaseStream.Seek(newPosition, origin);
            return Position;
        }

        /// <summary>
        /// Sets where this <see cref="SubStream"/> begins in the underlying <see cref="BaseStream"/>.
        /// </summary>
        /// <param name="offset">The offset to begin at.</param>
        public void SetOrigin(long offset)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, BaseStream.Length, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + _length, BaseStream.Length, nameof(offset));
            _offset = offset;
        }

        public override void SetLength(long value)
        {
            long baseLength = _offset + value;
            if (baseLength > BaseStream.Length)
            {
                BaseStream.SetLength(baseLength);
            }

            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(unchecked(BaseStream.Position + count), unchecked(_offset + _length), nameof(count));
            BaseStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(unchecked(BaseStream.Position + buffer.Length), unchecked(_offset + _length), nameof(buffer));
            BaseStream.Write(buffer);
        }

        #endregion
    }
}
