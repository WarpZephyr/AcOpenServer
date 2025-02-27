using AcOpenServer.Binary;
using AcOpenServer.Exceptions;
using AcOpenServer.Network.Data.FSDP;
using AcOpenServer.Utilities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Communication.Fsdp
{
    public class FsdpReliablePacketClient
    {
        /// <summary>
        /// How many values ACK increases before it rolls over.<br/>
        /// Each sequence value is only 12-bits, so (2^12)-1 or 4095 is the max they can be.
        /// </summary>
        private const int MaxAckValue = 1 << 12;

        /// <summary>
        /// The underlying packet client.
        /// </summary>
        private readonly FsdpPacketClient Client;

        /// <summary>
        /// The current state of the stream.
        /// </summary>
        private FsdpStreamState State;

        /// <summary>
        /// The sequence index on this side of the connection.
        /// </summary>
        private int SequenceIndex;
        private int SequenceIndexAcked;

        /// <summary>
        /// The sequence index of the other side of the connection.
        /// </summary>
        private int RemoteSequenceIndex;
        private int RemoteSequenceIndexAcked;
        private int LastPacketLocalAck;
        private int LastPacketRemoteAck;
        private DateTime LastAckSendTime;
        private DateTime CloseTime;

        public event EventHandler<FsdpReliablePacket>? Received;

        public FsdpReliablePacketClient(FsdpPacketClient client)
        {
            Client = client;
            Reset();
        }

        #region IO

        public void StartReceive()
        {
            Client.Received += OnInitialReceived;
            Client.StartReceive();
        }

        private void OnInitialReceived(object? sender, byte[] buffer)
        {
            // This is the initial packet that contains the connection data before it.
            // Strip this data off, we don't really care about it, just some usernames.
            Client.Received -= OnInitialReceived;
            Client.Received += OnReceived;
            if (buffer.Length > FsdpReliablePacketInitialData.Length && buffer[0] != 0xF5 && buffer[0] != 0x25)
            {
                buffer = buffer[FsdpReliablePacketInitialData.Length..];
            }

            OnReceived(sender, buffer);
        }

        private void OnReceived(object? sender, byte[] buffer)
        {
            var packet = Read(buffer);
            HandleIncoming(packet);
        }

        public Task SendAsync(FsdpReliablePacket packet)
        {
            // Swallow any packets being sent while we are closing.
            if (State == FsdpStreamState.Closing)
            {
                return Task.CompletedTask;
            }

            Debug.Assert(packet.Opcode != FsdpOpcode.UNKNOWN, $"Packet opcode must not be {FsdpOpcode.UNKNOWN} when sending.");
            byte[] bytes = Write(packet);
            return Client.SendAsync(bytes);
        }

        private void HandleIncoming(FsdpReliablePacket packet)
        {
            LastPacketLocalAck = packet.LocalAck;
            LastPacketRemoteAck = packet.RemoteAck;

            if (OpcodeSequenced(packet.Opcode))
            {
                // TODO
            }
            else
            {
                // TODO
            }
        }

        private void ProcessPacket(FsdpReliablePacket packet)
        {
            switch (packet.Opcode)
            {
                case FsdpOpcode.SYN:
                    HandleSynAsync(packet);
                    break;
                case FsdpOpcode.SYN_ACK:
                    HandleSynAckAsync(packet);
                    break;
                case FsdpOpcode.DAT:
                    HandleDat(packet);
                    break;
                case FsdpOpcode.HBT:
                    HandleHbtAsync(packet);
                    break;
                case FsdpOpcode.FIN:
                    HandleFinAsync(packet);
                    break;
                case FsdpOpcode.RST:
                    HandleRst(packet);
                    break;
                case FsdpOpcode.ACK:
                    HandleAck(packet);
                    break;
                case FsdpOpcode.RACK:
                    HandleRack(packet);
                    break;
                case FsdpOpcode.DAT_ACK:
                    HandleDatAck(packet);
                    break;
                case FsdpOpcode.FIN_ACK:
                    HandleFinAck(packet);
                    break;
                case FsdpOpcode.UNKNOWN:
                default:
                    throw new FsdpReliableException($"Unknown {nameof(FsdpOpcode)}: {packet.Opcode}");
            }
        }

        #endregion

        #region Opcode Handlers

        private async Task HandleSynAsync(FsdpReliablePacket packet)
        {
            State = FsdpStreamState.SynRecieved;

            var local = packet.LocalAck;

            // Send a SYN_ACK in response.
            await SendSynAckAsync(local);

            // And send our ACK message as well (this seems redundent, but its what happens in ds3).
            await SendAckAsync(local);
        }

        private async Task HandleSynAckAsync(FsdpReliablePacket packet)
        {
            State = FsdpStreamState.SynRecieved;

            var local = packet.LocalAck;
            RemoteSequenceIndex = local;

            // And send our ACK message as well (this seems redundent, but its what happens in ds3).
            await SendAckAsync(RemoteSequenceIndex);

            // SYN_ACK bumps the sequence index so is a "sequenced opcode", but doesn't abid by
            // any of the other conventions of sequenced ones. So simplest to just bump the sequence
            // index here.
            SequenceIndex = (SequenceIndex + 1) % MaxAckValue;
        }

        private async Task HandleHbtAsync(FsdpReliablePacket packet)
        {
            // TODO
        }

        private async Task HandleFinAsync(FsdpReliablePacket packet)
        {
            await SendFinAckAsync(packet.LocalAck);

            State = FsdpStreamState.Closing;
        }

        private void HandleFinAck(FsdpReliablePacket packet)
        {
            State = FsdpStreamState.Closing;
        }

        private void HandleRst(FsdpReliablePacket packet)
        {
            Reset();
        }

        private void HandleAck(FsdpReliablePacket packet)
        {
            // TODO
        }

        private void HandleRack(FsdpReliablePacket packet)
        {
            // TODO
        }

        private void HandleDat(FsdpReliablePacket packet)
        {
            // TODO
        }

        private void HandleDatAck(FsdpReliablePacket packet)
        {
            // TODO
        }

        #endregion

        #region Opcode Senders

        private Task SendSynAsync()
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = SequenceIndex;
            header.RemoteAck = 0;
            header.Opcode = FsdpOpcode.SYN;

            var syn = FsdpSyn.CreateDefault();
            byte[] payload = new byte[FsdpSyn.Length];
            BinaryBufferWriter.Write(payload, 0, syn);

            var packet = new FsdpReliablePacket(header, payload);
            return SendAsync(packet);
        }

        private async Task SendSynAckAsync(int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = SequenceIndex;
            header.RemoteAck = remoteIndex;
            header.Opcode = FsdpOpcode.SYN_ACK;

            var synAck = FsdpSynAck.CreateDefault();
            byte[] payload = new byte[FsdpSynAck.Length];
            BinaryBufferWriter.Write(payload, 0, synAck);

            var response = new FsdpReliablePacket(header, payload);
            await SendAsync(response);

            RemoteSequenceIndex = remoteIndex;

            // SYN_ACK bumps the sequence index so is a "sequenced opcode", but doesn't abid by
            // any of the other conventions of sequenced ones. So simplest to just bump the sequence
            // index here.
            SequenceIndex = (SequenceIndex + 1) % MaxAckValue;
        }

        private async Task SendAckAsync(int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = 0;
            header.RemoteAck = remoteIndex;
            header.Opcode = FsdpOpcode.ACK;

            var response = new FsdpReliablePacket(header, []);
            await SendAsync(response);

            RemoteSequenceIndexAcked = remoteIndex;
            LastAckSendTime = DateTime.Now;
        }

        private async Task SendDatAckAsync(int localIndex, int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = localIndex;
            header.RemoteAck = remoteIndex;
            header.Opcode = FsdpOpcode.DAT_ACK;

            var response = new FsdpReliablePacket(header, []);
            await SendAsync(response);

            RemoteSequenceIndexAcked = remoteIndex;
            LastAckSendTime = DateTime.Now;
        }

        private Task SendFinAckAsync(int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = SequenceIndex;
            header.RemoteAck = remoteIndex;
            header.Opcode = FsdpOpcode.FIN_ACK;

            var response = new FsdpReliablePacket(header, []);
            return SendAsync(response);
        }

        private async Task SendFinAsync()
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = SequenceIndex;
            header.RemoteAck = 0;
            header.Opcode = FsdpOpcode.FIN;

            var packet = new FsdpReliablePacket(header, []);
            await SendAsync(packet);

            State = FsdpStreamState.Closing;
            CloseTime = DateTime.Now;
        }

        private Task SendHbtAsync()
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAck = 0;
            header.RemoteAck = RemoteSequenceIndexAcked;
            header.Opcode = FsdpOpcode.HBT;

            var packet = new FsdpReliablePacket(header, []);
            return SendAsync(packet);
        }

        #endregion

        #region Stream State

        private void Reset()
        {
            State = FsdpStreamState.Listening;
            SequenceIndex = RandomHelper.NextInt32() % MaxAckValue;
            SequenceIndexAcked = 0;
            RemoteSequenceIndex = 0;
            RemoteSequenceIndexAcked = 0;
        }

        private int GetNextRemoteSequenceIndex()
            => (RemoteSequenceIndex + 1) % MaxAckValue;

        #endregion

        #region Serialization

        private FsdpReliablePacket Read(byte[] buffer)
        {
            if (buffer.Length < FsdpReliablePacketHeader.Length)
            {
                throw new FsdpReliableException($"Buffer is too short for required {nameof(FsdpReliablePacketHeader)} to exist.");
            }

            var header = BinaryBufferReader.Read<FsdpReliablePacketHeader>(buffer);
            return new FsdpReliablePacket(header, buffer[FsdpReliablePacketHeader.Length..]);
        }

        private byte[] Write(FsdpReliablePacket packet)
        {
            throw new NotImplementedException("TODO");
        }

        #endregion

        #region Opcode

        private static bool OpcodeSequenced(FsdpOpcode opcode)
            => opcode == FsdpOpcode.DAT
            || opcode == FsdpOpcode.DAT_ACK
            || opcode == FsdpOpcode.FIN_ACK;

        #endregion
    }
}
