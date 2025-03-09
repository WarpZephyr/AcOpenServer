using AcOpenServer.Crypto;
using AcOpenServer.Exceptions;
using AcOpenServer.Network.Crypto.RPCN;
using BinaryMemory.IO;

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
        /// Whether or not the issue date is in the future.
        /// </summary>
        public bool NotIssuedYet
            => DateTimeOffset.Now < IssuedDate;

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
        /// Create a new and empty <see cref="Ticket"/>.
        /// </summary>
        public Ticket()
        {
            Version = 0x21010000;
            Serial = new byte[20];
            IssuerID = 0x100;
            IssuedDate = DateTimeOffset.Now;
            ExpireDate = IssuedDate.AddMinutes(15);
            UserID = 0UL;
            OnlineID = string.Empty;
            Region = new byte[4];
            Domain = string.Empty;
            ServiceID = new byte[24];
            Status = 0;
            Cookie = new byte[33];
            Signer = new byte[4];
            Signature = new byte[63];
        }

        /// <summary>
        /// Parse a new <see cref="Ticket"/> from a payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <exception cref="NotSupportedException">The payload was too big.</exception>
        /// <exception cref="InvalidDataException">The payload was too small for the specified sizes.</exception>
        private Ticket(ReadOnlySpan<byte> payload)
        {
            var br = new BinarySpanReader(payload, true);
            Version = br.ReadUInt32();
            uint size = br.ReadUInt32();
            if (size > payload.Length - 8)
                throw new TicketParseException($"Payload buffer is too small for the specified data size; Minimum Expected: {size}, Remaining: {payload.Length - 8}");

            ReadUserdata(ref br);
            ReadSignature(ref br);
        }

        #endregion

        #region Read

        public static Ticket Read(ReadOnlySpan<byte> payload)
            => new(payload);

        public static bool TryRead(ReadOnlySpan<byte> payload, [NotNullWhen(true)] out Ticket? result, [NotNullWhen(false)] out string? error)
        {
            try
            {
                result = Read(payload);
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

        #region Write

        public Span<byte> Write()
        {
            using var bw = new BinaryStreamWriter(true);
            bw.WriteUInt32(Version);
            bw.ReserveUInt32("Size");
            long dataStart = bw.Position;
            WriteUserdata(bw);
            WriteSignature(bw);
            bw.FillUInt32("Size", (uint)(bw.Position - dataStart));
            return bw.ToArray();
        }

        private byte[] GetUserdataBytes()
        {
            using var bw = new BinaryStreamWriter(true);
            WriteUserdata(bw);
            return bw.ToArray();
        }

        #endregion

        #region Read Helpers

        private static ushort Expect(ref BinarySpanReader br, TicketDataType expectedType)
        {
            var type = (TicketDataType)br.ReadUInt16();
            if (type != expectedType)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)}; Expected: {expectedType}, Received: {type}");

            // Length
            return br.ReadUInt16();
        }

        private static void Expect(ref BinarySpanReader br, TicketDataType expectedType, int expectedLength)
        {
            var type = (TicketDataType)br.ReadUInt16();
            if (type != expectedType)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)}; Expected: {expectedType}, Received: {type}");

            var length = br.ReadUInt16();
            if (length != expectedLength)
                throw new TicketParseException($"Unexpected {nameof(TicketDataType)} length; Expected: {expectedLength}, Received: {length}");
        }

        private static uint ReadTicketDataU32(ref BinarySpanReader br)
        {
            Expect(ref br, TicketDataType.U32, sizeof(uint));
            return br.ReadUInt32();
        }

        private static ulong ReadTicketDataU64(ref BinarySpanReader br)
        {
            Expect(ref br, TicketDataType.U64, sizeof(ulong));
            return br.ReadUInt64();
        }

        private static DateTimeOffset ReadTicketDataTime(ref BinarySpanReader br)
        {
            Expect(ref br, TicketDataType.Time, sizeof(ulong));
            return DateTimeOffset.FromUnixTimeMilliseconds((long)br.ReadUInt64());
        }

        private static string ReadTicketDataBString(ref BinarySpanReader br)
        {
            ushort length = Expect(ref br, TicketDataType.BString);
            return br.ReadUTF8(length);
        }

        private static byte[] ReadTicketDataBinary(ref BinarySpanReader br)
        {
            ushort length = Expect(ref br, TicketDataType.Binary);
            return br.ReadBytes(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadTicketDataEmpty(ref BinarySpanReader br)
        {
            Expect(ref br, TicketDataType.Empty, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TicketDataType PeekTicketDataType(ref BinarySpanReader br)
            => (TicketDataType)br.PeekUInt16();

        [MemberNotNull(nameof(Serial))]
        [MemberNotNull(nameof(OnlineID))]
        [MemberNotNull(nameof(Region))]
        [MemberNotNull(nameof(Domain))]
        [MemberNotNull(nameof(ServiceID))]
        [MemberNotNull(nameof(Cookie))]
        private void ReadUserdata(ref BinarySpanReader br)
        {
            _ = Expect(ref br, TicketDataType.BlobUserdata);
            Serial = ReadTicketDataBinary(ref br);
            IssuerID = ReadTicketDataU32(ref br);
            IssuedDate = ReadTicketDataTime(ref br);
            ExpireDate = ReadTicketDataTime(ref br);
            UserID = ReadTicketDataU64(ref br);
            OnlineID = ReadTicketDataBString(ref br);
            Region = ReadTicketDataBinary(ref br);
            Domain = ReadTicketDataBString(ref br);
            ServiceID = ReadTicketDataBinary(ref br);
            Status = ReadTicketDataU32(ref br);

            if (PeekTicketDataType(ref br) != TicketDataType.Empty)
            {
                Cookie = ReadTicketDataBinary(ref br);
            }
            else
            {
                Cookie = [];
            }

            ReadTicketDataEmpty(ref br);
            ReadTicketDataEmpty(ref br);
        }

        [MemberNotNull(nameof(Signer))]
        [MemberNotNull(nameof(Signature))]
        private void ReadSignature(ref BinarySpanReader br)
        {
            _ = Expect(ref br, TicketDataType.BlobSignature);
            Signer = ReadTicketDataBinary(ref br);
            Signature = ReadTicketDataBinary(ref br);
        }

        #endregion

        #region Write Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTicketDataType(BinaryStreamWriter bw, TicketDataType type)
            => bw.WriteUInt16((ushort)type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTicketDataHeader(BinaryStreamWriter bw, TicketDataType type, ushort length)
        {
            WriteTicketDataType(bw, type);
            bw.WriteUInt16(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTicketDataHeader(BinaryStreamWriter bw, TicketDataType type, string lengthReservation)
        {
            WriteTicketDataType(bw, type);
            bw.ReserveUInt16(lengthReservation);
        }

        private static void WriteTicketDataU32(BinaryStreamWriter bw, uint value)
        {
            WriteTicketDataHeader(bw, TicketDataType.U32, sizeof(uint));
            bw.WriteUInt32(value);
        }

        private static void WriteTicketDataU64(BinaryStreamWriter bw, ulong value)
        {
            WriteTicketDataHeader(bw, TicketDataType.U64, sizeof(ulong));
            bw.WriteUInt64(value);
        }

        private static void WriteTicketDataTime(BinaryStreamWriter bw, DateTimeOffset value)
        {
            WriteTicketDataHeader(bw, TicketDataType.Time, sizeof(ulong));
            bw.WriteUInt64((ulong)value.ToUnixTimeMilliseconds());
        }

        private static void WriteTicketDataBString(BinaryStreamWriter bw, string value, ushort length)
        {
            WriteTicketDataHeader(bw, TicketDataType.BString, length);
            bw.WriteFixedUTF8(value, length, 0);
        }

        private static void WriteTicketDataBinary(BinaryStreamWriter bw, byte[] value)
        {
            WriteTicketDataHeader(bw, TicketDataType.Binary, (ushort)value.Length);
            bw.WriteBytes(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTicketDataEmpty(BinaryStreamWriter bw)
        {
            WriteTicketDataHeader(bw, TicketDataType.Empty, 0);
        }

        private void WriteUserdata(BinaryStreamWriter bw)
        {
            WriteTicketDataHeader(bw, TicketDataType.BlobUserdata, "UserdataLength");
            long dataStart = bw.Position;

            WriteTicketDataBinary(bw, Serial);
            WriteTicketDataU32(bw, IssuerID);
            WriteTicketDataTime(bw, IssuedDate);
            WriteTicketDataTime(bw, ExpireDate);
            WriteTicketDataU64(bw, UserID);
            WriteTicketDataBString(bw, OnlineID, 32);
            WriteTicketDataBinary(bw, Region);
            WriteTicketDataBString(bw, Domain, 4);
            WriteTicketDataBinary(bw, ServiceID);
            WriteTicketDataU32(bw, Status);

            if (Cookie.Length > 0)
            {
                WriteTicketDataBinary(bw, Cookie);
            }

            WriteTicketDataEmpty(bw);
            WriteTicketDataEmpty(bw);

            bw.FillUInt16("UserdataLength", (ushort)(bw.Position - dataStart));
        }

        private void WriteSignature(BinaryStreamWriter bw)
        {
            WriteTicketDataHeader(bw, TicketDataType.BlobSignature, "SignatureLength");
            long dataStart = bw.Position;

            WriteTicketDataBinary(bw, Signer);
            WriteTicketDataBinary(bw, Signature);

            bw.FillUInt16("SignatureLength", (ushort)(bw.Position - dataStart));
        }

        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetSerialString()
            => BinaryBufferReader.PeekUTF8(Serial);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetRegionString()
            => BinaryBufferReader.PeekUTF8(Region);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetServiceIdString()
            => BinaryBufferReader.PeekUTF8(ServiceID);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetSignerString()
            => BinaryBufferReader.PeekUTF8(Signer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRPCN()
            // Check for "RPCN"
            => Signer.Length == 4
            && Signer[0] == 0x52
            && Signer[1] == 0x50
            && Signer[2] == 0x43
            && Signer[3] == 0x4E;

        #endregion

        #region Crypto

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] GetRpcnSignedBytes()
            => GetUserdataBytes();

        public bool RpcnSignatureValid(ReadOnlySpan<byte> input)
            => RpcnSignatureVerifier.RpcnSignatureValid(input, Signature);

        public bool RpcnSignatureValid(CipherSignatureParameters parameters, ReadOnlySpan<byte> input)
            => SignatureVerifier.SignatureValid(parameters, input, Signature);

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
