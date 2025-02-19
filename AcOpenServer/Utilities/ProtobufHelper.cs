using Google.Protobuf;
using System;
using System.Diagnostics.CodeAnalysis;

namespace AcOpenServer.Utilities
{
    internal static class ProtobufHelper
    {
        public static T ParseFrom<T>(byte[] payload) where T : IMessage, new()
        {
            T result = new();
            result.MergeFrom(payload);
            return result;
        }

        public static bool TryParse<T>(byte[] payload, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out string? error) where T : IMessage, new()
        {
            try
            {
                result = ParseFrom<T>(payload);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                result = default;
                error = ex.Message;
                return false;
            }
        }
    }
}
