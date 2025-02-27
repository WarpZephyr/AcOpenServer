namespace BinaryMemory.IO
{
    public interface IBinaryReader
    {
        public long Position { get; set; }
        public long Length { get; }
        public long Remaining
            => Length - Position;

        public sbyte ReadSByte();
        public byte ReadByte();
        public short ReadInt16();
        public ushort ReadUInt16();
        public int ReadInt32();
        public uint ReadUInt32();
        public long ReadInt64();
        public ulong ReadUInt64();
        public float ReadSingle();
        public double ReadDouble();
        public bool ReadBoolean();
        public char ReadChar();
        public string ReadUTF8();
        public string ReadASCII();
        public string ReadShiftJIS();
        public string ReadUTF16();
    }
}
