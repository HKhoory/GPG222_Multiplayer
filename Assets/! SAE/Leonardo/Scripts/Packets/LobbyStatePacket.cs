using System.Collections.Generic;
using Hamad.Scripts;

namespace __SAE.Leonardo.Scripts.Packets
{
    public class LobbyStatePacket : Packet
    {
        public class LobbyPlayerInfo
        {
            public int ClientId;
            public string PlayerName;
            public bool IsReady;

            public LobbyPlayerInfo(int clientId, string playerName, bool isReady)
            {
                ClientId = clientId;
                PlayerName = playerName;
                IsReady = isReady;
            }
        }

        public List<LobbyPlayerInfo> Players { get; private set; }

        public LobbyStatePacket() : base(PacketType.LobbyState, null)
        {
            Players = new List<LobbyPlayerInfo>();
        }

        public LobbyStatePacket(PlayerData playerData) : base(PacketType.LobbyState, playerData)
        {
            this.playerData = playerData;
            Players = new List<LobbyPlayerInfo>();
        }

        public void AddPlayer(int clientId, string playerName, bool isReady)
        {
            Players.Add(new LobbyPlayerInfo(clientId, playerName, isReady));
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            
            // Write the number of players.
            _binaryWriter.Write(Players.Count);
            
            // Write each player's info.
            foreach (var player in Players)
            {
                _binaryWriter.Write(player.ClientId);
                _binaryWriter.Write(player.PlayerName);
                _binaryWriter.Write(player.IsReady);
            }
            
            return EndSerialize();
        }

        public new LobbyStatePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            
            // Read the number of players.
            int playerCount = _binaryReader.ReadInt32();
            Players = new List<LobbyPlayerInfo>(playerCount);
            
            // Read each player's info.
            for (int i = 0; i < playerCount; i++)
            {
                int clientId = _binaryReader.ReadInt32();
                string playerName = _binaryReader.ReadString();
                bool isReady = _binaryReader.ReadBoolean();
                
                Players.Add(new LobbyPlayerInfo(clientId, playerName, isReady));
            }
            
            return this;
        }
    }
}