    using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;
using Leonardo.Scripts;

namespace Dyson_GPG222_Server
{
    public class Server : MonoBehaviour
    {
        // Leo: Added serializable fields so some things can be tweaked in the inspector.
        [Header("- Server Configuration")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private int port = 2121;
        [SerializeField] private bool startServerOnAwake = true;

        private static Server instance;
        private static int MaxPlayers;
        private static int Port;

        private static TcpListener tcpListener;
        private static Dictionary<int, ClientHandler> clients = new Dictionary<int, ClientHandler>();


        private void Awake()
        {
            instance = this;
        }

        public  void Start()
        {
            if (startServerOnAwake)
            {
                StartServer(maxPlayers, port);
                InitializeServerData();
            }
        }

        // Leo: in case someone wants to start the server manually.
        public void StartServer()
        {
            StartServer(maxPlayers, port);
            InitializeServerData();
        }
        
        private static void InitializeServerData()
        {
            clients.Clear();
            Debug.Log($"Server.cs: Initialized server data.");
        }

        // Leo: turned code at start into a method.
        private static void StartServer(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;
            Debug.Log( $"Server started on {Port}");
            
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }
        
        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            
            Debug.Log($"Server.cs: New player connected: {client.Client.RemoteEndPoint}");
            
            // Find an empty slot for the client.
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients[i] = new ClientHandler(i, client);
                HandleClient(client, i);
                return;
            }
            Debug.Log(client.Client.RemoteEndPoint + " : Server full");
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
                    new ClientState { 
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
                    clients.Remove(state.ClientId);
                    return;
                }
                
                byte[] data = new byte[bytesRead];
                Array.Copy(state.Buffer, data, bytesRead);
                ProcessReceivedData(data, state.ClientId);
                
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

        private static void ProcessReceivedData(byte[] data, int clientId)
        {
            try
            {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                switch (basePacket.packetType)
                {
                    case Packet.PacketType.Message:
                        MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                        Broadcast(data, clientId);
                        break;

                    case Packet.PacketType.PlayersPositionData:
                        PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received position updates for {positionPacket.PlayerPositionData.Count} players.");
                        Broadcast(data, clientId);
                        break;

                    case Packet.PacketType.PlayersRotationData:
                        PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                        Debug.Log($"Server.cs: Received rotation updates for {rotationPacket.PlayerRotationData.Count} players.");
                        Broadcast(data, clientId);
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
        
        // LEO: I'm adding this function so the server can broadcast to all clients.
        private static void Broadcast(byte[] data, int senderId)
        {
            foreach (var client in clients)
            {
                try
                {
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
