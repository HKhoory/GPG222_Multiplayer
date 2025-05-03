using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using __SAE.Leonardo.Scripts.Packets;
using Dyson.GPG222.Lobby;
using Hamad.Scripts;
using Hamad.Scripts.Heartbeat;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Restart;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using Leonardo.Scripts.Packets;
using UnityEngine;

namespace __SAE.Dyson.Scripts
{
    /// <summary>
    /// Server component that manages client connections, packet processing,
    /// and broadcasting messages between clients.
    /// </summary>
    public class Server : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private int maxPlayers = 4;

        [SerializeField] private int port = 2121;
        [SerializeField] private float clientTimeoutSeconds = 10f;
        [SerializeField] private int maxConnectionsQueue = 10;

        [Header("Debug Settings")]
        [SerializeField] private bool verboseLogging = false;

        [SerializeField] private bool logPacketData = false;
        [SerializeField] private float statusUpdateInterval = 5f;

        private TcpListener tcpListener;
        private Dictionary<int, ClientHandler> clients = new Dictionary<int, ClientHandler>();
        private Dictionary<int, PlayerData> connectedPlayers = new Dictionary<int, PlayerData>();
        private Dictionary<int, float> clientLastActivity = new Dictionary<int, float>();

        private bool isServerRunning = false;
        private float nextStatusUpdateTime = 0f;
        private int totalPacketsReceived = 0;
        private int totalPacketsSent = 0;
        private bool shutdownRequested = false;
        private object clientsLock = new object();

        private Lobby.Lobby lobby;

        public enum ServerState
        {
            Stopped,
            Starting,
            Running,
            Error,
            ShuttingDown
        }

        private ServerState currentState = ServerState.Stopped;
        private string errorMessage = string.Empty;

        public bool IsServerFull => clients.Count >= maxPlayers;
        public bool IsRunning => isServerRunning;
        public ServerState CurrentState => currentState;
        public int ClientCount => clients.Count;
        public int MaxPlayers => maxPlayers;
        public int Port => port;

        private void Awake() {
            enabled = false;
            LogInfo("Server component initialized");
        }

        private void Start() {
            lobby = FindObjectOfType<Lobby.Lobby>();
            LogInfo("Server component ready");
        }

        private void Update() {
            if (!isServerRunning) return;

            CheckForNewConnections();
            PollClientsForData();
            CheckClientTimeouts();

            if (Time.time >= nextStatusUpdateTime) {
                nextStatusUpdateTime = Time.time + statusUpdateInterval;
                LogServerStatus();
            }
        }

        private void OnDestroy() {
            StopServer();
        }

