namespace AcOpenServer.Network.Communication.Fsdp
{
    /// <summary>
    /// The different packet transmission states for an FSDP stream.
    /// </summary>
    public enum FsdpStreamState 
    {
        /// <summary>
        /// The stream is listening.
        /// </summary>
        Listening,

        /// <summary>
        /// A SYN request was received.
        /// </summary>
        SynRecieved,

        /// <summary>
        /// The connection is established.
        /// </summary>
        Established,

        /// <summary>
        /// The connection is closing.
        /// </summary>
        Closing,

        /// <summary>
        /// The connection is closed.
        /// </summary>
        Closed,

        /// <summary>
        /// The stream is connecting.
        /// </summary>
        Connecting
    }
}
