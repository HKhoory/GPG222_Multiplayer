using System.Collections;
using System.Collections.Generic;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using UnityEngine;

namespace Dyson.GPG222.Lobby
{
    public class LobbyPacket : Packet
    {
        private NetworkConnection _networkConnection;
        private PlayerData _playerData;
        public LobbyPacket(NetworkConnection connection, PlayerData playerData) : base(PacketType.JoinLobby, null)
        {
            _networkConnection = connection;
            _playerData = playerData;
        }

        public byte[] Serialize()
        {
            BeginSerialize();

            return EndSerialize();
        }

        public void SendLobbyPacket()
        {
            var joinLobbyPacket = new LobbyPacket(_networkConnection, _playerData);
            byte[] data = joinLobbyPacket.Serialize();
            _networkConnection.SendData(data);
        }
        
        public new LobbyPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            
            return this;
        }
    }
}
