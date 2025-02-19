using AcOpenServer.Binary;
using AcOpenServer.Network.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Network.Data.RPCN
{
    /// <summary>
    /// A ticket for netplay.
    /// </summary>
    public class Ticket
    {
        #region Members

        /// <summary>
        /// The version.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// The number of packets sent since the netplay server started.
        /// </summary>
        public byte[] Serial { get; set; }

        /// <summary>
        /// The ID of the issuer;<br/>
        /// Release is 0x100,<br/>
        /// Others are 8 and 1, which may be test or debug?
        /// </summary>
        public uint IssuerID { get; set; }

        /// <summary>
        /// The date the ticket was issued.
        /// </summary>
        public DateTimeOffset IssuedDate { get; set; }

        /// <summary>
        /// The date the ticket expires.
        /// </summary>
        public DateTimeOffset ExpireDate { get; set; }

        /// <summary>
        /// The ID of the user requesting the ticket.
        /// </summary>
        public ulong UserID { get; set; }

        /// <summary>
        /// The name of the user requesting the ticket.
        /// </summary>
        public string OnlineID { get; set; }

        /// <summary>
        /// The region of the user requesting the ticket.
        /// </summary>
        public byte[] Region { get; set; }

        /// <summary>
        /// The domain of the user requesting the ticket.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The title ID of the game requesting the ticket.
        /// </summary>
        public byte[] ServiceID { get; set; }

        /// <summary>
        /// The status of the ticket.
        /// </summary>
        public uint Status { get; set; }

        /// <summary>
        /// Userdata within the ticket.
        /// </summary>
        public byte[] Cookie { get; set; }

        /// <summary>
        /// The signer of the ticket, empty if not signed.
        /// </summary>
        public byte[] Signer { get; set; }

        /// <summary>
        /// The signature of the ticket, empty if not signed.
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// Whether or not this ticket is now expired.
        /// </summary>
        public bool IsExpired
            => DateTimeOffset.Now >= ExpireDate;

        /// <summary>
        /// Whether or not this ticket has a non-empty signer currently.
        /// </summary>
        public bool HasSigner
        {
            get
            {
                for (int i = 0; i < Signer.Length; i++)
                    if (Signer[i] != 0)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Whether or not this ticket has a non-empty signature currently.
        /// </summary>
        public bool HasSignature
        {
            get
            {
                for (int i = 0; i < Signature.Length; i++)
                    if (Signature[i] != 0)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Whether or not this ticket is signed currently.
        /// </summary>
        public bool IsSigned
            => HasSigner && HasSignature;

        #endregion

        #region Constructors

        /// <summary>
        /// Parse a new <see cref="Ticket"/> from a payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <exception cref="NotSupportedException">The payload was too big.</exception>
        /// <exception cref="InvalidDataException">The payload was too small for the specified sizes.</exception>
        public Ticket(Span<byte> payload)
        {
            int offset = 0;
            Version = BinaryBufferReader.ReadUInt32BigEndian(payload, ref offset);
            int size = BinaryBufferReader.ReadInt32BigEndian(payload, ref offset);
            if (size < 0)
                throw new TicketParseException($"Payloads bigger than {int.MaxValue} are not supported; Size: {(uint)size}");

            if (size > payload.Length - 8)
                throw new TicketParseException($"Payload buffer is too small for the specified data size; Minimum Expected: {size}, Remaining: {payload.Length - 8}");

            ReadUserdata(payload, ref offset);
            ReadSignature(payload, ref offset);
        }

        #endregion

        #region Parse

        public static Ticket Parse(Span<byte> payload)
            => new Ticket(payload);

        public static bool TryParse(Span<byte> payload, [NotNullWhen(true)] out Ticket? result, [NotNullWhen(false)] out string? error)
        {
            // TODO Stop using exceptions as logic
            try
            {
                result = Parse(payload);
                error = null;
                return true;
            }
            catch (TicketParseException ex)
            {
                result = null;
                error = ex.Message;
                return false;
            }
        }

        #endregion

        #region Read

        private static ushort Expect(Span<byte> payload, ref int offset, TicketDataType expectedType)
        {
            var type = (TicketDataType)BinaryBufferReader.ReadUInt16BigEndian(payload, ref offset);
            if (type != expectedType)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)}; Expected: {expectedType}, Received: {type}");

            // Length
            return BinaryBufferReader.ReadUInt16BigEndian(payload, ref offset);
        }

        private static void Expect(Span<byte> payload, ref int offset, TicketDataType expectedType, int expectedLength)
        {
            var type = (TicketDataType)BinaryBufferReader.ReadUInt16BigEndian(payload, ref offset);
            if (type != expectedType)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)}; Expected: {expectedType}, Received: {type}");

            var length = BinaryBufferReader.ReadUInt16BigEndian(payload, ref offset);
            if (length != expectedLength)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)} length; Expected: {expectedLength}, Received: {length}");
        }

        private static uint ReadTicketDataU32(Span<byte> payload, ref int offset)
        {
            Expect(payload, ref offset, TicketDataType.U32, sizeof(uint));
            return BinaryBufferReader.ReadUInt32BigEndian(payload, ref offset);
        }

        private static ulong ReadTicketDataU64(Span<byte> payload, ref int offset)
        {
            Expect(payload, ref offset, TicketDataType.U64, sizeof(ulong));
            return BinaryBufferReader.ReadUInt64BigEndian(payload, ref offset);
        }

        private static DateTimeOffset ReadTicketDataTime(Span<byte> payload, ref int offset)
        {
            Expect(payload, ref offset, TicketDataType.Time, sizeof(ulong));
            return DateTimeOffset.FromUnixTimeMilliseconds((long)BinaryBufferReader.ReadUInt64BigEndian(payload, ref offset));
        }

        private static string ReadTicketDataBString(Span<byte> payload, ref int offset)
        {
            ushort length = Expect(payload, ref offset, TicketDataType.BString);
            return BinaryBufferReader.ReadFixedUTF8(payload, length, ref offset);
        }

        private static byte[] ReadTicketDataBinary(Span<byte> payload, ref int offset)
        {
            ushort length = Expect(payload, ref offset, TicketDataType.Binary);
            var value = payload.Slice(offset, length).ToArray();
            offset += length;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadTicketDataEmpty(Span<byte> payload, ref int offset)
        {
            Expect(payload, ref offset, TicketDataType.Empty, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TicketDataType PeekTicketDataType(Span<byte> payload, int offset)
            => (TicketDataType)BinaryBufferReader.ReadUInt16BigEndian(payload, offset);

        [MemberNotNull(nameof(Serial))]
        [MemberNotNull(nameof(OnlineID))]
        [MemberNotNull(nameof(Region))]
        [MemberNotNull(nameof(Domain))]
        [MemberNotNull(nameof(ServiceID))]
        [MemberNotNull(nameof(Cookie))]
        private void ReadUserdata(Span<byte> payload, ref int offset)
        {
            _ = Expect(payload, ref offset, TicketDataType.BlobUserdata);
            Serial = ReadTicketDataBinary(payload, ref offset);
            IssuerID = ReadTicketDataU32(payload, ref offset);
            IssuedDate = ReadTicketDataTime(payload, ref offset);
            ExpireDate = ReadTicketDataTime(payload, ref offset);
            UserID = ReadTicketDataU64(payload, ref offset);
            OnlineID = ReadTicketDataBString(payload, ref offset);
            Region = ReadTicketDataBinary(payload, ref offset);
            Domain = ReadTicketDataBString(payload, ref offset);
            ServiceID = ReadTicketDataBinary(payload, ref offset);
            Status = ReadTicketDataU32(payload, ref offset);

            if (PeekTicketDataType(payload, offset) != TicketDataType.Empty)
            {
                Cookie = ReadTicketDataBinary(payload, ref offset);
            }
            else
            {
                Cookie = [];
            }

            ReadTicketDataEmpty(payload, ref offset);
            ReadTicketDataEmpty(payload, ref offset);
        }

        [MemberNotNull(nameof(Signer))]
        [MemberNotNull(nameof(Signature))]
        private void ReadSignature(Span<byte> payload, ref int offset)
        {
            _ = Expect(payload, ref offset, TicketDataType.BlobSignature);
            Signer = ReadTicketDataBinary(payload, ref offset);
            Signature = ReadTicketDataBinary(payload, ref offset);
        }

        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetSerialString()
            => BinaryBufferReader.ReadUTF8(Serial);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetRegionString()
            => BinaryBufferReader.ReadUTF8(Region);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetServiceIdString()
            => BinaryBufferReader.ReadUTF8(ServiceID);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetSignerString()
            => BinaryBufferReader.ReadUTF8(Signer);

        #endregion

        #region Types

        /// <summary>
        /// The supported data types.
        /// </summary>
        private enum TicketDataType
        {
            /// <summary>
            /// No data.
            /// </summary>
            Empty = 0,

            /// <summary>
            /// Data is an unsigned 32-bit integer.
            /// </summary>
            U32 = 1,

            /// <summary>
            /// Data is an unsigned 64-bit integer.
            /// </summary>
            U64 = 2,

            /// <summary>
            /// Data is a string.
            /// </summary>
            BString = 4,

            /// <summary>
            /// Data is a unix millisecond epoch timestamp in 64-bits.
            /// </summary>
            Time = 7,

            /// <summary>
            /// Data is raw binary.
            /// </summary>
            Binary = 8,

            /// <summary>
            /// Data is a userdata blob.
            /// </summary>
            BlobUserdata = 0x3000 | 0,

            /// <summary>
            /// Data is a signature blob.
            /// </summary>
            BlobSignature = 0x3000 | 2
        }

        #endregion
    }
}
