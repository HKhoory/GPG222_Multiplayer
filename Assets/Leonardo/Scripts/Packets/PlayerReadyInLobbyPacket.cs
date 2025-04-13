using Hamad.Scripts;

namespace Leonardo.Scripts.Packets
{
    public class ReadyInLobbyPacket : Packet
    {
        public bool IsReady { get; private set; }

        public ReadyInLobbyPacket() : base(PacketType.ReadyInLobby, null)
        {
            IsReady = false;
        }

        public ReadyInLobbyPacket(PlayerData playerData, bool isReady) : base(PacketType.ReadyInLobby, playerData)
        {
            this.playerData = playerData;
            IsReady = isReady;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(IsReady);
            return EndSerialize();
        }

        public new ReadyInLobbyPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            IsReady = _binaryReader.ReadBoolean();
            return this;
        }
    }
}