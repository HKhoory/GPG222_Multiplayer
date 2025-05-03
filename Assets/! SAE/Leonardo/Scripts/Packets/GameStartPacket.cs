using Hamad.Scripts;

namespace __SAE.Leonardo.Scripts.Packets
{
    public class GameStartPacket : Packet
    {
        public GameStartPacket() : base(PacketType.GameStart, null)
        {
        }

        public GameStartPacket(PlayerData playerData) : base(PacketType.GameStart, playerData)
        {
            this.playerData = playerData;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            return EndSerialize();
        }

        public new GameStartPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            return this;
        }
    }
}