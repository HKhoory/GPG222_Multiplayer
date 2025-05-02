using Hamad.Scripts;

namespace Leonardo.Scripts.Packets
{
    public class ReadyInLobbyPacket : Packet
    {
        public bool isPlayerReady { get; private set; }

        public ReadyInLobbyPacket() : base(PacketType.ReadyInLobby, null)
        {
            isPlayerReady = false;
        }

        public ReadyInLobbyPacket(PlayerData playerData, bool isReady) : base(PacketType.ReadyInLobby, playerData)
        {
            this.playerData = playerData;
            this.isPlayerReady = isReady;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(isPlayerReady);
            return EndSerialize();
        }

        public new ReadyInLobbyPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            isPlayerReady = _binaryReader.ReadBoolean();
            return this;
        }
    }
}