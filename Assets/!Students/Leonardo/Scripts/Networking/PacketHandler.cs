using System;
using System.Collections.Generic;
using System.Resources;
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
using UnityEngine.SceneManagement;

namespace Leonardo.Scripts.Networking
{
    public class PacketHandler
    {
        public event Action<string, string> OnMessageReceived;
        public event Action<PlayerPositionData> OnPositionReceived;
        public event Action OnPingResponseReceived;
        public event Action<int, Vector3, string> OnPushEventReceived;
        public event Action<PlayerData, bool> OnPlayerReadyStateChanged;

        //Hamad: adding event for HeartBeatReceived and Restart
        public event Action OnHeartbeat;
        public event Action<byte> OnHeartbeatReceived;

        public event Action<byte> OnRestart;

        public event Action<byte[]> OnJoiningLobby; 
        private PlayerData _localPlayerData;

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
                            Debug.Log("PacketHandler.cs: Attempting to process push event packet");
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
                            Debug.LogError("No heartbeat byte found");
                        }
                        break;
                    case Packet.PacketType.Restart:
                        try
                        {
                            ProcessRestartPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("No restart packet found");
                        }
                        break;
                    case Packet.PacketType.JoinLobby:
                        try
                        {
                            ProcessJoiningLobby(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("No lobby found");
                        }
                        break;
                    case Packet.PacketType.ReadyInLobby:
                        try
                        {
                            ProcessReadyInLobbyPacket(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error processing ready state packet: {e.Message}");
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

        //Hamad: adding the heartbeat and restart action
        private void ProcessHeartbeatPacket(byte[] data)
        {

            byte heartbeat = data[0];
            if (data != null) {
                HeartbeatPacket heartbeatPacket = new HeartbeatPacket().Deserialize(data);
                OnHeartbeatReceived?.Invoke(heartbeat);
                OnHeartbeat?.Invoke();
            }
        }

        private void ProcessJoiningLobby(byte[] data)
        {
            byte lobby = data[0];

            if (data != null)
            {
                LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);
                OnJoiningLobby?.Invoke(data);
            }
        }
        private void ProcessRestartPacket(byte[] data)
        {

            byte reset = data[0];
            bool isReset = reset != 0;

            if (data != null)
            {
                RestartPacket restartPacket = new RestartPacket().Deserialize(data);
                OnRestart?.Invoke(reset);
            }
        }

        private void ProcessPushEventPacket(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                //Debug.LogError("PacketHandler.cs: Null data in ProcessPushEventPacket");
                return;
            }

            try
            {
                PushEventPacket pushPacket = new PushEventPacket();
                pushPacket = pushPacket.Deserialize(data);

                if (pushPacket == null)
                {
                    //Debug.LogError("PacketHandler.cs: Failed to deserialize push packet");
                    return;
                }

                if (pushPacket.playerData == null)
                {
                    //Debug.LogError("PacketHandler.cs: Push packet has null player data");
                    return;
                }

                Vector3 force = new Vector3(pushPacket.ForceX, pushPacket.ForceY, pushPacket.ForceZ);
                Debug.Log($"PacketHandler.cs: Successfully processed push packet. Target: {pushPacket.TargetPlayerTag}, Force: {force}");

                if (OnPushEventReceived != null)
                {
                    OnPushEventReceived.Invoke(pushPacket.TargetPlayerTag, force, pushPacket.EffectName ?? string.Empty);
                }
                else
                {
                    //Debug.LogWarning("PacketHandler.cs: No listeners for OnPushEventReceived event");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PacketHandler.cs: Error in ProcessPushEventPacket: {e.Message}\n{e.StackTrace}");
            }
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

        //Hamad: Creating function for heartbeat

        public byte[] CreateHeartbeatPacket(byte playerHeartbeat)
        {
            HeartbeatPacket heartbeatPacket = new HeartbeatPacket(_localPlayerData, playerHeartbeat);
            return heartbeatPacket.Serialize();

        }
        
        //Dyson: Create function for lobby

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
        
        private void ProcessReadyInLobbyPacket(byte[] data)
        {
            ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket().Deserialize(data);
            OnPlayerReadyStateChanged?.Invoke(readyPacket.playerData, readyPacket.isPlayerReady);
            Debug.Log($"Player {readyPacket.playerData.name} ready state: {readyPacket.isPlayerReady}");
        }

        public byte[] CreateReadyInLobbyPacket(bool isReady)
        {
            ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket(_localPlayerData, isReady);
            return readyPacket.Serialize();
        }

    }
}