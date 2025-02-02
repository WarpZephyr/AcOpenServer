using Google.Protobuf;

namespace AcOpenServer.Core.Utilities
{
    internal static class ProtobufHelper
    {
        public static T ParseFrom<T>(byte[] payload) where T : IMessage, new()
        {
            T result = new();
            result.MergeFrom(payload);
            return result;
        }
    }
}
