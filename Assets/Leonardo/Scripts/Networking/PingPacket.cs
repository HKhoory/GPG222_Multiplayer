using System;
using Hamad.Scripts;

namespace Leonardo.Scripts.Networking
{
    public class PingPacket : Packet
    {
        // Timestamp to track when the ping was sent.
        public long Timestamp { get; private set; }

        public PingPacket() : base(PacketType.Ping, null)
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public PingPacket(PlayerData playerData) : base(PacketType.Ping, playerData)
        {
            this.playerData = playerData;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(Timestamp);
            return EndSerialize();
        }

        public new PingPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            Timestamp = _binaryReader.ReadInt64();
            return this;
        }
    }
}