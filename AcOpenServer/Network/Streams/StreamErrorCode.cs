namespace AcOpenServer.Network.Streams
{
    /// <summary>
    /// An enum the result of state of a stream.
    /// </summary>
    public enum StreamErrorCode
    {
        /// <summary>
        /// The operation was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The operation was not successful.
        /// </summary>
        NotSuccess,

        /// <summary>
        /// An error occurred.
        /// </summary>
        Error
    }
}
