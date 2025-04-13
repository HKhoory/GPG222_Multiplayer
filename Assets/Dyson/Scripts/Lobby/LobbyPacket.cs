using Hamad.Scripts;

namespace Dyson.GPG222.Lobby
{
    public class LobbyPacket : Packet
    {
        public bool isLobbyJoined { get; private set; }

        public LobbyPacket() : base(PacketType.JoinLobby, null)
        {
            isLobbyJoined = false;
        }

        public LobbyPacket(PlayerData playerData) : base(PacketType.JoinLobby, playerData)
        {
            this.playerData = playerData;
            isLobbyJoined = true;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(isLobbyJoined);
            return EndSerialize();
        }

        public new LobbyPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            isLobbyJoined = _binaryReader.ReadBoolean();
            return this;
        }
    }
}
