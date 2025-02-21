using AcOpenServer.Binary;
using AcOpenServer.Exceptions;
using AcOpenServer.Network.Data.FSDP;
using AcOpenServer.Utilities;
using System;

namespace AcOpenServer.Network.Communication.Fsdp
{
    public class FsdpReliablePacketClient
    {
        /// <summary>
        /// How many values ACK increases before it rolls over.
        /// </summary>
        private const int MaxAckValue = 4096;

        private readonly FsdpPacketClient Client;
        private FsdpStreamState State;
        private int SequenceIndex;
        private int SequenceIndexAcked;
        private int RemoteSequenceIndex;
        private int RemoteSequenceIndexAcked;
        private int LastPacketLocalAck;
        private int LastPacketRemoteAck;
        private DateTime LastAckSendTime;

        public event EventHandler<FsdpReliablePacket>? Received;

        public FsdpReliablePacketClient(FsdpPacketClient client)
        {
            Client = client;
            State = FsdpStreamState.Listening;
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

        public void Send(FsdpReliablePacket packet)
        {
            if (State == FsdpStreamState.Closing)
            {
                return;
            }

            if (OpcodeSequenced(packet.Header.Opcode) || packet.Header.Opcode == FsdpOpcode.UNKNOWN)
            {
                // TODO
            }
            else
            {
                // TODO
            }
        }

        private void HandleIncoming(FsdpReliablePacket packet)
        {
            LastPacketLocalAck = packet.Header.LocalAckCounter;
            LastPacketRemoteAck = packet.Header.RemoteAckCounter;

            if (OpcodeSequenced(packet.Header.Opcode))
            {

            }
            else
            {
                
            }
        }

        private void ProcessPacket(FsdpReliablePacket packet)
        {
            switch (packet.Header.Opcode)
            {
                case FsdpOpcode.SYN:
                    HandleSyn(packet);
                    break;
                case FsdpOpcode.SYN_ACK:
                    break;
                case FsdpOpcode.DAT:
                    break;
                case FsdpOpcode.HBT:
                    break;
                case FsdpOpcode.FIN:
                    break;
                case FsdpOpcode.RST:
                    break;
                case FsdpOpcode.ACK:
                    break;
                case FsdpOpcode.RACK:
                    break;
                case FsdpOpcode.DAT_ACK:
                    break;
                case FsdpOpcode.FIN_ACK:
                    break;
                case FsdpOpcode.UNKNOWN:
                default:
                    throw new FsdpReliableException($"Unknown {nameof(FsdpOpcode)}: {packet.Header.Opcode}");
            }
        }

        #endregion

        #region Opcode Handlers

        private void HandleSyn(FsdpReliablePacket packet)
        {
            State = FsdpStreamState.SynRecieved;

            var local = packet.Header.LocalAckCounter;

            // Send a SYN_ACK in response.
            SendSynAck(local);

            // And send our ACK message as well (this seems redundent, but its what happens in ds3).
            SendAck(local);
        }

        #endregion

        #region Opcode Senders

        private void SendSyn()
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAckCounter = SequenceIndex;
            header.RemoteAckCounter = 0;
            header.Opcode = FsdpOpcode.SYN;

            var syn = FsdpSyn.CreateDefault();
            byte[] payload = new byte[FsdpSyn.Length];
            BinaryBufferWriter.Write(payload, 0, syn);

            var packet = new FsdpReliablePacket(header, payload);
            Send(packet);
        }

        private void SendSynAck(int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAckCounter = SequenceIndex;
            header.RemoteAckCounter = remoteIndex;
            header.Opcode = FsdpOpcode.SYN_ACK;

            var synAck = FsdpSynAck.CreateDefault();
            byte[] payload = new byte[FsdpSynAck.Length];
            BinaryBufferWriter.Write(payload, 0, synAck);

            var response = new FsdpReliablePacket(header, payload);
            Send(response);

            RemoteSequenceIndex = remoteIndex;

            // SYN_ACK bumps the sequence index so is a "sequenced opcode", but doesn't abid by
            // any of the other conventions of sequenced ones. So simplest to just bump the sequence
            // index here.
            SequenceIndex = (SequenceIndex + 1) % MaxAckValue;
        }

        private void SendAck(int remoteIndex)
        {
            var header = FsdpReliablePacketHeader.CreateDefault();
            header.LocalAckCounter = 0;
            header.RemoteAckCounter = remoteIndex;
            header.Opcode = FsdpOpcode.ACK;

            var response = new FsdpReliablePacket(header, []);
            Send(response);

            RemoteSequenceIndexAcked = remoteIndex;
            LastAckSendTime = DateTime.Now;
        }

        #endregion

        #region Stream State

        private void Reset()
        {
            SequenceIndex = RandomHelper.NextInt32() % 4096;
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

        #endregion

        #region Opcode

        private static bool OpcodeSequenced(FsdpOpcode opcode)
            => opcode == FsdpOpcode.DAT
            || opcode == FsdpOpcode.DAT_ACK
            || opcode == FsdpOpcode.FIN_ACK;

        #endregion
    }
}
