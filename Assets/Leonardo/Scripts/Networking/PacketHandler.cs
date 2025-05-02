using System;
using System.Collections.Generic;
using Dyson.GPG222.Lobby;
using UnityEngine;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Heartbeat;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Controller;
using Leonardo.Scripts.Packets;
using Leonardo.Scripts.Player;
using Hamad.Scripts.Restart;

namespace Leonardo.Scripts.Networking
{
    public class PacketHandler
    {
        public event Action<string, string> OnMessageReceived;
        public event Action<PlayerPositionData> OnPositionReceived;
        public event Action OnPingResponseReceived;
        public event Action<int, Vector3, string> OnPushEventReceived;
        public event Action<PlayerData, bool> OnPlayerReadyStateChanged;
        public event Action OnHeartbeat;
        public event Action<byte> OnHeartbeatReceived;
        public event Action<byte> OnRestart;
        public event Action<byte[]> OnJoiningLobby; 
        
        private PlayerData _localPlayerData;
        private Dictionary<int, PlayerData> _knownPlayers = new Dictionary<int, PlayerData>();

        public PacketHandler(PlayerData localPlayerData)
        {
            this._localPlayerData = localPlayerData;
        }

        public void ProcessPacket(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("PacketHandler.cs: Received null or empty data packet");
                return;
            }
            
            try
            {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);
                
                // Cache player data from all packets to build a mapping between tags and client IDs
                if (basePacket.playerData != null)
                {
                    _knownPlayers[basePacket.playerData.tag] = basePacket.playerData;
                }

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

                    case Packet.PacketType.PushEvent:
                        try
                        {
                            ProcessPushEventPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"PacketHandler.cs: Error processing push event: {e.Message}\n{e.StackTrace}");
                        }
                        break;

                    case Packet.PacketType.Heartbeat:
                        try
                        {
                            ProcessHeartbeatPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"PacketHandler.cs: Error processing heartbeat: {e.Message}");
                        }
                        break;
                        
                    case Packet.PacketType.Restart:
                        try
                        {
                            ProcessRestartPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"PacketHandler.cs: Error processing restart packet: {e.Message}");
                        }
                        break;
                        
                    case Packet.PacketType.JoinLobby:
                        try
                        {
                            ProcessJoiningLobby(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"PacketHandler.cs: Error processing lobby packet: {e.Message}");
                        }
                        break;
                        
                    case Packet.PacketType.ReadyInLobby:
                        try
                        {
                            ProcessReadyInLobbyPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"PacketHandler.cs: Error processing ready state packet: {e.Message}");
                        }
                        break;

                    default:
                        Debug.LogWarning($"PacketHandler.cs: Unknown packet type received: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PacketHandler.cs: Error processing packet: {e.Message}\n{e.StackTrace}");
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

        private void ProcessHeartbeatPacket(byte[] data)
        {
            HeartbeatPacket heartbeatPacket = new HeartbeatPacket().Deserialize(data);
            byte heartbeat = heartbeatPacket.heartbeat;
            
            OnHeartbeatReceived?.Invoke(heartbeat);
            OnHeartbeat?.Invoke();
        }

        private void ProcessJoiningLobby(byte[] data)
        {
            LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);
            OnJoiningLobby?.Invoke(data);
        }
        
        private void ProcessRestartPacket(byte[] data)
        {
            RestartPacket restartPacket = new RestartPacket().Deserialize(data);
            OnRestart?.Invoke(restartPacket.isReset ? (byte)1 : (byte)0);
        }

        private void ProcessPushEventPacket(byte[] data)
        {
            PushEventPacket pushPacket = new PushEventPacket().Deserialize(data);
            
            if (pushPacket == null || pushPacket.playerData == null)
            {
                Debug.LogError("PacketHandler.cs: Invalid push packet");
                return;
            }

            Vector3 force = new Vector3(pushPacket.ForceX, pushPacket.ForceY, pushPacket.ForceZ);
            Debug.Log($"PacketHandler.cs: Processing push packet. Target: {pushPacket.TargetPlayerTag}, Force: {force}");

            OnPushEventReceived?.Invoke(pushPacket.TargetPlayerTag, force, pushPacket.EffectName ?? string.Empty);
        }
        
        private void ProcessReadyInLobbyPacket(byte[] data)
        {
            ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket().Deserialize(data);
            OnPlayerReadyStateChanged?.Invoke(readyPacket.playerData, readyPacket.isPlayerReady);
            Debug.Log($"PacketHandler.cs: Player {readyPacket.playerData.name} (tag: {readyPacket.playerData.tag}) ready state: {readyPacket.isPlayerReady}");
        }

        public byte[] CreatePushEventPacket(int targetPlayerTag, Vector3 force, string effectName)
        {
            PushEventPacket pushPacket = new PushEventPacket(_localPlayerData, targetPlayerTag, force, effectName);
            return pushPacket.Serialize();
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

        public byte[] CreateHeartbeatPacket(byte playerHeartbeat)
        {
            HeartbeatPacket heartbeatPacket = new HeartbeatPacket(_localPlayerData, playerHeartbeat);
            return heartbeatPacket.Serialize();
        }
        
        public byte[] CreateLobbyPacket()
        {
            LobbyPacket lobbyPacket = new LobbyPacket(_localPlayerData);
            return lobbyPacket.Serialize();
        }
        
        public byte[] CreateRestartPacket(bool reset)
        {
            RestartPacket restartPacket = new RestartPacket(_localPlayerData, reset);
            return restartPacket.Serialize();
        }
        
        public byte[] CreateReadyInLobbyPacket(bool isReady)
        {
            ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket(_localPlayerData, isReady);
            return readyPacket.Serialize();
        }

        public PlayerData GetLocalPlayerData()
        {
            return _localPlayerData;
        }
        
        public PlayerData GetPlayerDataByTag(int tag)
        {
            if (_knownPlayers.ContainsKey(tag))
            {
                return _knownPlayers[tag];
            }
            return null;
        }
    }
}