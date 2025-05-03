using System;
using System.Collections.Generic;
using System.Text;
using __SAE.Leonardo.Scripts.Packets;
using Dyson.GPG222.Lobby;
using UnityEngine;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Heartbeat;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Controller;
using Leonardo.Scripts.Packets;
using Hamad.Scripts.Restart;

namespace Leonardo.Scripts.Networking
{
    /// <summary>
    /// Handles packet serialization, deserialization, and processing.
    /// Acts as the interface between network data and game functionality.
    /// </summary>
    public class PacketHandler
    {
        #region Event Declarations

        public event Action<string, string> OnMessageReceived;
        public event Action<PlayerPositionData> OnPositionReceived;
        public event Action OnPingResponseReceived;
        public event Action<int, Vector3, string> OnPushEventReceived;
        public event Action<PlayerData, bool> OnPlayerReadyStateChanged;
        public event Action OnHeartbeat;
        public event Action<byte> OnHeartbeatReceived;
        public event Action<byte> OnRestart;
        public event Action<byte[]> OnJoiningLobby;

        public event Action<Packet.PacketType, int> OnPacketReceived;
        public event Action<Packet.PacketType, string> OnPacketError;

        #endregion

        #region Private Fields

        private PlayerData _localPlayerData;
        private Dictionary<int, PlayerData> _knownPlayers = new Dictionary<int, PlayerData>();
        private bool _verboseLogging = false;
        private int _totalPacketsReceived = 0;
        private Dictionary<Packet.PacketType, int> _packetTypeCounter = new Dictionary<Packet.PacketType, int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new packet handler for the specified local player.
        /// </summary>
        /// <param name="localPlayerData">The local player data.</param>
        /// <param name="verboseLogging">Whether to enable verbose logging.</param>
        public PacketHandler(PlayerData localPlayerData, bool verboseLogging = false) {
            this._localPlayerData = localPlayerData;
            this._verboseLogging = verboseLogging;

            // Initialize counters for all packet types
            foreach (Packet.PacketType packetType in Enum.GetValues(typeof(Packet.PacketType))) {
                _packetTypeCounter[packetType] = 0;
            }

            LogInfo($"PacketHandler initialized for player {localPlayerData.name}");
        }

        #endregion

        #region Packet Processing Methods

        public event Action<List<LobbyStatePacket.LobbyPlayerInfo>> OnLobbyStateReceived;

        /// <summary>
        /// Process lobby state packets.
        /// </summary>
        /// <param name="data">The raw packet data.</param>
        private void ProcessLobbyStatePacket(byte[] data)
        {
            try
            {
                LobbyStatePacket statePacket = new LobbyStatePacket().Deserialize(data);
        
                if (_verboseLogging)
                {
                    LogInfo($"Received lobby state with {statePacket.Players.Count} players");
                }
        
                OnLobbyStateReceived?.Invoke(statePacket.Players);
            }
            catch (Exception e)
            {
                LogError($"Error processing lobby state packet: {e.Message}");
                OnPacketError?.Invoke(Packet.PacketType.LobbyState, e.Message);
            }
        }
        
