/*using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;
using Leonardo.Scripts.Controller;
using Leonardo.Scripts.Networking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// This script handles the client-side networking functionality, connections, instantiation, etc.
    /// </summary>
    public class Client : MonoBehaviour
    {
        [Header("- Connection Settings")] [SerializeField]
        private string playerNamePrefix = "Client-Player";

        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private bool connectionOnStart = true;
        [SerializeField] private int port = 2121;

        [Header("- Players Settings")] [SerializeField]
        private GameObject playerPrefab;

        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1, 0);
        [Header("- Network Settings")] [SerializeField]
        private float updateInterval = .1f;

        public PlayerData localPlayer { get; private set; }
        private Socket socket;
        private Dictionary<int, GameObject> _playerObjects = new Dictionary<int, GameObject>();
        private float nextUpdateTime;
        private bool isConnected;

        public bool IsConnected => isConnected;


        #region Unity Methods

        private void Start()
        {
            if (connectionOnStart)
            {
                // Try to connect to server.
                ConnectToServer($"{playerNamePrefix}{Random.Range(1, 9999)}");
            }
        }

        private void Update()
        {
            // Check for any incoming data from the server.
            ReceiveData();

            // Send position updates.
            SendPositionUpdates();
        }

        #endregion

        #region Script Specific Methods

        /// <summary>
        /// Checks for incoming data and processes it
        /// </summary>
        private void ReceiveData()
        {
            if (socket != null && socket.Connected && socket.Available > 0)
            {
                try
                {
                    byte[] buffer = new byte[socket.Available];
                    socket.Receive(buffer);

                    if (buffer.Length > 0)
                    {
                        HandlePacket(buffer);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                    {
                        Debug.LogError($"Client: Socket error: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Sends updates on position and rotation on intervals to not "clog" the network with data.
        /// </summary>
        private void SendPositionUpdates()
        {
            if (isConnected && Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;

                if (localPlayer != null && _playerObjects.ContainsKey(localPlayer.tag))
                {
                    SendPosition(_playerObjects[localPlayer.tag].transform.position);
                    //SendRotation(_playerObjects[localPlayer.tag].transform.eulerAngles);
                }
            }
        }

        /// <summary>
        /// Connects to the server with the given username.
        /// </summary>
        /// <param name="username">The username to connect with.</param>
        private void ConnectToServer(string username)
        {
            try
            {
                localPlayer = new PlayerData(username, Random.Range(0, 9999));

                // Create socket
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the server (blocking operation, but happens only once)
                socket.Connect(ipAddress, port);

                // Set to non-blocking mode for future operations
                socket.Blocking = false;

                if (socket.Connected)
                {
                    isConnected = true;
                    Debug.LogWarning($"Client: Connected to {ipAddress}:{port}.");

                    // Send initial "hello" message
                    var messagePacket = new MessagePacket(localPlayer, "Connected, hi!");
                    byte[] data = messagePacket.Serialize();
                    socket.Send(data);

                    // Spawn local player
                    SpawnLocalPlayer(localPlayer);
                }
                else
                {
                    Debug.LogError($"Client: Failed to connect to server.");
                }
            }
            catch (SocketException socketException)
            {
                Debug.LogWarning($"Client: Socket exception: {socketException.Message}");
            }
        }


        /// <summary>
        /// Spawns the LOCAL player.
        /// </summary>
        /// <param name="playerData">The player data that is going to be used.</param>
        private void SpawnLocalPlayer(PlayerData playerData)
        {
            Debug.LogWarning($"SPAWNING LOCAL PLAYER: {playerData.name} with tag {playerData.tag}");

            try
            {
                Vector3 position = new Vector3(0, 1, 0);
                GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
                _playerObjects[playerData.tag] = newPlayer;

                var controller = newPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetLocalplayer(true);
                    Debug.LogWarning($"Set player {playerData.name} as local player (BLUE)");
                }

                newPlayer.name = $"LocalPlayer_{playerData.name}";
                Debug.LogWarning($"Local player: {playerData.name} spawned at {position}");

                SendPosition(position);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error spawning player: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Updates or creates a player based on position data.
        /// </summary>
        /// <param name="playerPos">The player position data.</param>
        private void UpdateOrCreatePlayerPosition(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
    
            // Skip updates for our own player.
            if (playerTag == localPlayer.tag)
                return;
            
            if (!_playerObjects.ContainsKey(playerTag))
            {
                Debug.LogWarning($"CREATING REMOTE PLAYER: {playerPos.playerData.name} with tag {playerTag}");
        
                Vector3 position = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
                GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
                _playerObjects[playerTag] = newPlayer;
        
                var controller = newPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetLocalplayer(false);
                }
        
                newPlayer.AddComponent<RemotePlayerController>();
        
                newPlayer.name = $"RemotePlayer_{playerPos.playerData.name}";
            }
            else
            {
                // Update existing player's position target.
                Vector3 newPosition = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
                
                var remoteController = _playerObjects[playerTag].GetComponent<RemotePlayerController>();
                if (remoteController != null)
                {
                    remoteController.SetPositionTarget(newPosition);
                }
                else
                {
                    _playerObjects[playerTag].transform.position = newPosition;
                }
        
                Debug.LogWarning($"Updated position for existing player {playerPos.playerData.name} to {newPosition}");
            }
        }


        /// <summary>
        /// Handles deserializing the packet and running the logic depending on the type of packet that it is.
        /// </summary>
        /// <param name="data">The packet data converted to bytes.</param>
        private void HandlePacket(byte[] data)
        {
            try
            {
                Packet basePacket = new Packet();
                basePacket.Deserialize(data);

                switch (basePacket.packetType)
                {
                    case Packet.PacketType.Message:
                        MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                        Debug.Log(
                            $"Client: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                        break;

                    case Packet.PacketType.PlayersPositionData:
                        PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                        Debug.LogWarning(
                            $"Received position updates for {positionPacket.PlayerPositionData.Count} players from server");

                        foreach (var playerPos in positionPacket.PlayerPositionData)
                        {
                            Debug.LogWarning(
                                $"Processing position for player: {playerPos.playerData.name} (Tag: {playerPos.playerData.tag})");

                            // Skip updates for this client's player.
                            if (playerPos.playerData.tag == localPlayer.tag)
                            {
                                Debug.LogWarning($"Skipping own player position update");
                                continue;
                            }

                            UpdateOrCreatePlayerPosition(playerPos);
                        }

                        break;
                    
                    case Packet.PacketType.PingResponse:
                        PingResponsePacket pingResponsePacket = new PingResponsePacket().Deserialize(data);
    
                        PingMeter pingMeter = FindObjectOfType<PingMeter>();
                        if (pingMeter != null)
                        {
                            pingMeter.OnPingResponse();
                        }
    
                        //Debug.Log($"Client: Received ping response from server");
                        break;

                    default:
                        Debug.LogWarning($"Client: Unknown packet type received: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling packet: {e.Message}");
            }
        }

        #endregion

        #region Send (public) Methods

        /// <summary>
        /// Sends a message to all other connected clients.
        /// </summary>
        /// <param name="message">The message that is going to be sent.</param>
        public void SendMessagePacket(string message)
        {
            if (!isConnected || socket == null) return;

            try
            {
                MessagePacket messagePacket = new MessagePacket(localPlayer, message);
                byte[] data = messagePacket.Serialize();
                socket.Send(data);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError($"Client: Error sending message: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Sends the player position to all other clients in the server.
        /// </summary>
        /// <param name="position">The position to be sent.</param>
        public void SendPosition(Vector3 position)
        {
            if (!isConnected || socket == null) return;

            try
            {
                PlayerPositionData positionData = new PlayerPositionData(
                    localPlayer,
                    position.x,
                    position.y,
                    position.z
                );

                List<PlayerPositionData> playerPositionList = new List<PlayerPositionData> { positionData };

                PlayersPositionDataPacket positionPacket =
                    new PlayersPositionDataPacket(localPlayer, playerPositionList);
                byte[] data = positionPacket.Serialize();
                socket.Send(data);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError($"Client: Error sending position: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Sends the player rotation to all other clients in the server.
        /// </summary>
        /// <param name="rotation">The rotation to be sent</param>
        public void SendRotation(Vector3 rotation)
        {
            if (!isConnected || socket == null) return;

            try
            {
                PlayerRotationData rotationData = new PlayerRotationData(
                    localPlayer,
                    rotation.x,
                    rotation.y,
                    rotation.z
                );

                List<PlayerRotationData> playerRotationList = new List<PlayerRotationData> { rotationData };

                PlayersRotationDataPacket rotationPacket =
                    new PlayersRotationDataPacket(localPlayer, playerRotationList);
                byte[] data = rotationPacket.Serialize();
                socket.Send(data);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError($"Client: Error sending rotation: {e.Message}");
                }
            }
        }

        /// <summary>
        /// To check latency.
        /// </summary>
        public void SendPingPacket()
        {
            if (!isConnected || socket == null) return;
    
            try
            {
                PingPacket pingPacket = new PingPacket(localPlayer);
                byte[] data = pingPacket.Serialize();
                socket.Send(data);
        
                //Debug.LogWarning("Client: Sent ping packet to server");
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError($"Client: Error sending ping: {e.Message}");
                }
            }
        }

        #endregion


        /// <summary>
        /// This is just to clean up (i might have OCD)
        /// </summary>
        private void OnDestroy()
        {
            if (socket != null && socket.Connected)
            {
                socket.Close();
            }
        }
    }
}*/