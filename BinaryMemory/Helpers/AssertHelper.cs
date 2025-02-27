using System;
using System.IO;
using System.Linq;

namespace BinaryMemory.Helpers
{
    internal static class AssertHelper
    {
        public static T Assert<T>(T value, string typeName, string valueFormat, ReadOnlySpan<T> options) where T : IEquatable<T>
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
            throw new InvalidDataException($"Assertion failed for {typeName}: {strValue} | Expected: {strOptions}");
        }

        public static T Assert<T>(T value, string typeName, string valueFormat, T option) where T : IEquatable<T>
        {
            if (value.Equals(option))
            {
                return value;
            }

            string strValue = string.Format(valueFormat, value);
            string strOption = string.Format(valueFormat, option);
            throw new InvalidDataException($"Assertion failed for {typeName}: {strValue} | Expected: {strOption}");
        }

        public static T Assert<T>(T value, string typeName, ReadOnlySpan<T> options) where T : IEquatable<T>
        {
            foreach (T option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strOptions = string.Join(", ", options.ToArray());
            throw new InvalidDataException($"Assertion failed for {typeName}: {value} | Expected: {strOptions}");
        }

        public static string Assert(string value, string encodingName, ReadOnlySpan<string> options)
        {
            foreach (string option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strOptions = string.Join(", ", options.ToArray());
            throw new InvalidDataException($"Assertion failed for {encodingName} string: {value} | Expected: {strOptions}");
        }

        public static T Assert<T>(T value, string typeName, T option) where T : IEquatable<T>
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Assertion failed for {typeName}: {value} | Expected: {option}");
        }

        public static string Assert(string value, string encodingName, string option)
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Assertion failed for {encodingName} string: {value} | Expected: {option}");
        }

        public static T Assert<T>(T value, ReadOnlySpan<T> options) where T : IEquatable<T>
        {
            foreach (T option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strOptions = string.Join(", ", options.ToArray());
            throw new InvalidDataException($"Assertion failed for value: {value} | Expected: {strOptions}");
        }

        public static string Assert(string value, ReadOnlySpan<string> options)
        {
            foreach (string option in options)
            {
                if (value.Equals(option))
                {
                    return value;
                }
            }

            string strOptions = string.Join(", ", options.ToArray());
            throw new InvalidDataException($"Assertion failed for string: {value} | Expected: {strOptions}");
        }

        public static T Assert<T>(T value, T option) where T : IEquatable<T>
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Assertion failed for value: {value} | Expected: {option}");
        }

        public static string Assert(string value, string option)
        {
            if (value.Equals(option))
            {
                return value;
            }

            throw new InvalidDataException($"Assertion failed for string: {value} | Expected: {option}");
        }
    }
}
