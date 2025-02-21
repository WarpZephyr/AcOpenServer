using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Utilities
{
    internal static class IPAddressHelper
    {
        public static bool TryGetIPV4UInt32(IPAddress address, [NotNullWhen(true)] out uint value)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                value = default;
                return false;
            }

            value = ByteHelper.ToUInt32(address.GetAddressBytes());
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrivateLocalTcpClient(TcpClient client)
            => IsPrivateLocalSocket(client.Client);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrivateRemoteTcpClient(TcpClient client)
            => IsPrivateRemoteSocket(client.Client);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrivateLocalSocket(Socket socket)
            => socket.LocalEndPoint != null
            && IsPrivateEndPoint(socket.LocalEndPoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrivateRemoteSocket(Socket socket)
            => socket.RemoteEndPoint != null
            && IsPrivateEndPoint(socket.RemoteEndPoint);

        public static bool IsPrivateEndPoint(EndPoint endPoint)
        {
            if (endPoint is IPEndPoint ipEndPoint)
            {
                return IsPrivateAddress(ipEndPoint.Address);
            }

            // TODO
            return false;
        }

        public static bool IsPrivateAddress(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = address.GetAddressBytes();
                return b[0] == 127                               // 127.*.*.*
                    || b[0] == 10                                // 10.*.*.*
                    || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) // 172.16.*.* -> 172.31.*.*
                    || (b[0] == 192 && b[1] == 168);             // 192.168.*.*
            }
            
            // TODO
            return false;
        }
    }
}
