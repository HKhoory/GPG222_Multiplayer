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
        public NetworkConnection _networkConnection;
        public PlayerData _playerData;
        
        public LobbyPacket() : base(PacketType.JoinLobby, null)
        {
        }
        
        public LobbyPacket(PlayerData playerData) : base(PacketType.JoinLobby, playerData)
        {
        //    _networkConnection = connection;
            _playerData = playerData;
        }

        public byte[] Serialize()
        {
            if (_playerData == null)
            {
                Debug.LogError("player data is null");
                return null;
            }
            BeginSerialize();
            _binaryWriter.Write(_playerData.name);
            _binaryWriter.Write(_playerData.tag);
            return EndSerialize();
        }

       /* public void SendLobbyPacket()
        {
            _playerData = new PlayerData("test", 123);
            var joinLobbyPacket = new LobbyPacket(_networkConnection, _playerData);
            byte[] data = joinLobbyPacket.Serialize();
           _networkConnection.SendData(data);
        } */
        
        public new LobbyPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            string name = _binaryReader.ReadString();
            int tag = _binaryReader.ReadInt32();
            Debug.Log($"LobbyPacket deserialized with type: {packetType}");
            return this;
        }
    }
}
