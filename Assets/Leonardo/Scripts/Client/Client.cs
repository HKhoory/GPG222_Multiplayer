using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;
using Leonardo.Scripts.Controller;
using Unity.VisualScripting;


namespace Leonardo.Scripts
{
    public class Client : MonoBehaviour
    {
        [Header("- Connection Settings")] [SerializeField]
        private string ipAddress = "127.0.0.1";

        [SerializeField] private int port = 2121;
        [SerializeField] private bool connectionOnStart = true;
        [SerializeField] private string playerNamePrefix = "Client-Player";

        [Header("- Players Settings")] [SerializeField]
        private GameObject playerPrefab;

        [SerializeField] private Vector3 spawnPosition = new Vector3(0,1,0);
        
        [Header("- Test Settings")]
        [SerializeField] private bool enableTestMovement = true;
        [SerializeField] private float testMovementSpeed = 2f;
        
        private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();
        private TcpClient socket;
        private NetworkStream stream;
        byte[] receiveBuffer;
        private PlayerData localPlayer;
        private bool isConnected = false;
        private float nextUpdateTime = 0f;
        private float updateInterval = 0.1f;

        #region Unity Methods

        private void Start()
        {
            if (connectionOnStart)
            {
                // Try to connect to server.
                ConnectToServer($"{playerNamePrefix}{Random.Range(1,9999)}");
            }
        }

        private void Update()
        {
            // Send the position/rotation of players AT THE DEFINED INTERVAL:
            bool shouldSpawn = false;
            PlayerData playerData = null;

            lock (this)
            {
                if (shouldSpawnPlayer)
                {
                    shouldSpawn = true;
                    playerData = playerToSpawn;
                    shouldSpawnPlayer = false;
                }
            }

            if (shouldSpawn && playerData != null)
            {
                Debug.LogWarning($"Spawning player from main thread.");
                SpawnLocalPlayer(playerData);
            }
            
            if (isConnected && Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;
                
                if (playerObjects.ContainsKey(localPlayer.tag))
                {
                    SendPosition(playerObjects[localPlayer.tag].transform.position);
                    SendRotation(playerObjects[localPlayer.tag].transform.eulerAngles);
                }
            }

            if (enableTestMovement && playerObjects.ContainsKey(localPlayer.tag))
            {
                // This is to move the player for testing purposes.
                Vector3 movement = new Vector3(
                    Mathf.Sin(Time.time * testMovementSpeed), 
                    0, 
                    Mathf.Cos(Time.time * testMovementSpeed)
                ) * testMovementSpeed * Time.deltaTime;
            
                playerObjects[localPlayer.tag].transform.position += movement;
            }

        }

        #endregion

        #region Private Methods

        public void ManualConnect(string playerName = null)
        {
            if (playerName == null)
            {
                playerName = $"{playerNamePrefix}{Random.Range(1,9999)}";
            }
            
            ConnectToServer(playerName);
        }
        
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

        private bool shouldSpawnPlayer = false;
        private PlayerData playerToSpawn = null;
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

        private void SpawnLocalPlayer(PlayerData playerData)
        {
            Debug.LogWarning("SPWANING PLAYER!!!!!!!!! HELLOOO");
    
            try
            {
                Vector3 spawnPosition = new Vector3(0, 1, 0);
                GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                playerObjects[localPlayer.tag] = newPlayer;
                
                var controller = newPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetLocalplayer(true);
                }
                
                Debug.LogWarning($"Local player: {playerData.name} spawned at {spawnPosition}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error spawning player: {e.Message}\n{e.StackTrace}");
            }
        }

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

        // Handles deserializing the packet and running logic depending on the type of packet that it is.
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
                        Debug.Log(
                            $"Client: Received position updates for {positionPacket.PlayerPositionData.Count} players");

                        foreach (var playerPos in positionPacket.PlayerPositionData)
                        {
                            // Skip updates for this client's player.
                            if (playerPos.playerData.tag == localPlayer.tag)
                                continue;

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

        private void UpdateOrCreatePlayerPosition(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;

            // If we don't have this player yet, create it.
            if (!playerObjects.ContainsKey(playerTag))
            {
                Vector3 position = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
                GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
                playerObjects[playerTag] = newPlayer;

                var controller = newPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetLocalplayer(false);
                }
                
                newPlayer.name = $"Player_{playerPos.playerData.name}";
                Debug.Log($"Created new player: {playerPos.playerData.name} with tag: {playerTag}");
            }
            else
            {
                // Update existing player position.
                Vector3 newPosition = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
                playerObjects[playerTag].transform.position = newPosition;
            }
        }

        private void UpdatePlayerRotation(PlayerRotationData playerRot)
        {
            int playerTag = playerRot.playerData.tag;

            // Only update rotation if we have this player
            if (playerObjects.ContainsKey(playerTag))
            {
                Vector3 newRotation = new Vector3(playerRot.xRot, playerRot.yRot, playerRot.zRot);
                playerObjects[playerTag].transform.rotation = Quaternion.Euler(newRotation);
            }
        }

        // --- 'SEND' Methods.
        public void SendMessagePacket(string message)
        {
            MessagePacket messagePacket = new MessagePacket(localPlayer, message);
            byte[] data = messagePacket.Serialize();
            stream.Write(data, 0, data.Length);
        }

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