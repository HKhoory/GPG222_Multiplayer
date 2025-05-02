using System;
using System.Collections;
using Dyson_GPG222_Server;
using Dyson.GPG222.Lobby;
using UnityEngine;
using Hamad.Scripts;
using Leonardo.Scripts.Networking;
using Leonardo.Scripts.Player;
using Random = UnityEngine.Random;

namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// Handles client-side network communication including connections, 
    /// packet sending, and reconnection logic.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        #region Serialized Fields
        [Header("- Connection Settings")]
        [SerializeField] private string playerNamePrefix = "Client-Player";
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private bool connectionOnStart = true;
        [SerializeField] private int port = 2121;
        [SerializeField] private float connectionTimeout = 10f;
        
        [Header("- Player Settings")]
        [SerializeField] private GameObject playerPrefab;
        
        [Header("- Network Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private float heartbeatInterval = 2.0f;
        [SerializeField] private float reconnectInterval = 5.0f;
        [SerializeField] private int maxReconnectAttempts = 5;
        
        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private Fields
        private NetworkConnection _networkConnection;
        private PacketHandler _packetHandler;
        private PlayerManager _playerManager;
        private PingMeter _pingMeter;
        private float _nextUpdateTime;
        private float _nextHeartbeatTime;
        private float _reconnectTimer;
        private int _reconnectAttempts = 0;
        private bool _hasInitializedConnection = false;
        private bool _isReconnecting = false;
        private bool _isHost;
        private Coroutine _connectionTimeoutCoroutine;
        
        
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            Failed
        }
        
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        #endregion

        #region Public Properties
        public PlayerData LocalPlayer { get; private set; }
        public bool IsConnected => _networkConnection?.IsConnected ?? false;
        public ConnectionState State => _connectionState;
        #endregion

        #region Unity Lifecycle Methods
        private void Awake()
        {
            // Prevent destruction when loading a new scene
            DontDestroyOnLoad(gameObject);
            LogInfo("NetworkClient initialized");
        }
        
        private void Start()
        {
            _isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
            ipAddress = PlayerPrefs.GetString("ServerIP", "127.0.0.1");
            
            _pingMeter = FindObjectOfType<PingMeter>();
            _reconnectTimer = reconnectInterval;
            
            if (connectionOnStart && !_hasInitializedConnection)
            {
                InitiateConnection();
            }
        }
        
        private void Update()
        {
            switch (_connectionState)
            {
                case ConnectionState.Disconnected:
                    // Nothing to do when disconnected.
                    break;
                    
                case ConnectionState.Connecting:
                    // Handled by connection timeout coroutine.
                    break;
                    
                case ConnectionState.Connected:
                    if (!IsConnected)
                    {
                        // Connection lost.
                        HandleDisconnect();
                        break;
                    }
                    
                    // Update network connection.
                    UpdateConnection();
                    break;
                    
                case ConnectionState.Reconnecting:
                    UpdateReconnection();
                    break;
                    
                case ConnectionState.Failed:
                    // Nothing to do when failed.
                    break;
            }
        }
        
        private void OnDestroy()
        {
            CleanupConnection();
            LogInfo("NetworkClient destroyed");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initiates a connection to the server.
        /// </summary>
        public void InitiateConnection()
        {
            if (_connectionState == ConnectionState.Connected)
            {
                LogWarning("Already connected to server, ignoring connection request");
                return;
            }
            
            if (_connectionState == ConnectionState.Connecting)
            {
                LogWarning("Connection already in progress, ignoring connection request");
                return;
            }
            
            _hasInitializedConnection = true;
            _connectionState = ConnectionState.Connecting;
            _reconnectAttempts = 0;
            
            // Generate unique player name.
            string playerName = $"{playerNamePrefix}{Random.Range(1000, 9999)}";
            LogInfo($"Initiating connection as {playerName} to {ipAddress}:{port}");
            
            ConnectToServer(playerName);
            
            // Start connection timeout coroutine.
            if (_connectionTimeoutCoroutine != null)
            {
                StopCoroutine(_connectionTimeoutCoroutine);
            }
            _connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());
            
            // If host, ensure server is running.
            if (_isHost)
            {
                StartServer();
            }
        }
        
        /// <summary>
        /// Sends a chat message to all connected clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessagePacket(string message)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot send message: not connected to server");
                return;
            }
            
            try
            {
                byte[] data = _packetHandler.CreateMessagePacket(message);
                _networkConnection.SendData(data);
                if (verboseLogging)
                {
                    LogInfo($"Sent message: {message}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends the player's current position to the server.
        /// </summary>
        /// <param name="position">The position to send.</param>
        public void SendPosition(Vector3 position)
        {
            if (!IsConnected) return;
            
            try
            {
                byte[] data = _packetHandler.CreatePositionPacket(position);
                _networkConnection.SendData(data);
                if (verboseLogging)
                {
                    LogInfo($"Sent position: {position}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to send position: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a ping packet to measure latency.
        /// </summary>
        public void SendPingPacket()
        {
            if (!IsConnected) return;
            
            try
            {
                byte[] data = _packetHandler.CreatePingPacket();
                _networkConnection.SendData(data);
                if (verboseLogging)
                {
                    LogInfo("Sent ping packet");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to send ping: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a push event to the server.
        /// </summary>
        /// <param name="targetPlayerTag">The tag of the target player.</param>
        /// <param name="force">The force to apply.</param>
        /// <param name="effectName">The name of the effect to play.</param>
        public void SendPushEvent(int targetPlayerTag, Vector3 force, string effectName)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot send push event: not connected to server");
                return;
            }
            
            try
            {
                byte[] data = _packetHandler.CreatePushEventPacket(targetPlayerTag, force, effectName);
                _networkConnection.SendData(data);
                if (verboseLogging)
                {
                    LogInfo($"Sent push event to player {targetPlayerTag} with force {force}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to send push event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a heartbeat to keep the connection alive.
        /// </summary>
        public void SendHeartBeat()
        {
            if (!IsConnected) return;

            try
            {
                byte[] data = _packetHandler.CreateHeartbeatPacket(0x01);
                _networkConnection.SendData(data);
                if (verboseLogging)
                {
                    LogInfo("Sent heartbeat");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to send heartbeat: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a restart packet to the server.
        /// </summary>
        /// <param name="reset">Whether to reset the game.</param>
        public void SendRestartPacket(bool reset)
        {
            if (!IsConnected)
            {
                LogWarning("Cannot send restart packet: not connected to server");
                return;
            }

            try
            {
                byte[] data = _packetHandler.CreateRestartPacket(reset);
                _networkConnection.SendData(data);
                LogInfo($"Sent restart packet with reset={reset}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to send restart packet: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a lobby join request to the server.
        /// </summary>
        public void JoinLobby()
        {
            if (!IsConnected)
            {
                LogWarning("Cannot join lobby: not connected to server");
                return;
            }

            try
            {
                LogInfo("Sending lobby join request");
                byte[] data = _packetHandler.CreateLobbyPacket();
                _networkConnection.SendData(data);
            }
            catch (Exception ex)
            {
                LogError($"Failed to join lobby: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends the player's ready state to the server.
        /// </summary>
        /// <param name="isReady">Whether the player is ready.</param>
        public void SendPlayerReadyState(bool isReady)
        {
            if (!IsConnected)
            {
                LogWarning("Cannot send ready state: not connected to server");
                return;
            }
            
            try
            {
                LogInfo($"Sending player ready state: {isReady}");
                byte[] data = _packetHandler.CreateReadyInLobbyPacket(isReady);
                _networkConnection.SendData(data);
            }
            catch (Exception ex)
            {
                LogError($"Failed to send ready state: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the network connection.
        /// </summary>
        /// <returns>The network connection.</returns>
        public NetworkConnection GetConnection()
        {
            return _networkConnection;
        }
        
        /// <summary>
        /// Gets the packet handler.
        /// </summary>
        /// <returns>The packet handler.</returns>
        public PacketHandler GetPacketHandler()
        {
            return _packetHandler;
        }
        
        /// <summary>
        /// Gets the player manager.
        /// </summary>
        /// <returns>The player manager.</returns>
        public PlayerManager GetPlayerManager()
        {
            return _playerManager;
        }
        
        /// <summary>
        /// Forces disconnection from the server.
        /// </summary>
        public void ForceDisconnect()
        {
            LogInfo("Forcing disconnect from server");
            CleanupConnection();
            _connectionState = ConnectionState.Disconnected;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Starts the server if this client is the host.
        /// </summary>
        private void StartServer()
        {
            var serverComponent = FindObjectOfType<Server>();
            if (serverComponent != null)
            {
                serverComponent.enabled = true;
                serverComponent.StartServer();
                LogInfo("Started server");
            }
            else
            {
                LogError("Failed to start server: Server component not found");
            }
        }
        
        /// <summary>
        /// Connects to the server.
        /// </summary>
        /// <param name="username">The username to use.</param>
        private void ConnectToServer(string username)
        {
            // Create local player data if not already created
            if (LocalPlayer == null)
            {
                LocalPlayer = new PlayerData(username, Random.Range(1000, 9999));
            }
            
            // Clean up existing connection if any
            CleanupConnection();
            
            // Initialize network components
            _networkConnection = new NetworkConnection(ipAddress, port);
            _packetHandler = new PacketHandler(LocalPlayer);
            
            // Initialize player manager if not already created
            if (_playerManager == null)
            {
                _playerManager = new PlayerManager(playerPrefab, LocalPlayer);
            }
            
            // Set up event handlers
            _networkConnection.OnConnected += HandleConnected;
            _networkConnection.OnDataReceived += _packetHandler.ProcessPacket;
            _networkConnection.OnDisconnected += HandleDisconnect;
            _packetHandler.OnPositionReceived += _playerManager.UpdateRemotePlayerPosition;
            _packetHandler.OnPingResponseReceived += OnPingResponse;
            _packetHandler.OnPushEventReceived += _playerManager.HandlePushEvent;
            _packetHandler.OnMessageReceived += HandleLobbyMessages;
            _packetHandler.OnHeartbeat += _networkConnection.CheckHeartbeat;
            _packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyStateChanged;
            
            // Connect to server
            LogInfo($"Connecting to server at {ipAddress}:{port} as {username}");
            if (_networkConnection.Connect())
            {
                LogInfo($"Socket connected to server at {ipAddress}:{port}");
            }
            else
            {
                LogError($"Failed to connect to server at {ipAddress}:{port}");
                HandleConnectionFailed();
            }
        }
        
        /// <summary>
        /// Handles a successful connection to the server.
        /// </summary>
        private void HandleConnected()
        {
            _connectionState = ConnectionState.Connected;
            _reconnectAttempts = 0;
            
            LogInfo($"Connected to server at {ipAddress}:{port}");
            
            // Cancel timeout coroutine
            if (_connectionTimeoutCoroutine != null)
            {
                StopCoroutine(_connectionTimeoutCoroutine);
                _connectionTimeoutCoroutine = null;
            }
            
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            if (currentSceneName.Contains("Lobby"))
            {
                JoinLobby();
            }
            else
            {
                _playerManager.SpawnLocalPlayer();
                // Send an initial "hello" message
                SendMessagePacket("Connected, hi!");
            }
        }
        
        /// <summary>
        /// Handles a disconnection from the server.
        /// </summary>
        private void HandleDisconnect()
        {
            if (_connectionState == ConnectionState.Disconnected)
            {
                // Already disconnected
                return;
            }
            
            LogWarning("Disconnected from server");
            
            // Only go to reconnecting state if we were previously connected
            if (_connectionState == ConnectionState.Connected)
            {
                _connectionState = ConnectionState.Reconnecting;
                _reconnectTimer = reconnectInterval;
                _reconnectAttempts = 0;
            }
        }
        
        /// <summary>
        /// Handles a failed connection to the server.
        /// </summary>
        private void HandleConnectionFailed()
        {
            LogError("Connection to server failed");
            _connectionState = ConnectionState.Reconnecting;
            _reconnectTimer = reconnectInterval;
        }
        
        /// <summary>
        /// Handles a connection timeout.
        /// </summary>
        private IEnumerator ConnectionTimeoutCoroutine()
        {
            yield return new WaitForSeconds(connectionTimeout);
            
            if (_connectionState == ConnectionState.Connecting)
            {
                LogError($"Connection timed out after {connectionTimeout} seconds");
                HandleConnectionFailed();
            }
        }
        
        /// <summary>
        /// Updates the network connection.
        /// </summary>
        private void UpdateConnection()
        {
            // Update network connection
            _networkConnection.Update();
            
            // Send position updates
            SendPositionUpdates();
            
            // Send heartbeat
            if (Time.time >= _nextHeartbeatTime)
            {
                _nextHeartbeatTime = Time.time + heartbeatInterval;
                SendHeartBeat();
            }
        }
        
        /// <summary>
        /// Updates the reconnection logic.
        /// </summary>
        private void UpdateReconnection()
        {
            _reconnectTimer -= Time.deltaTime;
            
            if (_reconnectTimer <= 0f)
            {
                _reconnectAttempts++;
                
                if (_reconnectAttempts > maxReconnectAttempts)
                {
                    LogError($"Failed to reconnect after {maxReconnectAttempts} attempts");
                    _connectionState = ConnectionState.Failed;
                    return;
                }
                
                // Exponential backoff for reconnect interval
                float backoffMultiplier = Mathf.Min(1f + (_reconnectAttempts * 0.5f), 3f);
                _reconnectTimer = reconnectInterval * backoffMultiplier;
                
                LogInfo($"Attempting to reconnect ({_reconnectAttempts}/{maxReconnectAttempts})");
                
                // Attempt to reconnect with the same player data
                if (LocalPlayer != null)
                {
                    ConnectToServer(LocalPlayer.name);
                }
                else
                {
                    ConnectToServer($"{playerNamePrefix}{Random.Range(1000, 9999)}");
                }
                
                // Update connection state
                _connectionState = ConnectionState.Connecting;
            }
        }
        
        /// <summary>
        /// Sends position updates for the local player.
        /// </summary>
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
        
        /// <summary>
        /// Cleans up the connection.
        /// </summary>
        private void CleanupConnection()
        {
            if (_networkConnection != null)
            {
                _networkConnection.Disconnect();
                _networkConnection = null;
            }
            
            if (_playerManager != null)
            {
                _playerManager.CleanUp();
            }
            
            if (_connectionTimeoutCoroutine != null)
            {
                StopCoroutine(_connectionTimeoutCoroutine);
                _connectionTimeoutCoroutine = null;
            }
        }
        
        /// <summary>
        /// Handles a ping response from the server.
        /// </summary>
        private void OnPingResponse()
        {
            if (_pingMeter != null)
            {
                _pingMeter.OnPingResponse();
            }
        }
        
        /// <summary>
        /// Handles lobby messages from the server.
        /// </summary>
        /// <param name="playerName">The name of the player who sent the message.</param>
        /// <param name="message">The message content.</param>
        private void HandleLobbyMessages(string playerName, string message)
        {
            LogInfo($"Received message from {playerName}: {message}");
            
            if (message == "START_GAME")
            {
                Lobby lobby = FindObjectOfType<Lobby>();
                if (lobby != null)
                {
                    lobby.OnStartGameMessageReceived();
                }
                else
                {
                    LogWarning("Cannot start game: Lobby component not found");
                }
            }
            else if (message.StartsWith("LOBBY_STATE:"))
            {
                // This will be handled by the Lobby component which also subscribes to OnMessageReceived
                if (verboseLogging)
                {
                    LogInfo("Received lobby state update");
                }
            }
        }
        
        /// <summary>
        /// Handles player ready state changes.
        /// </summary>
        /// <param name="playerData">The player data.</param>
        /// <param name="isReady">Whether the player is ready.</param>
        private void HandlePlayerReadyStateChanged(PlayerData playerData, bool isReady)
        {
            LogInfo($"Received player ready state change for {playerData.name}: {isReady}");
            // This would be handled by the Lobby component
        }
        #endregion

        #region Logging Methods
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message)
        {
            Debug.Log($"[NetworkClient] {message}");
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[NetworkClient] WARNING: {message}");
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message)
        {
            Debug.LogError($"[NetworkClient] ERROR: {message}");
        }
        #endregion
    }
}