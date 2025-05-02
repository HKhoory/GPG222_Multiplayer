/*using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Leonardo.Scripts
{
    /// <summary>
    /// This is a placeholder to test the functionality of my Client-related script(s).
    /// </summary>
    public class ServerPlaceholder : MonoBehaviour
    {
        [Header("- Server Settings")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private int serverPort = 2121;

        private Socket serverSocket;
        private List<Socket> clients = new List<Socket>();
        
        private Dictionary<int, PlayerData> playersInSession = new Dictionary<int, PlayerData>();

        #region Unity Methods
        private void Start()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
            serverSocket.Listen(10);
            serverSocket.Blocking = false;
            Debug.LogWarning("ServerPlaceholder.cs: Waiting for players...");
        }

        private void Update()
        {
            
            // Handle new player connections.
            try
            {
                Socket newClient = serverSocket.Accept();
                clients.Add(newClient);
                newClient.Blocking = false;
                Debug.Log("ServerPlaceholder.cs: Player connected!!");
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError(e.ToString());
                }
            }
            
            // Handle receiving the player data.
            foreach (var client in clients)
            {
                if (client.Available > 0)
                {
                    byte[] buffer = new byte[client.Available];
                    client.Receive(buffer);
                    
                    // CONTINUE DESERIALIZATION HERE----------------------
                    PlayerData receivedPlayer = DeserializePlayerData(buffer);
                    if (!playersInSession.ContainsKey(receivedPlayer.tag))
                    {
                        playersInSession[receivedPlayer.tag] = receivedPlayer;
                        Debug.LogWarning($"ServerPlaceholder.cs: Received player {receivedPlayer.name} #{receivedPlayer.tag}!");
                    }
                    else
                    {
                        playersInSession[receivedPlayer.tag].position = receivedPlayer.position;
                        playersInSession[receivedPlayer.tag].rotation = receivedPlayer.rotation;
                    }
                    
                    // Sync player data with all clients
                    byte[] data = SerializePlayerData(receivedPlayer);
                    foreach (var _client in clients)
                    {
                        _client.Send(data);
                    }
                }
            }
        }
        #endregion

        #region Script Specific Methods

        private byte[] SerializePlayerData(PlayerData playerData)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            {
                writer.Write(playerData.name);
                writer.Write(playerData.tag);
                writer.Write(playerData.position.x);
                writer.Write(playerData.position.y);
                writer.Write(playerData.position.z);
                writer.Write(playerData.rotation.x);
                writer.Write(playerData.rotation.y);
                writer.Write(playerData.rotation.z);
                writer.Write(playerData.rotation.w);
                return memoryStream.ToArray();
            }
        }
        
        private PlayerData DeserializePlayerData(byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream(buffer);
            BinaryReader reader = new BinaryReader(memoryStream);
            {
                string name = reader.ReadString();
                int tag = reader.ReadInt32();
                Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Quaternion rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                return new PlayerData(name, tag, position, rotation);
            }
        }

        #endregion
        
    }
}*/