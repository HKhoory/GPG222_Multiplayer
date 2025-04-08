using Hamad.Scripts;

namespace Leonardo.Scripts
{
    public class PingResponsePacket : Packet
    {
        public long OriginalTimestamp { get; private set; }

        public PingResponsePacket() : base(PacketType.PingResponse, null)
        {
            OriginalTimestamp = 0;
        }

        public PingResponsePacket(PlayerData playerData, long originalTimestamp) : base(PacketType.PingResponse, playerData)
        {
            this.playerData = playerData;
            this.OriginalTimestamp = originalTimestamp;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(OriginalTimestamp);
            return EndSerialize();
        }

        public new PingResponsePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            OriginalTimestamp = _binaryReader.ReadInt64();
            return this;
        }
    }
}