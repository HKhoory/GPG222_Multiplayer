using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Dyson.GPG222.Lobby;
using Hamad.Scripts;
using Hamad.Scripts.Heartbeat;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Restart;
using Hamad.Scripts.Rotation;
using Leonardo.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using Leonardo.Scripts.Packets;
using UnityEngine.SceneManagement;

namespace Dyson_GPG222_Server
{
    public class Server : MonoBehaviour
    {
        [Header("- Server Configuration")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private int port = 2121;
        [SerializeField] private float serverHeartbeatInterval = 2.0f; // Time in seconds

        private static Server instance;
        private static int MaxPlayers;
        private static int Port;

        private static TcpListener tcpListener;
        private static Dictionary<int, ClientHandler> clients = new Dictionary<int, ClientHandler>();
        private static Dictionary<int, PlayerData> connectedPlayers = new Dictionary<int, PlayerData>();

        private float nextHeartbeatTime = 0f;

        void Awake()
        {
            enabled = false;
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }

        void Update()
        {
            if (Time.time >= nextHeartbeatTime)
            {
                SendHeartbeatsToClients();
                nextHeartbeatTime = Time.time + serverHeartbeatInterval;
            }
        }


        public void StartServer()
        {
            if (tcpListener != null)
            {
                Debug.LogWarning("Server is already running!");
                return;
            }

            MaxPlayers = maxPlayers;
            Port = port;
            Debug.Log($"Server started on {Port}");

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }

        private static void InitializeServerData()
        {
            clients.Clear();
            connectedPlayers.Clear();
            Debug.Log($"Server.cs: Initialized server data.");
        }

        private void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client;
            try
            {
                 client = tcpListener.EndAcceptTcpClient(result);
            }
            catch (ObjectDisposedException)
            {
                 Debug.LogWarning("Server.cs: Listener closed, accepting no more connections.");
                 return;
            }

            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            string endpoint = client.Client.RemoteEndPoint.ToString();
            Debug.LogWarning($"Server.cs: New player connected from endpoint: {endpoint}");

            lock (clients)
            {
                 bool alreadyConnected = false;
                 int existingClientId = -1;
                 foreach (var existingClient in clients)
                 {
                     if (existingClient.Value.Socket != null &&
                         existingClient.Value.Socket.Connected &&
                         existingClient.Value.Socket.Client.RemoteEndPoint.ToString() == endpoint)
                     {
                         alreadyConnected = true;
                         existingClientId = existingClient.Key;
                         break;
                     }
                 }

                 if (alreadyConnected)
                 {
                     Debug.LogWarning($"Client from {endpoint} is already connected with ID: {existingClientId}");
                     client.Close();
                     return;
                 }

                 for (int i = 1; i <= MaxPlayers; i++)
                 {
                     if (!clients.ContainsKey(i))
                     {
                         clients[i] = new ClientHandler(i, client);

                         string clientIds = string.Join(", ", clients.Keys);
                         Debug.LogWarning($"Server.cs: Assigned client {endpoint} to slot {i}. Total clients: {clients.Count}. All client IDs: {clientIds}");

                         HandleClient(client, i);
                         return;
                     }
                 }
            }

            Debug.LogWarning($"Server.cs: Server full. Current clients: {clients.Count}. Disconnecting {endpoint}");
            // Consider sending a "Server Full" message before closing
            client.Close();
        }

        private static void HandleClient(TcpClient client, int clientId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                stream.BeginRead(buffer, 0, buffer.Length, ReceiveCallback,
                    new ClientState
                    {
                        Client = client,
                        ClientId = clientId,
                        Buffer = buffer
                    });
            }
            catch (Exception e)
            {
                Debug.LogError($"Server.cs: Error setting up client handler for ID {clientId}: {e.Message}");
                RemoveClient(clientId);
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            ClientState state = (ClientState)ar.AsyncState;
            int clientId = state.ClientId;
            TcpClient client = state.Client;
            NetworkStream stream = null;

            try
            {
                if (!client.Connected)
                {
                    Debug.LogWarning($"Server.cs client: {clientId} found disconnected before EndRead.");
                    RemoveClient(clientId);
                    return;
                }

                stream = client.GetStream();
                int bytesRead = stream.EndRead(ar);

                if (bytesRead <= 0)
                {
                    Debug.LogWarning($"Server.cs client: {clientId} disconnected (0 bytes read).");
                    RemoveClient(clientId);
                    return;
                }

                byte[] data = new byte[bytesRead];
                Array.Copy(state.Buffer, data, bytesRead);

                // *** Process the received data ***
                instance.ProcessReceivedData(data, clientId); // Call instance method

                // Continue reading
                stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReceiveCallback, state);
            }
            catch (System.IO.IOException ioEx)
            {
                 Debug.LogWarning($"Server.cs IO Error reading data from client {clientId}: {ioEx.Message}. Client likely disconnected forcefully.");
                 RemoveClient(clientId);
            }
            catch (ObjectDisposedException)
            {
                 Debug.LogWarning($"Server.cs Socket/Stream disposed for client {clientId} during read. Client likely disconnected.");
                 RemoveClient(clientId);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server.cs: Error reading data from client {clientId}: {e.GetType()} - {e.Message}\n{e.StackTrace}");
                RemoveClient(clientId);
            }
        }

