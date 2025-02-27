namespace AcOpenServer.Network.Data.FSDP
{
    /// <summary>
    /// The different opcodes used by FSDP for communication.
    /// </summary>
    public enum FsdpOpcode : byte
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        UNKNOWN = 0x00,

        /// <summary>
        /// Used to establish connection and sync sequence numbers.
        /// </summary>
        SYN = 0x02,

        /// <summary>
        /// Unknown.
        /// </summary>
        RACK = 0x03,

        /// <summary>
        /// Data fragment packet.
        /// </summary>
        DAT = 0x04,

        /// <summary>
        /// Heartbeat packet.
        /// </summary>
        HBT = 0x05,

        /// <summary>
        /// Connection termination.
        /// </summary>
        FIN = 0x06,

        /// <summary>
        /// Reset the connection.
        /// </summary>
        RST = 0x07,

        /// <summary>
        /// Unknown.
        /// </summary>
        PT_DAT_FRAG = 0x08,

        /// <summary>
        /// Acknowledges the highest packet in the sequence that has been recieved.
        /// </summary>
        ACK = 0x01 | 0x30,

        /// <summary>
        /// Acknowledgement of SYN packet along with remote machines owning sequence number information.
        /// </summary>
        SYN_ACK = SYN | 0x30,

        /// <summary>
        /// Acknowledges data packet and also contains a payload. This basically seems to be a "reply" opcode.
        /// </summary>
        DAT_ACK = DAT | 0x30,

        /// <summary>
        /// Acknowledges connection termination.
        /// </summary>
        FIN_ACK = FIN | 0x30,

        /// <summary>
        /// Unknown.
        /// </summary>
        PT_DAT_FRAG_ACK = PT_DAT_FRAG | 0x30
    }
}
