using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Hamad.Scripts.Rotation;


namespace Leonardo.Scripts
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("- Connection Settings")] [SerializeField]
        private string ipAddress = "127.0.0.1";

        [SerializeField] private int port = 2121;

        [Header("- Players Settings")] [SerializeField]
        private GameObject playerPrefab;

        private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();

        private TcpClient socket;
        private NetworkStream stream;
        byte[] receiveBuffer;
        private PlayerData localPlayer;

        #region Unity Methods

        private void Start()
        {
            // Try to connect to server.
            ConnectToServer("Player" + Random.Range(1, 9999));
        }

        private void Update()
        {
            // Send the position of players:
            if (playerObjects.ContainsKey(localPlayer.tag))
            {
                SendPosition(playerObjects[localPlayer.tag].transform.position);
            }
        }

        #endregion

        #region Private Methods

        /*private byte[] SerializePlayerData(PlayerData playerData)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            {
                writer.Write(playerData.name);
                writer.Write(playerData.tag);
                return memoryStream.ToArray();
            }
        }

        private void DeserialzePlayerData(byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream(buffer);
            BinaryReader reader = new BinaryReader(memoryStream);
            {
                string name = reader.ReadString();
                int tag = reader.ReadInt32();
                Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Quaternion rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle());

                if (!players.ContainsKey(tag))
                {
                    players[tag] = new PlayerData(name, tag);

                    // Spawn player in scene.
                    GameObject newPlayer = Instantiate(playerPrefab, position, rotation);

                    // Save the reference of this player's object in the dictionary for future proofing (maybe we'll need it later in the project).
                    playerObjects[tag] = newPlayer;
                    Debug.LogWarning($"New player joined {name}");
                }
                else
                {
                    
                }
            }
        }*/

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

        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (!socket.Connected)
            {
                Debug.LogError($"NetworkManger.cs: Client failed to connect.");
                return;
            }

            stream = socket.GetStream();
            Debug.LogWarning($"NetworkManger.cs: Client connected to {ipAddress}:{port}.");

            // Here start reading stuff since client connected.
            stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, ReceiveCallback, null);

            // This is to test and send a Hello package to each person at first.
            var messagePacket = new MessagePacket(localPlayer, "Connected, hi!");
            byte[] data = messagePacket.Serialize();
            stream.Write(data, 0, data.Length);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLenght = stream.EndRead(result);
                if (byteLenght <= 0)
                {
                    return;
                }

                byte[] data = new byte[byteLenght];
                Array.Copy(receiveBuffer, data, byteLenght);

                HandlePacket(data);
                stream.BeginRead(receiveBuffer,0,receiveBuffer.Length,ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"NetworkManager.cs: Error in ReceiveCallback: {e.Message}");
                throw;
            }
        }
        
        // Handles deserializing the packet and running logic depending on the type of packet that it is.
        private void HandlePacket(byte[] data)
        {
            Packet basePacket = new Packet();
            basePacket.Deserialize(data);
            
            switch (basePacket.packetType)
            {
                case Packet.PacketType.Message:
                    MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                    Debug.Log($"Client: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                    break;
                
                case Packet.PacketType.PlayersPositionData:
                    PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket().Deserialize(data);
                    Debug.Log($"Client: Received position updates for {positionPacket.PlayerPositionData.Count} players");
                    
                    foreach (var playerPos in positionPacket.PlayerPositionData)
                    {
                        // Skip updates for our own player
                        if (playerPos.playerData.tag == localPlayer.tag)
                            continue;
                            
                        UpdateOrCreatePlayerPosition(playerPos);
                    }
                    break;
                
                case Packet.PacketType.PlayersRotationData:
                    PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket().Deserialize(data);
                    Debug.Log($"Client: Received rotation updates for {rotationPacket.PlayerRotationData.Count} players");
                    
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
        
        private void UpdateOrCreatePlayerPosition(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
            
            // If we don't have this player yet, create it
            if (!playerObjects.ContainsKey(playerTag))
            {
                Vector3 position = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
                GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
                playerObjects[playerTag] = newPlayer;
                Debug.Log($"Created new player: {playerPos.playerData.name} with tag: {playerTag}");
            }
            else
            {
                // Update existing player position
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
        public void SendMessage(string message)
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
            
            List<PlayerPositionData> playerPositionList = new List<PlayerPositionData>{positionData};

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
            
            List<PlayerRotationData> playerRotationList = new List<PlayerRotationData>{rotationData};

            PlayersRotationDataPacket rotationPacket = new PlayersRotationDataPacket(localPlayer, playerRotationList);
            byte[] data = rotationPacket.Serialize();
            stream.Write(data, 0, data.Length);
        }
        
        #endregion
    }
}