using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;

namespace Dyson_GPG222_Server
{
    public class Server : MonoBehaviour
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }

        private static TcpListener tcpListener;
        
        public static void Start(int maxPlayers, int port)
        {
            StartServer(maxPlayers, port);
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
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }
            Debug.Log(client.Client.RemoteEndPoint + " : Server full");
            client.Close();
        }

        // LEO: New function to HandleClient.
        private static void HandleClient(TcpClient client, int clientId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            
            stream.BeginRead(buffer, 0, buffer.Length, ar =>
            {
                try
                {
                    int bytesRead = stream.EndRead(ar);
                    if (bytesRead <= 0)
                    {
                        // Client disconnected.
                        clients.Remove(clientId);
                        Debug.LogWarning($"Server.cs: Client {clientId} disconnected.");
                        return;
                    }
                    
                    byte[] receiveData = new byte[bytesRead];
                    Array.Copy(buffer, receiveData, bytesRead);
                    
                    // Process received data.
                    ProcessReceivedData(receiveData, clientId);
                    
                    // Continue listening for more data.
                    stream.BeginRead(buffer, 0, buffer.Length, HandleReceiveCallback, new ClientReadState { Client = client, ClientId = clientId, Buffer = buffer });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Server.cs: Error reading data from client {clientId}: {e.Message}");
                    clients.Remove(clientId);
                }
            }, null);
        }

        private static void HandleReceiveCallback(IAsyncResult ar)
        {
            ClientReadState state = (ClientReadState)ar.AsyncState;
            try
            {
                NetworkStream stream = state.Client.GetStream();
                int bytesRead = stream.EndRead(ar);

                if (bytesRead <= 0)
                {
                    // The client disconnected.
                    clients.Remove(state.ClientId);
                    Debug.LogWarning($"Server.cs client: {state.ClientId} disconnected.");
                    return;
                }
                
                byte[] receiveData = new byte[bytesRead];
                Array.Copy(state.Buffer, receiveData, bytesRead);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ProcessReceivedData(byte[] data, int clientId)
        {
            Packet basePacket = new Packet();
            basePacket.Deserialize(data);

            switch (basePacket.packetType)
            {
                case Packet.PacketType.Message:
                    MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                    Debug.Log($"Server: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                    Broadcast(data);
                    break;

                case Packet.PacketType.PlayersPositionData:
                    PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                    Debug.Log($"Server: Received position updates for {positionPacket.PlayerPositionData.Count} players.");
                    Broadcast(data);
                    break;

                case Packet.PacketType.PlayersRotationData:
                    PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                    Debug.Log($"Server: Received rotation updates for {rotationPacket.PlayerRotationData.Count} players.");
                    Broadcast(data);
                    break;

                default:
                    Debug.Log($"Server: Unknown packet type received.");
                    break;
            }
        }
        
        // LEO: I'm adding this function so the server can broadcast to all clients.
        private static void Broadcast(byte[] data)
        {
            foreach (var client in clients.Values)
            {
                try
                {
                    if (client.Connected)
                    {
                        client.GetStream().Write(data, 0, data.Length);
                        Debug.Log("Server: Broadcasted data to client.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Server: Failed to broadcast data to a client. Error: {e.Message}");
                }
            }
        }
        
        private static void InitializeServerData()
        {
            for (int i = 0; i <= MaxPlayers; i++)
            {
                clients.Add(i, new TestClient(i));
            }
        }
    }
}
