using UnityEngine;
using Hamad.Scripts;
using Leonardo.Scripts.Networking;
using Leonardo.Scripts.Player;

namespace Leonardo.Scripts.ClientRelated
{
    public class NetworkClient : MonoBehaviour
    {
        [Header("- Connection Settings")]
        [SerializeField] private string playerNamePrefix = "Client-Player";
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private bool connectionOnStart = true;
        [SerializeField] private int port = 2121;
        
        [Header("- Player Settings")]
        [SerializeField] private GameObject playerPrefab;
        
        [Header("- Network Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        
        private NetworkConnection _networkConnection;
        private PacketHandler _packetHandler;
        private PlayerManager _playerManager;
        private PingMeter _pingMeter;
        
        private float _nextUpdateTime;
        
        public PlayerData LocalPlayer { get; private set; }
        public bool IsConnected => _networkConnection?.IsConnected ?? false;
        
        private void Start()
        {
            if (connectionOnStart)
            {
                ConnectToServer($"{playerNamePrefix}{Random.Range(1, 9999)}");
            }
            
            _pingMeter = FindObjectOfType<PingMeter>();
        }
        
        private void Update()
        {
            if (_networkConnection != null)
            {
                _networkConnection.Update();
            }
            
            SendPositionUpdates();
        }
        
        private void ConnectToServer(string username)
        {
            // Create local player data.
            LocalPlayer = new PlayerData(username, Random.Range(0, 9999));
            
            // Initialize components.
            _networkConnection = new NetworkConnection(ipAddress, port);
            _packetHandler = new PacketHandler(LocalPlayer);
            _playerManager = new PlayerManager(playerPrefab, LocalPlayer);
            
            // Set up event handlers.
            _networkConnection.OnDataReceived += _packetHandler.ProcessPacket;
            _packetHandler.OnPositionReceived += _playerManager.UpdateRemotePlayerPosition;
            _packetHandler.OnPingResponseReceived += OnPingResponse;
            _packetHandler.OnPushEventReceived += _playerManager.HandlePushEvent;

            
            // Connect to server.
            if (_networkConnection.Connect())
            {
                SendMessagePacket("Connected, hi!");
                _playerManager.SpawnLocalPlayer();
            }
        }
        
        private void SendPositionUpdates()
        {
            if (IsConnected && Time.time >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.time + updateInterval;
                
                GameObject localPlayerObj = _playerManager.GetLocalPlayerObject();
                if (localPlayerObj != null)
                {
                    SendPosition(localPlayerObj.transform.position);
                }
            }
        }

        public void SendMessagePacket(string message)
        {
            if (!IsConnected) return;
            
            byte[] data = _packetHandler.CreateMessagePacket(message);
            _networkConnection.SendData(data);
        }

        public void SendPosition(Vector3 position)
        {
            if (!IsConnected) return;
            
            byte[] data = _packetHandler.CreatePositionPacket(position);
            _networkConnection.SendData(data);
        }
        
        public void SendPingPacket()
        {
            if (!IsConnected) return;
            
            byte[] data = _packetHandler.CreatePingPacket();
            _networkConnection.SendData(data);
        }
        
        public void SendPushEvent(int targetPlayerTag, Vector3 force, string effectName)
        {
            if (!IsConnected) return;
    
            byte[] data = _packetHandler.CreatePushEventPacket(targetPlayerTag, force, effectName);
            _networkConnection.SendData(data);
        }
        
        private void OnPingResponse()
        {
            if (_pingMeter != null)
            {
                _pingMeter.OnPingResponse();
            }
        }
        
        private void OnDestroy()
        {
            _networkConnection?.Disconnect();
            _playerManager?.CleanUp();
        }
    }
}