        private void OnApplicationQuit() {
            StopServer();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer() {
            if (isServerRunning) {
                LogWarning("Server is already running!");
                return;
            }

            try {
                SetServerState(ServerState.Starting);
                LogInfo($"Starting server on port {Port}");

                tcpListener = new TcpListener(IPAddress.Any, Port);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start(maxConnectionsQueue);

                isServerRunning = true;
                SetServerState(ServerState.Running);

                InitializeServerData();

                LogInfo($"Server started successfully on port {Port}");
            }
            catch (Exception e) {
                LogError($"Failed to start server: {e.Message}");
                SetServerState(ServerState.Error);
                errorMessage = $"Failed to start server: {e.Message}";
                isServerRunning = false;
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer() {
            if (!isServerRunning) return;

            LogInfo("Stopping server...");
            SetServerState(ServerState.ShuttingDown);
            shutdownRequested = true;

            try {
                lock (clientsLock) {
                    foreach (var client in clients.Values) {
                        if (client.Socket != null && client.Socket.Connected) {
                            try {
                                client.Socket.Close();
                                LogInfo($"Closed connection to client {client.Id}");
                            }
                            catch (Exception e) {
                                LogWarning($"Error closing client {client.Id} connection: {e.Message}");
                            }
                        }
                    }

                    clients.Clear();
                    connectedPlayers.Clear();
                    clientLastActivity.Clear();
                }

                if (tcpListener != null) {
                    tcpListener.Stop();
                    tcpListener = null;
                    LogInfo("Stopped TCP listener");
                }

                isServerRunning = false;
                SetServerState(ServerState.Stopped);

                LogInfo("Server stopped successfully");
            }
            catch (Exception e) {
                LogError($"Error stopping server: {e.Message}");
                SetServerState(ServerState.Error);
                errorMessage = $"Error stopping server: {e.Message}";
            }
        }

        /// <summary>
        /// Initializes server data structures.
        /// </summary>
        private void InitializeServerData() {
            lock (clientsLock) {
                clients.Clear();
                connectedPlayers.Clear();
                clientLastActivity.Clear();
            }

            totalPacketsReceived = 0;
            totalPacketsSent = 0;

            LogInfo("Initialized server data");
        }

        /// <summary>
        /// Sets the server state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        private void SetServerState(ServerState newState) {
            if (currentState == newState) return;

            LogInfo($"Server state changing from {currentState} to {newState}");
            currentState = newState;
        }

        /// <summary>
        /// Logs the current server status.
        /// </summary>
        private void LogServerStatus() {
            if (!verboseLogging) return;

            LogInfo($"Server status: {currentState}, Clients: {clients.Count}/{maxPlayers}, " +
                    $"Packets received: {totalPacketsReceived}, Packets sent: {totalPacketsSent}");
        }

        /// <summary>
        /// Checks for new client connections.
        /// </summary>
        private void CheckForNewConnections() {
            try {
                if (tcpListener == null || !tcpListener.Pending())
                    return;

                TcpClient client = tcpListener.AcceptTcpClient();

                string endpoint = client.Client.RemoteEndPoint.ToString();
                LogInfo($"New connection from {endpoint}");

                if (IsServerFull) {
                    LogWarning($"Server full. Rejecting connection from {endpoint}");
                    client.Close();
                    return;
                }

                bool alreadyConnected = false;
                int existingClientId = -1;

                lock (clientsLock) {
                    foreach (var existingClient in clients) {
                        if (existingClient.Value.Socket != null &&
                            existingClient.Value.Socket.Client.RemoteEndPoint.ToString() == endpoint) {
                            alreadyConnected = true;
                            existingClientId = existingClient.Key;
                            break;
                        }
                    }
                }

                if (alreadyConnected) {
                    LogWarning($"Client from {endpoint} is already connected with ID: {existingClientId}");
                    client.Close();
                    return;
                }

                int clientId = AssignClientId();
                if (clientId == -1) {
                    LogWarning($"Could not assign client ID for {endpoint}. Server might be full.");
                    client.Close();
                    return;
                }

                client.NoDelay = true;
                client.ReceiveBufferSize = 4096;
                client.SendBufferSize = 4096;

                ClientHandler clientHandler = new ClientHandler(clientId, client);

                lock (clientsLock) {
                    clients[clientId] = clientHandler;
                    clientLastActivity[clientId] = Time.time;
                }

                LogInfo($"Assigned client {endpoint} to slot {clientId}. Total clients: {clients.Count}");
            }
            catch (Exception e) {
                if (shutdownRequested) {
                    return;
                }

                LogError($"Error accepting client connection: {e.Message}");
            }
        }

        /// <summary>
        /// Polls all clients for incoming data.
        /// </summary>
        private void PollClientsForData() {
            List<int> clientsToDisconnect = new List<int>();

            lock (clientsLock) {
                foreach (var clientPair in clients) {
                    int clientId = clientPair.Key;
                    ClientHandler client = clientPair.Value;

                    if (client == null || client.Socket == null || !client.Socket.Connected) {
                        clientsToDisconnect.Add(clientId);
                        continue;
                    }

                    try {
                        NetworkStream stream = client.Socket.GetStream();

                        if (client.Socket.Available > 0) {
                            byte[] data = new byte[client.Socket.Available];
                            int bytesRead = stream.Read(data, 0, data.Length);

                            if (bytesRead > 0) {
                                clientLastActivity[clientId] = Time.time;

                                byte[] receivedData = new byte[bytesRead];
                                Array.Copy(data, receivedData, bytesRead);

                                totalPacketsReceived++;

                                ProcessReceivedData(receivedData, clientId);
                            }
                            else {
                                clientsToDisconnect.Add(clientId);
                            }
                        }
                    }
                    catch (Exception e) {
                        LogError($"Error reading from client {clientId}: {e.Message}");
                        clientsToDisconnect.Add(clientId);
                    }
                }
            }

            foreach (int clientId in clientsToDisconnect) {
                DisconnectClient(clientId);
            }
        }

        /// <summary>
        /// Assigns a client ID from available slots.
        /// </summary>
        /// <returns>The assigned client ID, or -1 if no slots are available.</returns>
        private int AssignClientId() {
            lock (clientsLock) {
                for (int i = 1; i <= maxPlayers; i++) {
                    if (!clients.ContainsKey(i)) {
                        return i;
                    }
                }
            }

            return -1; // No slots available
        }

        /// <summary>
        /// Disconnects a client.
        /// </summary>
        /// <param name="clientId">The client ID to disconnect.</param>
        private void DisconnectClient(int clientId) {
            ClientHandler client = null;

            lock (clientsLock) {
                if (clients.TryGetValue(clientId, out client)) {
                    clients.Remove(clientId);
                }

                if (connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers.Remove(clientId);
                }

                if (clientLastActivity.ContainsKey(clientId)) {
                    clientLastActivity.Remove(clientId);
                }
            }

            if (client != null && client.Socket != null) {
                try {
                    if (client.Socket.Connected) {
                        client.Socket.Close();
                    }
                }
                catch (Exception e) {
                    LogWarning($"Error closing socket for client {clientId}: {e.Message}");
                }
            }

            LogInfo($"Client {clientId} has been disconnected");
        }

        /// <summary>
        /// Checks for client timeouts.
        /// </summary>
        private void CheckClientTimeouts() {
            float currentTime = Time.time;
            List<int> timeoutClients = new List<int>();

            lock (clientsLock) {
                foreach (var pair in clientLastActivity) {
                    int clientId = pair.Key;
                    float lastActivity = pair.Value;

                    if (currentTime - lastActivity > clientTimeoutSeconds) {
                        timeoutClients.Add(clientId);
                    }
                }
            }

            foreach (int clientId in timeoutClients) {
                LogWarning($"Client {clientId} timed out");
                DisconnectClient(clientId);
            }
        }

        /// <summary>
        /// Processes received data from a client.
        /// </summary>
        /// <param name="data">The received data.</param>
        /// <param name="clientId">The client ID that sent the data.</param>
        private void ProcessReceivedData(byte[] data, int clientId) {
            try {
                if (logPacketData) {
                    LogInfo($"Received {data.Length} bytes from client {clientId}");
                }

                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                if (verboseLogging) {
                    LogInfo($"Received packet type {basePacket.packetType} from client {clientId}");
                }

                switch (basePacket.packetType) {
                    case Packet.PacketType.Message:
                        ProcessMessagePacket(data, clientId);
                        break;

                    case Packet.PacketType.PlayersPositionData:
                        ProcessPositionPacket(data, clientId);
                        break;

                    case Packet.PacketType.PlayersRotationData:
                        ProcessRotationPacket(data, clientId);
                        break;

                    case Packet.PacketType.Ping:
                        ProcessPingPacket(data, clientId);
                        break;

                    case Packet.PacketType.PushEvent:
                        ProcessPushEventPacket(data, clientId);
                        break;

                    case Packet.PacketType.JoinLobby:
                        ProcessLobbyPacket(data, clientId);
                        break;

                    case Packet.PacketType.ReadyInLobby:
                        ProcessReadyInLobbyPacket(data, clientId);
                        break;

                    case Packet.PacketType.LobbyState:
                        ProcessLobbyStatePacket(data, clientId);
                        break;

                    case Packet.PacketType.FreezeEvent:
                        ProcessFreezeEventPacket(data, clientId);
                        break;

                    case Packet.PacketType.Heartbeat:
                        ProcessHeartbeatPacket(data, clientId);
                        break;

                    case Packet.PacketType.Restart:
                        ProcessRestartPacket(data, clientId);
                        break;

                    default:
                        LogWarning($"Unknown packet type received from client {clientId}: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e) {
                LogError($"Error processing data from client {clientId}: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Processes a heartbeat packet.
        /// </summary>
        private void ProcessHeartbeatPacket(byte[] data, int clientId) {
            try {
                HeartbeatPacket heartbeatPacket = new HeartbeatPacket().Deserialize(data);

                if (verboseLogging) {
                    LogInfo($"Received heartbeat from client {clientId}");
                }

                // Simply update the activity time
                clientLastActivity[clientId] = Time.time;
            }
            catch (Exception e) {
                LogError($"Error processing heartbeat packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a restart packet.
        /// </summary>
        private void ProcessRestartPacket(byte[] data, int clientId) {
            try {
                RestartPacket restartPacket = new RestartPacket().Deserialize(data);

                LogInfo($"Received restart request from client {clientId}");

                // Broadcast to all clients
                Broadcast(restartPacket.packetType, data, -1);
            }
            catch (Exception e) {
                LogError($"Error processing restart packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a message packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessMessagePacket(byte[] data, int clientId) {
            try {
                MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                LogInfo($"Received message from {messagePacket.playerData.name}: {messagePacket.Message}");

                if (!connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers[clientId] = messagePacket.playerData;
                    LogInfo($"Stored player data for client {clientId}: {messagePacket.playerData.name}");
                }

                // Critical change: Broadcast START_GAME to ALL clients (including sender)
                if (messagePacket.Message == "START_GAME") {
                    LogInfo($"Received START_GAME message from client {clientId}, broadcasting to ALL clients");
                    // Using -1 ensures all clients (including sender) receive the message
                    Broadcast(messagePacket.packetType, data, -1);
                }
                else if (messagePacket.Message == "Connected, hi!" && connectedPlayers.ContainsKey(clientId)) {
                    SendAllPlayersInfo(clientId);
                    Broadcast(messagePacket.packetType, data, clientId);
                }
                else {
                    // For regular messages, broadcast to everyone except sender
                    Broadcast(messagePacket.packetType, data, -1);
                }
            }
            catch (Exception e) {
                LogError($"Error processing message packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a position packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessPositionPacket(byte[] data, int clientId) {
            try {
                PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);

                if (verboseLogging) {
                    LogInfo(
                        $"Received position updates for {positionPacket.PlayerPositionData.Count} players from client {clientId}");
                }

                if (positionPacket.PlayerPositionData.Count > 0 &&
                    positionPacket.PlayerPositionData[0].playerData != null) {
                    connectedPlayers[clientId] = positionPacket.PlayerPositionData[0].playerData;
                }

                Broadcast(positionPacket.packetType, data, clientId);
            }
            catch (Exception e) {
                LogError($"Error processing position packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a rotation packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessRotationPacket(byte[] data, int clientId) {
            try {
                // We don't have a rotation packet class in the provided code, so this is simplified
                if (verboseLogging) {
                    LogInfo($"Received rotation packet from client {clientId}");
                }

                Broadcast(Packet.PacketType.PlayersRotationData, data, clientId);
            }
            catch (Exception e) {
                LogError($"Error processing rotation packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a freeze event packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessFreezeEventPacket(byte[] data, int clientId) {
            try {
                FreezeEventPacket freezePacket = new FreezeEventPacket().Deserialize(data);

                if (verboseLogging) {
                    LogInfo(
                        $"Received freeze event from client {clientId}, targeting player with tag {freezePacket.TargetPlayerTag}");
                }

                if (!connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers[clientId] = freezePacket.playerData;
                }

                Broadcast(freezePacket.packetType, data, clientId);
            }
            catch (Exception e) {
                LogError($"Error processing freeze event packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a lobby state packet.
        /// </summary>
        private void ProcessLobbyStatePacket(byte[] data, int clientId) {
            try {
                LobbyStatePacket statePacket = new LobbyStatePacket().Deserialize(data);

                LogInfo($"Received lobby state packet from client {clientId} with {statePacket.Players.Count} players");

                // Forward this packet to all clients.
                Broadcast(Packet.PacketType.LobbyState, data, clientId);
            }
            catch (Exception e) {
                LogError($"Error processing lobby state packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a ping packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessPingPacket(byte[] data, int clientId) {
            try {
                PingPacket pingPacket = new PingPacket().Deserialize(data);

                if (verboseLogging) {
                    LogInfo($"Received ping from client {clientId} ({pingPacket.playerData.name})");
                }

                if (!connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers[clientId] = pingPacket.playerData;
                }

                PingResponsePacket responsePacket = new PingResponsePacket(pingPacket.playerData, pingPacket.Timestamp);
                byte[] responseData = responsePacket.Serialize();

                SendToClient(clientId, responseData);
            }
            catch (Exception e) {
                LogError($"Error processing ping packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a push event packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessPushEventPacket(byte[] data, int clientId) {
            try {
                PushEventPacket pushPacket = new PushEventPacket().Deserialize(data);

                LogInfo(
                    $"Received push event from client {clientId}, targeting player with tag {pushPacket.TargetPlayerTag}");

                if (!connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers[clientId] = pushPacket.playerData;
                }

                Broadcast(pushPacket.packetType, data, clientId);
            }
            catch (Exception e) {
                LogError($"Error processing push event packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a lobby packet.
        /// </summary>
        private void ProcessLobbyPacket(byte[] data, int clientId) {
            try {
                LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);

                LogInfo($"Client {clientId} ({lobbyPacket.playerData.name}) is joining the lobby");
                connectedPlayers[clientId] = lobbyPacket.playerData;

                // Notify all clients about the new player.
                MessagePacket joinMessage = new MessagePacket(lobbyPacket.playerData, "JOIN_LOBBY");
                byte[] joinData = joinMessage.Serialize();
                Broadcast(Packet.PacketType.Message, joinData, -1);

                // Create a lobby state packet with all connected players.
                LobbyStatePacket statePacket = new LobbyStatePacket(new PlayerData("Server", 0));

                // Add all connected players to the state packet.
                foreach (var player in connectedPlayers) {
                    // Check if this player has a ready state (if they're in the lobby).
                    bool isReady = false;

                    // Add the player to the state packet.
                    statePacket.AddPlayer(player.Key, player.Value.name, isReady);
                }

                // Serialize and send the packet to the new client.
                byte[] stateData = statePacket.Serialize();
                SendToClient(clientId, stateData);

                LogInfo($"Sent current lobby state directly to client {clientId}");
            }
            catch (Exception e) {
                LogError($"Error processing lobby packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Processes a ready in lobby packet.
        /// </summary>
        /// <param name="data">The packet data.</param>
        /// <param name="clientId">The client ID that sent the packet.</param>
        private void ProcessReadyInLobbyPacket(byte[] data, int clientId) {
            try {
                ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket().Deserialize(data);

                LogInfo($"Client {clientId} ({readyPacket.playerData.name}) ready state: {readyPacket.isPlayerReady}");

                if (!connectedPlayers.ContainsKey(clientId)) {
                    connectedPlayers[clientId] = readyPacket.playerData;
                }

                // Critical fix: Broadcast ready state to ALL clients including sender
                Broadcast(readyPacket.packetType, data, -1);
            }
            catch (Exception e) {
                LogError($"Error processing ready state packet from client {clientId}: {e.Message}");
            }
        }

        /// <summary>
        /// Sends information about all connected players to a new client.
        /// </summary>
        /// <param name="newClientId">The client ID to send the information to.</param>
        private void SendAllPlayersInfo(int newClientId) {
            if (connectedPlayers.Count <= 1) {
                LogInfo($"No other players to send info about to new client {newClientId}");
                return;
            }

            List<PlayerPositionData> allPlayersPosition = new List<PlayerPositionData>();
            LogInfo($"Preparing to send info about {connectedPlayers.Count - 1} players to new client {newClientId}");

            foreach (var player in connectedPlayers) {
                if (player.Key == newClientId) {
                    continue;
                }

                PlayerPositionData playerPositionData = new PlayerPositionData(
                    player.Value, 0, 1, 0);

                allPlayersPosition.Add(playerPositionData);
            }

            if (allPlayersPosition.Count > 0 && connectedPlayers.ContainsKey(newClientId)) {
                PlayersPositionDataPacket positionDataPacket =
                    new PlayersPositionDataPacket(connectedPlayers[newClientId], allPlayersPosition);

                byte[] data = positionDataPacket.Serialize();

                SendToClient(newClientId, data);
            }
        }

        /// <summary>
        /// Broadcasts data to all clients except the sender.
        /// </summary>
        /// <param name="packetType">The packet type.</param>
        /// <param name="data">The data to broadcast.</param>
        /// <param name="senderId">The sender ID to exclude, or -1 to broadcast to all.</param>
        private void Broadcast(Packet.PacketType packetType, byte[] data, int senderId) {
            lock (clientsLock) {
                foreach (var client in clients) {
                    // Skip sender unless senderId is -1 (broadcast to all)
                    if (senderId != -1 && client.Key == senderId)
                        continue;

                    try {
                        SendToClient(client.Key, data);
                        if (verboseLogging) {
                            LogInfo($"Broadcasted {packetType} to client {client.Key}");
                        }
                    }
                    catch (Exception e) {
                        LogError($"Error broadcasting to client {client.Key}: {e.Message}");
                    }
                }
            }

            totalPacketsSent += (senderId == -1) ? clients.Count : clients.Count - 1;
        }

        /// <summary>
        /// Sends data to a specific client.
        /// </summary>
        /// <param name="clientId">The client ID to send to.</param>
        /// <param name="data">The data to send.</param>
        private void SendToClient(int clientId, byte[] data) {
            lock (clientsLock) {
                if (!clients.TryGetValue(clientId, out ClientHandler client)) {
                    LogWarning($"Cannot send to client {clientId}: client not found");
                    return;
                }

                if (client.Socket == null || !client.Socket.Connected) {
                    LogWarning($"Cannot send to client {clientId}: not connected");
                    return;
                }

                try {
                    NetworkStream stream = client.Socket.GetStream();
                    stream.Write(data, 0, data.Length);

                    totalPacketsSent++;

                    if (logPacketData) {
                        LogInfo($"Sent {data.Length} bytes to client {clientId}");
                    }
                }
                catch (Exception e) {
                    LogError($"Error sending data to client {clientId}: {e.Message}");
                    DisconnectClient(clientId);
                }
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message) {
            if (verboseLogging || message.Contains("error") || message.Contains("Error")) {
                Debug.Log($"[Server] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogWarning(string message) {
            Debug.LogWarning($"[Server] WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message) {
            Debug.LogError($"[Server] ERROR: {message}");
        }
    }
}