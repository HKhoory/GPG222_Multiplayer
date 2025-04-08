using System;
using System.Collections.Generic;
using UnityEngine;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;

namespace Leonardo.Scripts.Networking
{
    public class PacketHandler
    {
        public event Action<string, string> OnMessageReceived;
        public event Action<PlayerPositionData> OnPositionReceived;
        public event Action OnPingResponseReceived;
        
        private PlayerData _localPlayerData;
        
        public PacketHandler(PlayerData localPlayerData)
        {
            this._localPlayerData = localPlayerData;
        }
        
        public void ProcessPacket(byte[] data)
        {
            try
            {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);
                
                switch (basePacket.packetType)
                {
                    case Packet.PacketType.Message:
                        ProcessMessagePacket(data);
                        break;
                        
                    case Packet.PacketType.PlayersPositionData:
                        ProcessPositionPacket(data);
                        break;
                        
                    case Packet.PacketType.PingResponse:
                        ProcessPingResponsePacket();
                        break;
                        
                    default:
                        Debug.LogWarning($"PacketHandler.cs: Unknown packet type received: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PacketHandler.cs: Error processing packet: {e.Message}");
            }
        }
        
        private void ProcessMessagePacket(byte[] data)
        {
            MessagePacket messagePacket = new MessagePacket().Deserialize(data);
            OnMessageReceived?.Invoke(messagePacket.playerData.name, messagePacket.Message);
        }
        
        private void ProcessPositionPacket(byte[] data)
        {
            PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
            
            foreach (var playerPos in positionPacket.PlayerPositionData)
            {
                // Skip updates for local player
                if (playerPos.playerData.tag == _localPlayerData.tag)
                    continue;
                    
                OnPositionReceived?.Invoke(playerPos);
            }
        }
        
        private void ProcessPingResponsePacket()
        {
            OnPingResponseReceived?.Invoke();
        }
        
        public byte[] CreateMessagePacket(string message)
        {
            MessagePacket messagePacket = new MessagePacket(_localPlayerData, message);
            return messagePacket.Serialize();
        }
        
        public byte[] CreatePositionPacket(Vector3 position)
        {
            PlayerPositionData positionData = new PlayerPositionData(
                _localPlayerData,
                position.x,
                position.y,
                position.z
            );
            
            List<PlayerPositionData> playerPositionList = new List<PlayerPositionData> { positionData };
            PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket(_localPlayerData, playerPositionList);
            return positionPacket.Serialize();
        }
        
        public byte[] CreatePingPacket()
        {
            PingPacket pingPacket = new PingPacket(_localPlayerData);
            return pingPacket.Serialize();
        }
    }
}