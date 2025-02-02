namespace AcOpenServer.Network.Clients
{
    /// <summary>
    /// The state of a client being authenticated.
    /// </summary>
    public enum AuthClientState
    {
        /// <summary>
        /// The server is waiting on a handshake request from the client.
        /// </summary>
        WaitingForHandshakeRequest,

        /// <summary>
        /// The server is waiting on a service status request from the client.
        /// </summary>
        WaitingForServiceStatusRequest,

        /// <summary>
        /// Client authentication is complete.
        /// </summary>
        Complete
    }
}