         private static void RemoveClient(int clientId)
         {
             lock (clients)
             {
                 if (clients.ContainsKey(clientId))
                 {
                     try { clients[clientId].Socket?.Close(); } catch { }
                     clients.Remove(clientId);
                     Debug.Log($"Removed client {clientId} from clients dictionary.");
                 }
             }
             lock(connectedPlayers)
             {
                 if (connectedPlayers.ContainsKey(clientId))
                 {
                     PlayerData removedPlayerData = connectedPlayers[clientId];
                     connectedPlayers.Remove(clientId);
                     Debug.Log($"Removed player data for client {clientId}.");
                     // TODO: Broadcast PlayerDisconnected packet here
                     // Example using MessagePacket:
                     // MessagePacket disconnectMsg = new MessagePacket(new PlayerData("Server", 0), $"PLAYER_DISCONNECTED:{clientId}:{removedPlayerData?.name ?? "Unknown"}");
                     // Broadcast(Packet.PacketType.Message, disconnectMsg.Serialize(), 0); // 0 indicates server message
                 }
             }
             Debug.LogWarning($"Client {clientId} fully removed.");
         }


        private void ProcessReceivedData(byte[] data, int clientId)
        {
            try
            {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                lock(connectedPlayers) // Lock access to connectedPlayers
                {
                    // Update or add player data upon receiving any valid packet if they exist
                    if(basePacket.playerData != null && basePacket.playerData.tag != 0) // Ensure packet has valid player data
                    {
                        if (!connectedPlayers.ContainsKey(clientId))
                        {
                             Debug.LogWarning($"Server.cs: Adding player data for client {clientId}, Name: {basePacket.playerData.name}, Tag: {basePacket.playerData.tag}");
                             connectedPlayers[clientId] = basePacket.playerData;
                             // Send existing players' info only if this isn't a simple heartbeat etc.
                             if(basePacket.packetType != Packet.PacketType.Heartbeat)
                             {
                                 SendAllPlayersInfo(clientId);
                             }
                        }
                        else
                        {
                            // Optionally update existing PlayerData if needed (e.g., name change packet)
                            // connectedPlayers[clientId] = basePacket.playerData;
                        }
                    }
                }


                switch (basePacket.packetType)
                {
                    case Packet.PacketType.Message:
                        MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received message from {messagePacket.playerData?.name ?? "Unknown"}: {messagePacket.Message}");
                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.PlayersPositionData:
                        PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.PlayersRotationData:
                        PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.Ping:
                        PingPacket pingPacket = new PingPacket().Deserialize(data);
                        PingResponsePacket responsePacket = new PingResponsePacket(pingPacket.playerData, pingPacket.Timestamp);
                        SendToClient(clientId, responsePacket.Serialize());
                        break;

                    case Packet.PacketType.PushEvent:
                        PushEventPacket pushPacket = new PushEventPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received push event from {clientId} targeting player with tag {pushPacket.TargetPlayerTag}");
                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.Heartbeat:
                        // Received heartbeat from client, log it maybe. Reset client-specific timeout if implemented.
                        // Debug.Log($"Server.cs: Received heartbeat from client {clientId}");
                        break;

                    case Packet.PacketType.JoinLobby:
                        LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Client {clientId} ({lobbyPacket.playerData?.name}) sent JoinLobby packet.");
                        // Ensure player data is stored (should happen above)
                         lock(connectedPlayers) {
                             if (!connectedPlayers.ContainsKey(clientId) && lobbyPacket.playerData != null) {
                                 connectedPlayers[clientId] = lobbyPacket.playerData;
                                 SendAllPlayersInfo(clientId); // Send existing player info
                             }
                         }
                         // Broadcast the JoinLobby packet so clients know who joined
                        Broadcast(basePacket.packetType, data, clientId);
                        // TODO: Potentially update server-side lobby state if needed
                        break;

                    case Packet.PacketType.ReadyInLobby:
                         ReadyInLobbyPacket readyPacket = new ReadyInLobbyPacket().Deserialize(data);
                         Debug.Log($"Server.cs: Received ReadyInLobby state '{readyPacket.isPlayerReady}' from client {clientId} ({readyPacket.playerData?.name})");
                         // Broadcast the ready state change to all clients
                         Broadcast(basePacket.packetType, data, clientId);
                         // TODO: Check if all players are ready on the server if the server manages game start
                         break;

                    case Packet.PacketType.Restart:
                         RestartPacket restartPacket = new RestartPacket().Deserialize(data);
                         Debug.Log($"Server.cs: Received Restart packet from client {clientId}");
                         Broadcast(basePacket.packetType, data, clientId);
                         break;


                    default:
                        Debug.Log($"Server.cs: Unknown or unhandled packet type received: {basePacket.packetType} from client {clientId}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server.cs: Error processing data from client {clientId}: {e.GetType()} - {e.Message}\n{e.StackTrace}");
            }
        }

        private static void SendAllPlayersInfo(int newClientId)
        {
            lock(connectedPlayers)
            {
                if (connectedPlayers.Count <= 1) return;

                List<PlayerPositionData> allPlayersPosition = new List<PlayerPositionData>();

                foreach (var playerEntry in connectedPlayers)
                {
                    if (playerEntry.Key == newClientId) continue;

                    // Use a default position or fetch last known if available
                    PlayerPositionData playerPositionData = new PlayerPositionData(
                        playerEntry.Value, 0, 1, 0); // Default position
                    allPlayersPosition.Add(playerPositionData);
                }

                if (allPlayersPosition.Count > 0 && connectedPlayers.ContainsKey(newClientId))
                {
                    PlayersPositionDataPacket positionDataPacket =
                        new PlayersPositionDataPacket(connectedPlayers[newClientId], allPlayersPosition);
                    byte[] data = positionDataPacket.Serialize();
                    SendToClient(newClientId, data);
                    Debug.Log($"Server.cs: Sending data about {allPlayersPosition.Count} players to new client {newClientId}");
                }
            }
        }

        private static void SendToClient(int clientId, byte[] data)
        {
            lock (clients)
            {
                 if (clients.TryGetValue(clientId, out ClientHandler clientHandler) && clientHandler.Socket != null && clientHandler.Socket.Connected)
                 {
                     try
                     {
                         NetworkStream stream = clientHandler.Socket.GetStream();
                         stream.Write(data, 0, data.Length);
                     }
                     catch (Exception e)
                     {
                         Debug.LogError($"Server.cs: Error sending data to client {clientId}: {e.Message}");
                         RemoveClient(clientId);
                     }
                 }
            }
        }


        private static void Broadcast(Packet.PacketType packetType, byte[] data, int senderId)
        {
            lock (clients)
            {
                // Create a temporary list of client IDs to iterate over to avoid issues if RemoveClient modifies the dictionary during iteration
                 List<int> clientIds = new List<int>(clients.Keys);

                 foreach (int clientId in clientIds)
                 {
                      // Skip sender unless it's a packet type that should be echoed (e.g., maybe global announcements)
                     if (clientId == senderId) continue;

                     if (clients.TryGetValue(clientId, out ClientHandler clientHandler) && clientHandler.Socket != null && clientHandler.Socket.Connected)
                     {
                         try
                         {
                             NetworkStream stream = clientHandler.Socket.GetStream();
                             stream.Write(data, 0, data.Length);
                         }
                         catch (Exception e)
                         {
                             Debug.LogError($"Server.cs: Error broadcasting data to client {clientId}: {e.Message}");
                             // Consider removing the client if the broadcast fails repeatedly or due to critical errors
                             // RemoveClient(clientId); // Be cautious with removing clients during broadcast iteration
                         }
                     }
                 }
            }
        }

        private void SendHeartbeatsToClients()
        {
             lock (clients)
             {
                 if (clients.Count == 0) return;

                 HeartbeatPacket heartbeatPacket = new HeartbeatPacket(new PlayerData("Server", 0), 0x01); // Server heartbeat
                 byte[] data = heartbeatPacket.Serialize();

                 List<int> clientIds = new List<int>(clients.Keys);

                 foreach (int clientId in clientIds)
                 {
                     if (clients.TryGetValue(clientId, out ClientHandler clientHandler) && clientHandler.Socket != null && clientHandler.Socket.Connected)
                     {
                         try
                         {
                              NetworkStream stream = clientHandler.Socket.GetStream();
                              stream.Write(data, 0, data.Length);
                             // Debug.Log($"Sent heartbeat to client {clientId}");
                         }
                         catch (Exception e)
                         {
                             Debug.LogWarning($"Server.cs: Error sending heartbeat to client {clientId}: {e.Message}. Removing client.");
                             RemoveClient(clientId); // Remove client if heartbeat send fails
                         }
                     }
                 }
             }
        }


        void OnDestroy()
        {
            StopServer();
        }

        void OnApplicationQuit()
        {
             StopServer();
        }

        public void StopServer()
        {
             if (tcpListener != null)
             {
                  tcpListener.Stop();
                  tcpListener = null;
                  Debug.Log("Server stopped.");
             }

             lock (clients)
             {
                  foreach (var client in clients.Values)
                  {
                      try { client.Socket?.Close(); } catch { }
                  }
                  clients.Clear();
                  connectedPlayers.Clear();
                  Debug.Log("All client connections closed and cleared.");
             }
        }
    }
}