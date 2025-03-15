using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;
using Leonardo.Scripts.Controller;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// This script handles the client-side networking functionality, connections, instantiation, etc.
    /// </summary>
    public class Client : MonoBehaviour
    {
        [Header("- Connection Settings")] 
        [SerializeField] private string playerNamePrefix = "Client-Player";
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private bool connectionOnStart = true;
        [SerializeField] private int port = 2121;

        [Header("- Players Settings")] 
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1, 0);

        [Header("- Test Settings")] 
        [SerializeField] private bool enableTestMovement = true;
        [SerializeField] private float testMovementSpeed = 2f;
        
        public PlayerData localPlayer { get; private set; }
        public IReadOnlyDictionary<int, GameObject> playerObjects => new Dictionary<int, GameObject>(_playerObjects);

        private TcpClient socket;
        private NetworkStream stream;
        private Queue<PlayerPositionData> playerInstantiateQueue = new Queue<PlayerPositionData>();
        private Dictionary<int, GameObject> _playerObjects = new Dictionary<int, GameObject>();
        private PlayerData playerToSpawn = null;
        private byte[] receiveBuffer;
        private float nextUpdateTime = 0f;
        private float updateInterval = 0.1f;
        private bool isConnected = false;
        private bool shouldSpawnPlayer = false;
        private object queueLock = new object();

        #region Unity Methods

        private void Start()
        {
            if (connectionOnStart)
            {
                // Try to connect to server.
                ConnectToServer($"{playerNamePrefix}{Random.Range(1, 9999)}");
            }

            GameObject dispatchers = new GameObject("MainThreadDispatcher");
            dispatchers.AddComponent<UnityMainThreadDispatcher>();
        }

        private void Update()
        {
            // Process any queued remote player instantiations.
            lock (queueLock)
            {
                while (playerInstantiateQueue.Count > 0)
                {
                    var playerPos = playerInstantiateQueue.Dequeue();
                    InstantiatePlayer(playerPos);
                }
            }

            // Check if we need to spawn the local player.
            lock (this)
            {
                if (shouldSpawnPlayer && playerToSpawn != null)
                {
                    Debug.LogWarning($"Spawning local player from main thread: {playerToSpawn.name}");
                    SpawnLocalPlayer(playerToSpawn);
                    shouldSpawnPlayer = false;
                    playerToSpawn = null;
                }
            }

            // Send position updates.
            if (isConnected && Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;

                if (localPlayer != null && _playerObjects.ContainsKey(localPlayer.tag))
                {
                    SendPosition(_playerObjects[localPlayer.tag].transform.position);
                    SendRotation(_playerObjects[localPlayer.tag].transform.eulerAngles);
                    Debug.LogWarning($"Sending position update: {_playerObjects[localPlayer.tag].transform.position}");
                }
            }
        }

        #endregion

        #region Script Specific Methods
        
        // TODO: I should delete this.
        /// <summary>
        /// Manually connect to the server, needs player name as input.
        /// </summary>
        /// <param name="playerName">Player name to be set, if there is no name set a random one will be generated.</param>
        public void ManualConnect(string playerName = null)
        {
            if (playerName == null)
            {
                playerName = $"{playerNamePrefix}{Random.Range(1, 9999)}";
            }

            ConnectToServer(playerName);
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
                socket = new TcpClient();
                receiveBuffer = new byte[4096];

                socket.BeginConnect(ipAddress, port, ConnectCallback, socket);
                Debug.LogWarning($"Connecting to {ipAddress}:{port}");
            }
            catch (SocketException socketException)
            {
                Debug.LogWarning($"NetworkManager.cs, socket exception: {socketException.Message}");
            }
        }
        
        /// <summary>
        /// Callback for when the connection attempt completes.
        /// </summary>
        /// <param name="result">The result of the async operation.</param>
        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    Debug.LogError($"NetworkManger.cs: Client failed to connect.");
                    return;
                }

                stream = socket.GetStream();
                isConnected = true;
                Debug.LogWarning($"NetworkManger.cs: Client connected to {ipAddress}:{port}.");

                // Start reading from stream.
                stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, ReceiveCallback, null);

                // This is to test and send a Hello package to each person at first.
                var messagePacket = new MessagePacket(localPlayer, "Connected, hi!");
                byte[] data = messagePacket.Serialize();
                stream.Write(data, 0, data.Length);

                lock (this)
                {
                    shouldSpawnPlayer = true;
                    playerToSpawn = localPlayer;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
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

                // Send our position immediately to notify others
                SendPosition(position);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error spawning player: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Instantiates a remote player.
        /// </summary>
        /// <param name="playerPos">The position in which the player is going to be instantiated.</param>
        private void InstantiatePlayer(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;

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

                newPlayer.name = $"RemotePlayer_{playerPos.playerData.name}";
            }
        }

        /// <summary>
        /// Callback for when the data is received from the server.
        /// </summary>
        /// <param name="result">Result of the async operation.</param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                if (socket == null || !socket.Connected)
                {
                    return;
                }

                int byteLenght = stream.EndRead(result);
                if (byteLenght <= 0)
                {
                    return;
                }

                byte[] data = new byte[byteLenght];
                Array.Copy(receiveBuffer, data, byteLenght);

                HandlePacket(data);
                stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"NetworkManager.cs: Error in ReceiveCallback: {e.Message}");
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

                    case Packet.PacketType.PlayersRotationData:
                        PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                        Debug.Log(
                            $"Client: Received rotation updates for {rotationPacket.PlayerRotationData.Count} players");

                        foreach (var playerRot in rotationPacket.PlayerRotationData)
                        {
                            // Skip updates for our own player
                            if (playerRot.playerData.tag == localPlayer.tag)
                                continue;

                            UpdatePlayerRotation(playerRot);
                        }

                        break;

                    default:
                        Debug.Log($"Client: Unknown packet type received: {basePacket.packetType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Updates or creates a player based on position data.
        /// </summary>
        /// <param name="playerPos">The player position data.</param>
        private void UpdateOrCreatePlayerPosition(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
            Debug.LogWarning(
                $"UpdateOrCreatePlayerPosition called for player {playerPos.playerData.name} with tag {playerTag}, local player tag is {localPlayer.tag}");

            // If we don't have this player yet, queue it for instantiation on the main thread
            if (!_playerObjects.ContainsKey(playerTag))
            {
                lock (queueLock)
                {
                    Debug.LogWarning($"Queueing player {playerPos.playerData.name} for instantiation on main thread");
                    playerInstantiateQueue.Enqueue(playerPos);
                }
            }
            else
            {
                // Update existing player position.
                // This also needs to be queued for the main thread
                Vector3 newPosition = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);

                // Use Unity's main thread dispatcher
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_playerObjects.ContainsKey(playerTag))
                    {
                        _playerObjects[playerTag].transform.position = newPosition;
                        Debug.LogWarning(
                            $"Updated position for existing player {playerPos.playerData.name} to {newPosition}");
                    }
                });
            }
        }

        /// <summary>
        /// Updates player's rotation.
        /// </summary>
        /// <param name="playerRot">The player data of the player to be rotated.</param>
        private void UpdatePlayerRotation(PlayerRotationData playerRot)
        {
            int playerTag = playerRot.playerData.tag;

            // Only update rotation if we have this player.
            if (_playerObjects.ContainsKey(playerTag))
            {
                Vector3 newRotation = new Vector3(playerRot.xRot, playerRot.yRot, playerRot.zRot);

                // Use the dispatcher to update rotation on main thread.
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_playerObjects.ContainsKey(playerTag))
                    {
                        _playerObjects[playerTag].transform.rotation = Quaternion.Euler(newRotation);
                        Debug.LogWarning($"Updated rotation for player {playerRot.playerData.name} to {newRotation}");
                    }
                });
            }
        }
        #endregion
        
        
        // --- 'SEND' Methods. ---
        #region Send (public) Methods
        
        /// <summary>
        /// Sends a message to all other connected clients.
        /// </summary>
        /// <param name="message">The message that is going to be sent.</param>
        public void SendMessagePacket(string message)
        {
            MessagePacket messagePacket = new MessagePacket(localPlayer, message);
            byte[] data = messagePacket.Serialize();
            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Sends the player position to all other clients in the server.
        /// </summary>
        /// <param name="position">The position to be sent.</param>
        public void SendPosition(Vector3 position)
        {
            PlayerPositionData positionData = new PlayerPositionData(
                localPlayer,
                position.x,
                position.y,
                position.z
            );

            List<PlayerPositionData> playerPositionList = new List<PlayerPositionData> { positionData };

            PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket(localPlayer, playerPositionList);
            byte[] data = positionPacket.Serialize();
            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Sends the player rotation to all other clients in the server.
        /// </summary>
        /// <param name="rotation">The rotation to be sent</param>
        public void SendRotation(Vector3 rotation)
        {
            PlayerRotationData rotationData = new PlayerRotationData(
                localPlayer,
                rotation.x,
                rotation.y,
                rotation.z
            );

            List<PlayerRotationData> playerRotationList = new List<PlayerRotationData> { rotationData };

            PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket(localPlayer, playerRotationList);
            byte[] data = rotationPacket.Serialize();
            stream.Write(data, 0, data.Length);
        }
        #endregion
    }
}