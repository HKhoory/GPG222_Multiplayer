using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Leonardo.Scripts
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("- Connection Settings")]
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private int port = 8080;

        [Header("- Players Settings")]
        [SerializeField] private GameObject playerPrefab;

        private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();
        
        private Socket socket;
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
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Try to connect to server.
            ConnectToServer("Player" + Random.Range(1, 9999));
        }

        private void Update()
        {
            if (socket.Available > 0)
            {
                byte[] buffer = new byte[socket.Available];
                socket.Receive(buffer);
                DeserialzePlayerData(buffer);
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
                    players[tag] = new PlayerData(name, tag, position, rotation);
                    
                    // Spawn player in scene.\\
                    GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                    newPlayer.GetComponent<PlayerController>().playerData = players[tag];
                    
                    // Save the reference of this player's object in the dictionary for future proofing (maybe we'll need it later in the project).
                    playerObjects[tag] = newPlayer;
                    Debug.LogWarning($"New player joined {name}");
                }
                else
                {
                    players[tag].position = position;
                    players[tag].rotation = rotation;
                    
                    playerObjects[tag].GetComponent<PlayerController>().UpdatePlayerPosition(position, rotation);
                }
            }
        }

        private void ConnectToServer(string username)
        {
            try
            {
                localPlayer = new PlayerData(username, Random.Range(0, 9999), Vector3.zero, Quaternion.identity);
                socket.Connect(ipAddress, port);
                socket.Blocking = false;
                Debug.LogWarning("Connected to server! Yay!");

                // Send the player data.
                byte[] buffer = SerializePlayerData(localPlayer);
                socket.Send(buffer);
            }
            catch (SocketException socketException)
            {
                Debug.LogWarning($"NetworkManager.cs, socket exception: {socketException.Message}");
            }
        }
        #endregion
    }
}