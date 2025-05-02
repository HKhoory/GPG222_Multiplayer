using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Dyson.GPG222.Lobby;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
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
        // Leo: Added serializable fields so some things can be tweaked in the inspector.
        [Header("- Server Configuration")] [SerializeField]
        private int maxPlayers = 4;

        [SerializeField] private int port = 2121;

        private static Server instance;
        private static int MaxPlayers;
        private static int Port;

        private static TcpListener tcpListener;
        private static Dictionary<int, ClientHandler> clients = new Dictionary<int, ClientHandler>();
        private static Dictionary<int, PlayerData> connectedPlayers = new Dictionary<int, PlayerData>();
        public NetworkConnection networkConnection;
        public Lobby lobby;
        public JoinLobby _joinLobby;
        public bool isServerFull;

        void Awake() 
        {
            // Leo: this will be enabled only if a player is hosting.
            enabled = false;
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

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
    
            InitializeServerData();
        }

        private static void InitializeServerData()
        {
            clients.Clear();
            Debug.Log($"Server.cs: Initialized server data.");
        }

        private void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            string endpoint = client.Client.RemoteEndPoint.ToString();
            Debug.LogWarning($"Server.cs: New player connected from endpoint: {endpoint}");
            // Check if this client is already connected
            bool alreadyConnected = false;
            int existingClientId = -1;

            foreach (var existingClient in clients)
            {
                if (existingClient.Value.Socket != null &&
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

            // Find an empty slot for the client.
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (!clients.ContainsKey(i))
                {
                    // Leo: Create a new ClientHandler for this connection.
                    clients[i] = new ClientHandler(i, client);

                    string clientIds = string.Join(", ", clients.Keys);
                    Debug.LogWarning(
                        $"Server.cs: Assigned client {endpoint} to slot {i}. Total clients: {clients.Count}. All client IDs: {clientIds}");

                    HandleClient(client, i);
                    return;
                }
            }
            isServerFull = true;
            Debug.LogWarning($"Server.cs: Server full. Current clients: {clients.Count}");
            client.Close();
        }

        // LEO: New function to handle client data.
        private static void HandleClient(TcpClient client, int clientId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                // Begin reading.
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
                Debug.LogError($"Server.cs: Error setting up client handler: {e.Message}");
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            ClientState state = (ClientState)ar.AsyncState;
            try
            {
                NetworkStream stream = state.Client.GetStream();
                int bytesRead = stream.EndRead(ar);

                if (bytesRead <= 0)
                {
                    // The client disconnected.
                    Debug.LogWarning($"Server.cs client: {state.ClientId} disconnected.");

                    if (connectedPlayers.ContainsKey(state.ClientId))
                    {
                        connectedPlayers.Remove(state.ClientId);
                    }

                    clients.Remove(state.ClientId);
                    return;
                }

                byte[] data = new byte[bytesRead];
                Array.Copy(state.Buffer, data, bytesRead);

                stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server.cs: Error reading data: {e.Message}");
                if (clients.ContainsKey(state.ClientId))
                {
                    clients.Remove(state.ClientId);
                }
            }
        }

        private void ProcessReceivedData(byte[] data, int clientId)
        {
            try
            {
                Debug.Log("Received packet on server!");
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                switch (basePacket.packetType)
                {
                    case Packet.PacketType.Message:
                        MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                        Debug.Log(
                            $"Server.cs: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");

                        // Store this player's data when they first connect THIS IS FOR TESTING PURPOSES, THIS SHOULD BE CHANGED LATER TO NOT BE TOO SPECIFIC.
                        if (messagePacket.Message == "Connected, hi!" && !connectedPlayers.ContainsKey(clientId))
                        {
                            connectedPlayers[clientId] = messagePacket.playerData;

                            // Send information about all existing players to this new client
                            SendAllPlayersInfo(clientId);
                        }

                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.PlayersPositionData:
                        PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                        Debug.Log(
                            $"Server.cs: Received position updates for {positionPacket.PlayerPositionData.Count} players.");
                        Broadcast(basePacket.packetType, data, clientId);
                        break;

                    case Packet.PacketType.PlayersRotationData:
                        PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                        Debug.Log(
                            $"Server.cs: Received rotation updates for {rotationPacket.PlayerRotationData.Count} players.");
                        Broadcast(basePacket.packetType, data, clientId);
                        break;
                    
                    // Leo: This is to check the latency.
                    case Packet.PacketType.Ping:
                        PingPacket pingPacket = new PingPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received ping from {pingPacket.playerData.name}");
    
                        if (connectedPlayers.ContainsKey(clientId))
                        {
                            PingResponsePacket responsePacket = new PingResponsePacket(pingPacket.playerData, pingPacket.Timestamp);
                            byte[] responseData = responsePacket.Serialize();
        
                            // Send directly back to the one who sent it only.
                            if (clients.ContainsKey(clientId) && clients[clientId].Socket != null && clients[clientId].Socket.Connected)
                            {
                                try
                                {
                                    NetworkStream stream = clients[clientId].Socket.GetStream();
                                    stream.Write(responseData, 0, responseData.Length);
                                    //Debug.Log($"Server.cs: Sent ping response to {clientId}");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Server.cs: Error sending ping response: {e.Message}");
                                }
                            }
                        }
                        break;
                    
                    // Leo: this is for pushing
                    case Packet.PacketType.PushEvent:
                        PushEventPacket pushPacket = new PushEventPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received push event targeting player with tag {pushPacket.TargetPlayerTag}");
                        Broadcast(basePacket.packetType, data, clientId);
                        Debug.Log($"Received packet type: {basePacket.packetType}");
                        break;
                    
                    case Packet.PacketType.JoinLobby:
                        LobbyPacket lobbyPacket = new LobbyPacket().Deserialize(data);
                        Debug.Log("test test test");
                       /* ClientState newPlayer = new ClientState();
                        newPlayer.Client = clients[clientId].Socket;
                        newPlayer.ClientId = clientId;
                        lobby.AddPlayerToLobby(newPlayer);
                        connectedPlayers[clientId] = lobbyPacket._playerData; */
                      /* if (connectedPlayers.Count >= MaxPlayers)
                       {
                            Debug.Log("All players are in the lobby, we can start the game!");
                            SceneManager.LoadScene(1);
                        } */
                       // Debug.Log($"{newPlayer.ClientId} is joining the lobby!");
                        Broadcast(basePacket.packetType, data, clientId);
                        break;
                    default:
                        Debug.Log($"Server.cs: Unknown packet type received.");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server.cs: Error processing data: {e.Message}");
            }
        }
        
        private static void SendAllPlayersInfo(int newClientId)
        {
            // Don't send anything if its the first one since there is no one to receive.
            if (connectedPlayers.Count <= 1)
            {
                Debug.LogWarning($"No players to send info about to new client {newClientId}");
                return;
            }

            List<PlayerPositionData> allPlayersPosition = new List<PlayerPositionData>();
            Debug.LogWarning(
                $"Preparing to send info about {connectedPlayers.Count - 1} players to new client {newClientId}");


            foreach (var player in connectedPlayers)
            {
                if (player.Key == newClientId)
                {
                    continue;
                }

                // This is a default position that is going to be updated later by the client.
                PlayerPositionData playerPositionData = new PlayerPositionData(
                    player.Value, 0, 1, 0);

                allPlayersPosition.Add(playerPositionData);
            }

            if (allPlayersPosition.Count > 0)
            {
                // Create a packet with the information of all the positions of the players.
                PlayersPositionDataPacket positionDataPacket =
                    new PlayersPositionDataPacket(connectedPlayers[newClientId], allPlayersPosition);

                byte[] data = positionDataPacket.Serialize();

                // Get client's socket:
                if (clients.ContainsKey(newClientId) && clients[newClientId].Socket != null &&
                    clients[newClientId].Socket.Connected)
                {
                    try
                    {
                        NetworkStream stream = clients[newClientId].Socket.GetStream();
                        stream.Write(data, 0, data.Length);
                        Debug.Log($"Server.cs: Sending data about {allPlayersPosition.Count} players to {newClientId}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Server.cs: Error sending data about {allPlayersPosition.Count} players: {e.Message}");
                    }
                }
            }
        }

        // LEO: I'm adding this function so the server can broadcast to all clients.
        private static void Broadcast(Packet.PacketType packetType, byte[] data, int senderId)
        {
            {
                int clientCount = clients.Count;
                int otherClientsCount = clientCount - 1;

                string clientIds = string.Join(", ", clients.Keys);
                Debug.LogWarning(
                    $"Broadcasting data of type {packetType} from client {senderId}. Total clients: {clientCount}, other clients: {otherClientsCount}, client IDs: {clientIds}");

                foreach (var client in clients)
                {
                    try
                    {
                        Debug.LogWarning($"Broadcasting to client {client.Key} from sender {senderId}");
                        // Skip sender.
                        if (client.Key == senderId) continue;

                        if (client.Value.Socket != null && client.Value.Socket.Connected)
                        {
                            NetworkStream stream = client.Value.Socket.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Server.cs: Error broadcasting data: {e.Message}");
                    }
                }
            }
        }
    }
}