namespace BinaryMemory.IO
{
    public interface IBinaryWriter
    {
        public long Position { get; set; }
        public long Length { get; }
        public long Remaining
            => Length - Position;

        public void WriteSByte(sbyte value);
        public void WriteByte(byte value);
        public void WriteInt16(short value);
        public void WriteUInt16(ushort value);
        public void WriteInt32(int value);
        public void WriteUInt32(uint value);
        public void WriteInt64(long value);
        public void WriteUInt64(ulong value);
        public void WriteSingle(float value);
        public void WriteDouble(double value);
        public void WriteBoolean(bool value);
        public void WriteChar(char value);
        public void WriteUTF8(string value, bool terminate);
        public void WriteASCII(string value, bool terminate);
        public void WriteShiftJIS(string value, bool terminate);
        public void WriteUTF16(string value, bool terminate);
    }
}
