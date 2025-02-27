using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BinaryMemory.Helpers
{
    internal static class ArrayHelper
    {
        #region ReadOnlySpan GetEndianReversedCopy

        public static ReadOnlySpan<T> GetEndianReversedCopy<T>(this ReadOnlySpan<T> span, Func<T, T> reverse)
        {
            int count = span.Length;
            var array = new T[count];
            for (int i = 0; i < count; i++)
                array[i] = reverse(span[i]);
            return new ReadOnlySpan<T>(array);
        }

        public static ReadOnlySpan<short> GetEndianReversedCopy(this ReadOnlySpan<short> span)
        {
            int count = span.Length;
            var array = new short[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<short>(array);
        }

        public static ReadOnlySpan<ushort> GetEndianReversedCopy(this ReadOnlySpan<ushort> span)
        {
            int count = span.Length;
            var array = new ushort[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<ushort>(array);
        }

        public static ReadOnlySpan<int> GetEndianReversedCopy(this ReadOnlySpan<int> span)
        {
            int count = span.Length;
            var array = new int[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<int>(array);
        }

        public static ReadOnlySpan<uint> GetEndianReversedCopy(this ReadOnlySpan<uint> span)
        {
            int count = span.Length;
            var array = new uint[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<uint>(array);
        }

        public static ReadOnlySpan<long> GetEndianReversedCopy(this ReadOnlySpan<long> span)
        {
            int count = span.Length;
            var array = new long[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<long>(array);
        }

        public static ReadOnlySpan<ulong> GetEndianReversedCopy(this ReadOnlySpan<ulong> span)
        {
            int count = span.Length;
            var array = new ulong[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return new ReadOnlySpan<ulong>(array);
        }

        public static ReadOnlySpan<Half> GetEndianReversedCopy(this ReadOnlySpan<Half> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<Half, ushort>(span);
            var array = new ushort[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);
            return MemoryMarshal.Cast<ushort, Half>(new ReadOnlySpan<ushort>(array));
        }

        public static ReadOnlySpan<float> GetEndianReversedCopy(this ReadOnlySpan<float> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<float, uint>(span);
            var array = new uint[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);
            return MemoryMarshal.Cast<uint, float>(new ReadOnlySpan<uint>(array));
        }

        public static ReadOnlySpan<double> GetEndianReversedCopy(this ReadOnlySpan<double> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<double, ulong>(span);
            var array = new ulong[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);
            return MemoryMarshal.Cast<ulong, double>(new ReadOnlySpan<ulong>(array));
        }

        #endregion

        #region ReadOnlySpan ToEndianReversedArray

        public static T[] ToEndianReversedArray<T>(this ReadOnlySpan<T> span, Func<T, T> reverse)
        {
            int count = span.Length;
            var array = new T[count];
            for (int i = 0; i < count; i++)
                array[i] = reverse(span[i]);
            return array;
        }

        public static short[] ToEndianReversedArray(this ReadOnlySpan<short> span)
        {
            int count = span.Length;
            var array = new short[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static ushort[] ToEndianReversedArray(this ReadOnlySpan<ushort> span)
        {
            int count = span.Length;
            var array = new ushort[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static int[] ToEndianReversedArray(this ReadOnlySpan<int> span)
        {
            int count = span.Length;
            var array = new int[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static uint[] ToEndianReversedArray(this ReadOnlySpan<uint> span)
        {
            int count = span.Length;
            var array = new uint[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static long[] ToEndianReversedArray(this ReadOnlySpan<long> span)
        {
            int count = span.Length;
            var array = new long[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static ulong[] ToEndianReversedArray(this ReadOnlySpan<ulong> span)
        {
            int count = span.Length;
            var array = new ulong[count];
            for (int i = 0; i < count; i++)
                array[i] = BinaryPrimitives.ReverseEndianness(span[i]);
            return array;
        }

        public static Half[] ToEndianReversedArray(this ReadOnlySpan<Half> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<Half, ushort>(span);
            var array = new Half[count];
            var castCopySpan = MemoryMarshal.Cast<Half, ushort>(new Span<Half>(array));
            for (int i = 0; i < count; i++)
                castCopySpan[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);

            return array;
        }

        public static float[] ToEndianReversedArray(this ReadOnlySpan<float> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<float, uint>(span);
            var array = new float[count];
            var castCopySpan = MemoryMarshal.Cast<float, uint>(new Span<float>(array));
            for (int i = 0; i < count; i++)
                castCopySpan[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);

            return array;
        }

        public static double[] ToEndianReversedArray(this ReadOnlySpan<double> span)
        {
            int count = span.Length;
            var castSpan = MemoryMarshal.Cast<double, ulong>(span);
            var array = new double[count];
            var castCopySpan = MemoryMarshal.Cast<double, ulong>(new Span<double>(array));
            for (int i = 0; i < count; i++)
                castCopySpan[i] = BinaryPrimitives.ReverseEndianness(castSpan[i]);

            return array;
        }

        #endregion
    }
}
