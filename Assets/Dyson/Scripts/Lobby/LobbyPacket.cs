using Hamad.Scripts;

namespace Dyson.GPG222.Lobby
{
    namespace Dyson.GPG222.Lobby
    {
        public class LobbyPacket : Packet
        {
            public bool IsReady { get; private set; }
            public string PlayerName { get; private set; }

            public LobbyPacket() : base(PacketType.JoinLobby, null)
            {
                IsReady = false;
                PlayerName = "";
            }

            public LobbyPacket(PlayerData playerData) : base(PacketType.JoinLobby, playerData)
            {
                this.playerData = playerData;
                PlayerName = playerData.name;
                IsReady = false;
            }

            public LobbyPacket(PlayerData playerData, bool isReady) : base(PacketType.JoinLobby, playerData)
            {
                this.playerData = playerData;
                PlayerName = playerData.name;
                IsReady = isReady;
            }

            public byte[] Serialize()
            {
                BeginSerialize();
                _binaryWriter.Write(IsReady);
                _binaryWriter.Write(PlayerName);
                return EndSerialize();
            }

            public new LobbyPacket Deserialize(byte[] buffer)
            {
                base.Deserialize(buffer);
                IsReady = _binaryReader.ReadBoolean();
                PlayerName = _binaryReader.ReadString();
                return this;
            }
        }
    }
}