        /// <summary>
        /// Processes a raw packet received from the network.
        /// </summary>
        /// <param name="data">The raw packet data.</param>
        public void ProcessPacket(byte[] data) {
            if (data == null || data.Length == 0) {
                LogWarning("Received null or empty data packet");
                return;
            }

            try {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                _totalPacketsReceived++;
                _packetTypeCounter[basePacket.packetType]++;

                if (_verboseLogging) {
                    LogInfo($"Received packet type: {basePacket.packetType}, Size: {data.Length} bytes");
                }

                // Notify diagnostic listeners.
                OnPacketReceived?.Invoke(basePacket.packetType, data.Length);

                // Cache player data from all packets to build a mapping between tags and client IDs.
                if (basePacket.playerData != null) {
                    _knownPlayers[basePacket.playerData.tag] = basePacket.playerData;
                }

                // ! Process by packet type.
                switch (basePacket.packetType) {
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
                        try {
                            ProcessPushEventPacket(data);
                        }
                        catch (Exception e) {
                            LogError($"Error processing push event: {e.Message}");
                            OnPacketError?.Invoke(Packet.PacketType.PushEvent, e.Message);
                        }

                        break;

                    case Packet.PacketType.Heartbeat:
                        try {
                            ProcessHeartbeatPacket(data);
                        }
                        catch (Exception e) {
                            LogError($"Error processing heartbeat: {e.Message}");
                            OnPacketError?.Invoke(Packet.PacketType.Heartbeat, e.Message);
                        }

                        break;

                    case Packet.PacketType.Restart:
                        try {
                            ProcessRestartPacket(data);
                        }
                        catch (Exception e) {
                            LogError($"Error processing restart packet: {e.Message}");
                            OnPacketError?.Invoke(Packet.PacketType.Restart, e.Message);
                        }

                        break;

                    case Packet.PacketType.JoinLobby:
                        try {
                            ProcessJoiningLobby(data);
                        }
                        catch (Exception e) {
                            LogError($"Error processing lobby packet: {e.Message}");
                            OnPacketError?.Invoke(Packet.PacketType.JoinLobby, e.Message);
                        }

                        break;

                    case Packet.PacketType.LobbyState:
                        LogInfo("[PacketHandler] Processing LobbyState packet type in switch.");
                        try 
                        {
                            ProcessLobbyStatePacket(data); 
                            LogInfo("[PacketHandler] Called ProcessLobbyStatePacket.");
                        } 
                        catch (Exception e) 
                        {
                            LogError($"[PacketHandler] Error calling ProcessLobbyStatePacket: {e.Message}\n{e.StackTrace}"); 
                            OnPacketError?.Invoke(Packet.PacketType.LobbyState, e.Message);
                        }
                        break;
                    
                    case Packet.PacketType.ReadyInLobby:
                        try {
                            ProcessReadyInLobbyPacket(data);
                        }
                        catch (Exception e) {
                            LogError($"Error processing ready state packet: {e.Message}");
                            OnPacketError?.Invoke(Packet.PacketType.ReadyInLobby, e.Message);
                        }

                        break;

                    default:
                        LogWarning($"Unknown packet type received: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e) {
                LogError($"Error processing packet: {e.Message}\n{e.StackTrace}");
                OnPacketError?.Invoke(Packet.PacketType.None, e.Message);
            }
        }

        private void ProcessMessagePacket(byte[] data) {
            try {
                MessagePacket messagePacket = new MessagePacket().Deserialize(data);

                if (messagePacket.playerData == null) {
                    LogWarning("Received message packet with null player data");
                    return;
                }

                if (_verboseLogging) {
                    LogInfo($"Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                }

                OnMessageReceived?.Invoke(messagePacket.playerData.name, messagePacket.Message);
            }
            catch (Exception e) {
                LogError($"Error processing message packet: {e.Message}");
                OnPacketError?.Invoke(Packet.PacketType.Message, e.Message);
            }
        }

        private void ProcessPositionPacket(byte[] data) {
            try {
                PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);

                if (positionPacket.PlayerPositionData == null) {
                    LogWarning("Received position packet with null position data");
                    return;
                }

                foreach (var playerPos in positionPacket.PlayerPositionData) {
                    // Validate player data
                    if (playerPos.playerData == null) {
                        LogWarning("Received position update with null player data");
                        continue;
                    }

                    // Skip updates for local player
                    if (playerPos.playerData.tag == _localPlayerData.tag) {
                        continue;
                    }

                    if (_verboseLogging) {
                        LogInfo(
                            $"Received position update for player {playerPos.playerData.name} at ({playerPos.xPos}, {playerPos.yPos}, {playerPos.zPos})");
                    }

                    OnPositionReceived?.Invoke(playerPos);
                }
            }
            catch (Exception e) {
                LogError($"Error processing position packet: {e.Message}");
                OnPacketError?.Invoke(Packet.PacketType.PlayersPositionData, e.Message);
            }
        }

        private void ProcessPingResponsePacket() {
            if (_verboseLogging) {
                LogInfo("Received ping response");
            }

            OnPingResponseReceived?.Invoke();
        }

        private void ProcessHeartbeatPacket(byte[] data) {
            HeartbeatPacket heartbeatPacket = new HeartbeatPacket().Deserialize(data);
            byte heartbeat = heartbeatPacket.heartbeat;

            if (_verboseLogging) {
                LogInfo($"Received heartbeat: {heartbeat}");
            }

            OnHeartbeatReceived?.Invoke(heartbeat);
            OnHeartbeat?.Invoke();
        }

        private void ProcessJoiningLobby(byte[] data) {
            LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);

            if (lobbyPacket.playerData == null) {
                LogWarning("Received lobby packet with null player data");
                return;
            }

            if (_verboseLogging) {
                LogInfo($"Received lobby join from player {lobbyPacket.playerData.name}");
            }

            OnJoiningLobby?.Invoke(data);
        }

        private void ProcessRestartPacket(byte[] data) {
            RestartPacket restartPacket = new RestartPacket().Deserialize(data);

            if (restartPacket.playerData == null) {
                LogWarning("Received restart packet with null player data");
                return;
            }

            LogInfo($"Received restart packet from {restartPacket.playerData.name}: Reset={restartPacket.isReset}");
            OnRestart?.Invoke(restartPacket.isReset ? (byte)1 : (byte)0);
        }

        private void ProcessPushEventPacket(byte[] data) {
            PushEventPacket pushPacket = new PushEventPacket().Deserialize(data);

            if (pushPacket == null || pushPacket.playerData == null) {
                LogError("Invalid push packet");
                return;
            }

            Vector3 force = new Vector3(pushPacket.ForceX, pushPacket.ForceY, pushPacket.ForceZ);
            LogInfo(
                $"Processing push packet from {pushPacket.playerData.name}. Target: {pushPacket.TargetPlayerTag}, Force: {force}");

            OnPushEventReceived?.Invoke(pushPacket.TargetPlayerTag, force, pushPacket.EffectName ?? string.Empty);
        }

        private void ProcessReadyInLobbyPacket(byte[] data) {
            ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket().Deserialize(data);

            if (readyPacket.playerData == null) {
                LogWarning("Received ready state packet with null player data");
                return;
            }

            LogInfo(
                $"Player {readyPacket.playerData.name} (tag: {readyPacket.playerData.tag}) ready state: {readyPacket.isPlayerReady}");
            OnPlayerReadyStateChanged?.Invoke(readyPacket.playerData, readyPacket.isPlayerReady);
        }

        #endregion

        #region Packet Creation Methods

        /// <summary>
        /// Creates a push event packet.
        /// </summary>
        /// <param name="targetPlayerTag">The tag of the target player.</param>
        /// <param name="force">The force to apply.</param>
        /// <param name="effectName">The name of the effect to play.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreatePushEventPacket(int targetPlayerTag, Vector3 force, string effectName) {
            ValidateLocalPlayerData();

            try {
                PushEventPacket pushPacket = new PushEventPacket(_localPlayerData, targetPlayerTag, force, effectName);
                byte[] data = pushPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created push event packet for player {targetPlayerTag} with force {force}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating push event packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a message packet.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreateMessagePacket(string message) {
            ValidateLocalPlayerData();

            try {
                MessagePacket messagePacket = new MessagePacket(_localPlayerData, message);
                byte[] data = messagePacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created message packet: {message}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating message packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a position packet.
        /// </summary>
        /// <param name="position">The position to send.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreatePositionPacket(Vector3 position) {
            ValidateLocalPlayerData();

            try {
                PlayerPositionData positionData = new PlayerPositionData(
                    _localPlayerData,
                    position.x,
                    position.y,
                    position.z
                );

                List<PlayerPositionData> playerPositionList = new List<PlayerPositionData> { positionData };
                PlayersPositionDataPacket positionPacket =
                    new PlayersPositionDataPacket(_localPlayerData, playerPositionList);
                byte[] data = positionPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created position packet: {position}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating position packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a ping packet.
        /// </summary>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreatePingPacket() {
            ValidateLocalPlayerData();

            try {
                PingPacket pingPacket = new PingPacket(_localPlayerData);
                byte[] data = pingPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo("Created ping packet");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating ping packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a heartbeat packet.
        /// </summary>
        /// <param name="playerHeartbeat">The heartbeat value.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreateHeartbeatPacket(byte playerHeartbeat) {
            ValidateLocalPlayerData();

            try {
                HeartbeatPacket heartbeatPacket = new HeartbeatPacket(_localPlayerData, playerHeartbeat);
                byte[] data = heartbeatPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created heartbeat packet: {playerHeartbeat}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating heartbeat packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a lobby packet.
        /// </summary>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreateLobbyPacket() {
            ValidateLocalPlayerData();

            try {
                Debug.Log($"[NAME DEBUG] PacketHandler.CreateLobbyPacket: Creating packet with player name: {_localPlayerData.name}");
                LobbyPacket lobbyPacket = new LobbyPacket(_localPlayerData);
                byte[] data = lobbyPacket.Serialize();
                return data;
            }
            catch (Exception e) {
                LogError($"Error creating lobby packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a restart packet.
        /// </summary>
        /// <param name="reset">Whether to reset the game.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreateRestartPacket(bool reset) {
            ValidateLocalPlayerData();

            try {
                RestartPacket restartPacket = new RestartPacket(_localPlayerData, reset);
                byte[] data = restartPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created restart packet: {reset}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating restart packet: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a ready in lobby packet.
        /// </summary>
        /// <param name="isReady">Whether the player is ready.</param>
        /// <returns>The serialized packet data.</returns>
        public byte[] CreateReadyInLobbyPacket(bool isReady) {
            ValidateLocalPlayerData();

            try {
                ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket(_localPlayerData, isReady);
                byte[] data = readyPacket.Serialize();

                if (_verboseLogging) {
                    LogInfo($"Created ready in lobby packet: {isReady}");
                }

                return data;
            }
            catch (Exception e) {
                LogError($"Error creating ready in lobby packet: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the local player data.
        /// </summary>
        /// <returns>The local player data.</returns>
        public PlayerData GetLocalPlayerData() {
            return _localPlayerData;
        }

        /// <summary>
        /// Gets a player's data by their tag.
        /// </summary>
        /// <param name="tag">The player's tag.</param>
        /// <returns>The player data, or null if not found.</returns>
        public PlayerData GetPlayerDataByTag(int tag) {
            if (_knownPlayers.ContainsKey(tag)) {
                return _knownPlayers[tag];
            }

            LogWarning($"No player found with tag {tag}");
            return null;
        }

        /// <summary>
        /// Gets packet statistics.
        /// </summary>
        /// <returns>A string containing packet statistics.</returns>
        public string GetPacketStats() {
            StringBuilder statsBuilder = new StringBuilder();
            statsBuilder.AppendLine($"Total packets received: {_totalPacketsReceived}");
            statsBuilder.AppendLine("Packet type counts:");

            foreach (var pair in _packetTypeCounter) {
                if (pair.Value > 0) {
                    statsBuilder.AppendLine($"  {pair.Key}: {pair.Value}");
                }
            }

            return statsBuilder.ToString();
        }

        /// <summary>
        /// Resets packet statistics.
        /// </summary>
        public void ResetPacketStats() {
            _totalPacketsReceived = 0;

            foreach (Packet.PacketType packetType in Enum.GetValues(typeof(Packet.PacketType))) {
                _packetTypeCounter[packetType] = 0;
            }

            LogInfo("Packet statistics reset");
        }

        /// <summary>
        /// Sets verbose logging.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        public void SetVerboseLogging(bool verbose) {
            _verboseLogging = verbose;
            LogInfo($"Verbose logging {(_verboseLogging ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Validates local player data.
        /// </summary>
        private void ValidateLocalPlayerData() {
            if (_localPlayerData == null) {
                throw new InvalidOperationException("Local player data is null");
            }
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message) {
            Debug.Log($"[PacketHandler] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogWarning(string message) {
            Debug.LogWarning($"[PacketHandler] WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message) {
            Debug.LogError($"[PacketHandler] ERROR: {message}");
        }

        #endregion
    }
}