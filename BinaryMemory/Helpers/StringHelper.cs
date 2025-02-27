using System;

namespace BinaryMemory.Helpers
{
    internal static class StringHelper
    {
        #region Strlen

        public static int Strlen(ReadOnlySpan<byte> buffer)
        {
            // Zero length buffers should pass as we check for less than in the loop
            int i = 0;
            for (; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    i -= 1; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        public static int WStrlen(ReadOnlySpan<byte> buffer)
        {
            // Zero length buffers should pass as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than 0
            int i = 0;
            int runLength = buffer.Length % 2 - 1;
            for (; i < runLength; i += 2)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0)
                {
                    i = (i + 1) - 2; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        public static int DWStrlen(ReadOnlySpan<byte> buffer)
        {
            // Zero length buffers should pass as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than 0
            int i = 0;
            int runLength = buffer.Length % 4 - 3;
            for (; i < runLength; i += 4)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0 && buffer[i + 2] == 0 && buffer[i + 3] == 0)
                {
                    i = (i + 3) - 4; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        #endregion

        #region StrlenFixed

        public static int StrlenFixed(ReadOnlySpan<byte> buffer, int limit)
        {
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {limit} < {0}");
            }

            if (limit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {limit} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            int i = 0;
            for (; i < limit; i++)
            {
                if (buffer[i] == 0)
                {
                    i -= 1; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        public static int WStrlenFixed(ReadOnlySpan<byte> buffer, int limit)
        {
            int trueLimit = limit * 2;
            if (trueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {trueLimit} < {0}");
            }

            if (trueLimit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {trueLimit} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than 0
            int i = 0;
            int runLength = trueLimit - 1;
            for (; i < runLength; i += 2)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0)
                {
                    i = (i + 1) - 2; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        public static int DWStrlenFixed(ReadOnlySpan<byte> buffer, int limit)
        {
            int trueLimit = limit * 4;
            if (trueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {trueLimit} < {0}");
            }

            if (trueLimit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {trueLimit} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than 0
            int i = 0;
            int runLength = trueLimit - 3;
            for (; i < runLength; i += 4)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0 && buffer[i + 2] == 0 && buffer[i + 3] == 0)
                {
                    i = (i + 3) - 4; // Ignore terminator in length
                    break;
                }
            }

            return i;
        }

        #endregion

        #region StrlenOffset

        public static int StrlenOffset(ReadOnlySpan<byte> buffer, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than buffer length: {offset} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            int i = 0;
            int offsetIndex = offset;
            for (; offsetIndex < buffer.Length; i++)
            {
                if (buffer[offsetIndex] == 0)
                {
                    i -= 1; // Ignore terminator in length
                    break;
                }

                offsetIndex++;
            }

            return i;
        }

        public static int WStrlenOffset(ReadOnlySpan<byte> buffer, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than buffer length: {offset} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than the offset
            // We already confirm the offset is positive
            int i = 0;
            int offsetIndex = offset;
            for (; offsetIndex < buffer.Length - 1; i += 2)
            {
                if (buffer[offsetIndex] == 0 && buffer[offsetIndex + 1] == 0)
                {
                    i = (i + 1) - 2; // Ignore terminator in length
                    break;
                }

                offsetIndex += 2;
            }

            return i;
        }

        public static int DWStrlenOffset(ReadOnlySpan<byte> buffer, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than buffer length: {offset} > {buffer.Length}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than the offset
            // We already confirm the offset is positive
            int i = 0;
            int offsetIndex = offset;
            for (; offsetIndex < buffer.Length - 3; i += 4)
            {
                if (buffer[offsetIndex] == 0 && buffer[offsetIndex + 1] == 0 && buffer[offsetIndex + 2] == 0 && buffer[offsetIndex + 3] == 0)
                {
                    i = (i + 3) - 4; // Ignore terminator in length
                    break;
                }

                offsetIndex += 4;
            }

            return i;
        }

        #endregion

        #region StrlenOffsetFixed

        public static int StrlenOffsetFixed(ReadOnlySpan<byte> buffer, int offset, int limit)
        {
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {limit} < {0}");
            }

            if (limit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {limit} > {buffer.Length}");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > limit)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than length limit: {offset} > {limit}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            int i = 0;
            int offsetIndex = offset;
            for (; offsetIndex < limit; i++)
            {
                if (buffer[offsetIndex] == 0)
                {
                    i -= 1; // Ignore terminator in length
                    break;
                }

                offsetIndex++;
            }

            return i;
        }

        public static int WStrlenOffsetFixed(ReadOnlySpan<byte> buffer, int offset, int limit)
        {
            int trueLimit = limit * 2;
            if (trueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {trueLimit} < {0}");
            }

            if (trueLimit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {trueLimit} > {buffer.Length}");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > trueLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than length limit: {offset} > {trueLimit}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than the offset
            // We already confirm the offset is positive
            int i = 0;
            int offsetIndex = offset;
            int runLength = trueLimit - 1;
            for (; offsetIndex < runLength; i += 2)
            {
                if (buffer[offsetIndex] == 0 && buffer[offsetIndex + 1] == 0)
                {
                    i = (i + 1) - 2; // Ignore terminator in length
                    break;
                }

                offsetIndex += 2;
            }

            return i;
        }

        public static int DWStrlenOffsetFixed(ReadOnlySpan<byte> buffer, int offset, int limit)
        {
            int trueLimit = limit * 4;
            if (trueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit must be positive: {trueLimit} < {0}");
            }

            if (trueLimit > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), $"Length limit is greater than buffer length: {trueLimit} > {buffer.Length}");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must be positive: {offset} < 0");
            }

            if (offset > trueLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset must not be greater than length limit: {offset} > {trueLimit}");
            }

            // Zero length buffers should pass as we check for less than in the loop
            // An offset equal to the buffer length should return 0 anyways as we check for less than in the loop
            // Negative buffer length from the subtraction should pass anyways as it will be less than the offset
            // We already confirm the offset is positive
            int i = 0;
            int offsetIndex = offset;
            int runLength = trueLimit - 3;
            for (; offsetIndex < runLength; i += 4)
            {
                if (buffer[offsetIndex] == 0 && buffer[offsetIndex + 1] == 0 && buffer[offsetIndex + 2] == 0 && buffer[offsetIndex + 3] == 0)
                {
                    i = (i + 3) - 4; // Ignore terminator in length
                    break;
                }

                offsetIndex += 4;
            }

            return i;
        }

        #endregion
    }
}
