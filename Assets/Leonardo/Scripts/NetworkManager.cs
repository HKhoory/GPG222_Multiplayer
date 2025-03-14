using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;


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
        public const int dataBufferSize = 4096;
        private static NetworkManager instance { get; set; }
        private Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();
        private PlayerData localPlayer;

        #region Unity Methods

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

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

        private byte[] SerializePlayerData(PlayerData playerData)
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
        }

        private void ConnectToServer(string username)
        {
            try
            {
                localPlayer = new PlayerData(username, Random.Range(0, 9999));
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];

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
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

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
                    Debug.LogWarning($"NetworkManger.cs: Client disconnected from server.");
                    return;
                }

                byte[] data = new byte[byteLenght];
                Array.Copy(receiveBuffer, data, byteLenght);
                
                // Parse the received packet.
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
                        Broadcast(data); // Forward to all clients
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

                // Continue reading from this client
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch(Exception e)
            {
                Debug.LogError($"NetworkManager.cs: Error in ReceiveCallback: {e.Message}");
                throw;
            }
        }

        private void Broadcast(byte[] data)
        {
            throw new NotImplementedException();
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
                    Debug.LogWarning(
                        $"NetworkMananger.cs: Received message from {messagePacket.playerData.name}: {messagePacket.Message}");
                    // Later pass the message to the UI manager for it to be displayed to other players.
                    break;

                // Color packages.
                case Packet.PacketType.Color:
                    // I don't think we'll end up using color but whatever logic for these packages goes here.
                    break;

                case Packet.PacketType.PlayersPositionData:
                    PlayersPositionDataPacket playersPositionDataPacket =
                        new PlayersPositionDataPacket().Deserialize(data);
                    Debug.LogWarning(
                        $"Received positions for {playersPositionDataPacket.PlayerPositionData.Count} players.");

                    // TODO: Add sync logic here later.

                    break;

                case Packet.PacketType.PlayersRotationData:
                    PlayersRotationDataPacket playersRotationDataPacket =
                        new PlayersRotationDataPacket().Deserialize(data);
                    Debug.LogWarning(
                        $"Received rotations for {playersRotationDataPacket.PlayerRotationData.Count} players.");

                    // TODO: Add sync logic here later.

                    break;

                default:
                    Debug.LogWarning($"TestClient.cs: received unknown packet type: {basePacket.packetType}");
                    break;
            }
        }

        // Methods for future use.
        public void SendMessage(string message)
        {
            MessagePacket messagePacket = new MessagePacket(localPlayer, message);
            byte[] data = messagePacket.Serialize();
            stream.Write(data, 0, data.Length);
        }

        public void SendPosition(Vector3 position)
        {
            PlayerPositionData positionData = new PlayerPositionData(localPlayer, position);
            List<PlayerPositionData> playerPositionList = new List<PlayerPositionData>{positionData};

            PlayersPositionDataPacket positionPacket = new PlayersPositionDataPacket(localPlayer, playerPositionList);
            byte[] data = positionPacket.Serialize();
            stream.Write(data, 0, data.Length);
        }
        
        #endregion
    }
}