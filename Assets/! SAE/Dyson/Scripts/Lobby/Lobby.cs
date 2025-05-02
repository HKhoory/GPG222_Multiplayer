using System;
using System.Collections;
using System.Collections.Generic;
using __SAE.Dyson.Scripts.Lobby;
using __SAE.Leonardo.Scripts.ClientRelated;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dyson.Scripts.Lobby
{
    /// <summary>
    /// Manages the multiplayer lobby, including player connections, ready states,
    /// and game start conditions.
    /// </summary>
    public class Lobby : MonoBehaviour
    {
        #region Serialized Fields

        [Header("- Lobby Configuration")]
        [SerializeField] private string gameSceneName = "Gameplay";
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float initialJoinDelay = 0.5f;
        [SerializeField] private float lobbyTimeout = 300f;

        [Header("- Player States")]
        [SerializeField] private ClientState localPlayerState;

        [Header("- UI References")]
        [SerializeField] private Transform playersContainer;
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI timeoutText;
        
        [Header("- UI Styling")]
        [SerializeField] private Color localPlayerBackgroundColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color hostPlayerBackgroundColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color regularPlayerBackgroundColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Image backgroundPanel;

        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;

        #endregion

        #region Private Fields

        private NetworkClient networkClient;
        private Dictionary<int, ClientState> allPlayerStates = new Dictionary<int, ClientState>();
        private Dictionary<int, GameObject> playerCards = new Dictionary<int, GameObject>();

        private bool isHost;
        private bool initializedLobby = false;
        private float reconnectTimer = 0f;
        private float reconnectInterval = 5f;
        private float lobbyTimeoutTimer;

        // Lobby state management.
        public enum LobbyState
        {
            Initializing,
            Connecting,
            Waiting,
            Starting,
            Error
        }

        private LobbyState currentState = LobbyState.Initializing;
        private string errorMessage = string.Empty;

        // Coroutines.
        private Coroutine startGameCoroutine;
        private Coroutine lobbyTimeoutCoroutine;
        private Coroutine reconnectCoroutine;

        #endregion

        #region Public Properties

        public ClientState LocalPlayerClientState => localPlayerState;
        public LobbyState CurrentState => currentState;
        public string ErrorMessage => errorMessage;
        public bool IsHost => isHost;
        public int PlayerCount => allPlayerStates.Count;
        public int MinPlayersToStart => minPlayersToStart;

        #endregion

        #region Unity Lifecycle Methods

        private void Awake() {
            InitializeFields();
            LogInfo("Lobby initialized");
        }

        private void Start() {
            SetLobbyState(LobbyState.Connecting);
            UpdateStatusText();

            RegisterEventHandlers();

            // Start timeout timer.
            lobbyTimeoutTimer = lobbyTimeout;
            lobbyTimeoutCoroutine = StartCoroutine(LobbyTimeoutCoroutine());

            // Delay joining the lobby to ensure network is ready.
            Invoke("DelayedJoinLobby", initialJoinDelay);
        }

        private void Update() {
            // Update timeout timer display.
            if (timeoutText != null && currentState == LobbyState.Waiting) {
                timeoutText.text = $"Timeout: {Mathf.CeilToInt(lobbyTimeoutTimer)}s";
                lobbyTimeoutTimer -= Time.deltaTime;
            }

            // Monitor connection status.
            if (networkClient != null && !networkClient.IsConnected && initializedLobby) {
                HandleDisconnect();
            }
        }

        private void OnDestroy() {
            UnregisterEventHandlers();

            if (startGameCoroutine != null) {
                StopCoroutine(startGameCoroutine);
            }

            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
            }

            if (reconnectCoroutine != null) {
                StopCoroutine(reconnectCoroutine);
            }

            LogInfo("Lobby destroyed");
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes fields and references.
        /// </summary>
        private void InitializeFields() {
            isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;

            // Create local player state.
            if (localPlayerState == null) {
                localPlayerState = new ClientState();
            }

            localPlayerState.isReady = false;

            // Find or create the NetworkClient.
            networkClient = FindObjectOfType<NetworkClient>();
            if (networkClient == null) {
                LogError("NetworkClient not found! Creating one...");
                GameObject networkObj = new GameObject("NetworkClient");
                networkClient = networkObj.AddComponent<NetworkClient>();
            }
        }

        private void RegisterEventHandlers() {
            if (networkClient != null) {
                StartCoroutine(WaitForPacketHandlerAndRegister());
            }
            else {
                LogError("NetworkClient is still null during event registration!");
            }
        }

        private IEnumerator WaitForPacketHandlerAndRegister() {
            while (networkClient.GetPacketHandler() == null) {
                LogInfo("Waiting for NetworkClient's PacketHandler to be ready...");
                yield return null;
            }

            LogInfo("PacketHandler is ready. Registering events.");
            PacketHandler packetHandler = networkClient.GetPacketHandler();
            packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyState;
            packetHandler.OnMessageReceived += HandleLobbyMessage;
        }

        private void UnregisterEventHandlers() {
            if (networkClient != null) {
                PacketHandler packetHandler = networkClient.GetPacketHandler();
                if (packetHandler != null) {
                    packetHandler.OnPlayerReadyStateChanged -= HandlePlayerReadyState;
                    packetHandler.OnMessageReceived -= HandleLobbyMessage;
                }
            }
        }

        #endregion

        #region Lobby Connection Methods

        /// <summary>
        /// Attempts to join the lobby after a delay to ensure network is ready.
        /// </summary>
        private void DelayedJoinLobby() {
            if (networkClient == null || !networkClient.IsConnected) {
                LogWarning("Network not ready, retrying in 1 second...");
                Invoke("DelayedJoinLobby", 1.0f);
                return;
            }

            initializedLobby = true;

            if (isHost) {
                // Host sets up their own player first.
                LogInfo("Host is setting up player with ID 1");
                localPlayerState.ClientId = 1;
                AddPlayerToLobby(localPlayerState);

                SetLobbyState(LobbyState.Waiting);
                UpdateStatusText("Hosting lobby - Waiting for players...");
            }
            else {
                SetLobbyState(LobbyState.Connecting);
                UpdateStatusText("Joining lobby...");
            }

            // Send lobby join request.
            networkClient.JoinLobby();
            LogInfo("Sent lobby join request");
        }

        /// <summary>
        /// Handles a network disconnection.
        /// </summary>
        private void HandleDisconnect() {
            reconnectTimer -= Time.deltaTime;

            UpdateStatusText($"Connection lost. Reconnecting in {Mathf.CeilToInt(reconnectTimer)}...");

            if (reconnectTimer <= 0f) {
                reconnectTimer = reconnectInterval;
                networkClient.InitiateConnection();

                UpdateStatusText("Attempting to reconnect...");
            }
        }

        #endregion

        #region Message Handling Methods

        /// <summary>
        /// Handles lobby messages from the server.
        /// </summary>
        /// <param name="playerName">The name of the player who sent the message.</param>
        /// <param name="message">The message that was sent.</param>
        private void HandleLobbyMessage(string playerName, string message) {
            LogInfo($"Received message from {playerName}: {message}");

            if (message == "JOIN_LOBBY") {
                HandlePlayerJoin(playerName);
            }
            else if (message == "START_GAME") {
                LogInfo("Received START_GAME message");
                OnStartGameMessageReceived();
            }
            else if (message.StartsWith("LOBBY_STATE:")) {
                if (!isHost) {
                    ParseLobbyState(message);
                }
            }
        }

        /// <summary>
        /// Handles a player joining the lobby.
        /// </summary>
        /// <param name="playerName">The name of the player who joined.</param>
        private void HandlePlayerJoin(string playerName) {
            LogInfo($"Player {playerName} joined the lobby");

            if (isHost) {
                // Find if player already exists.
                bool foundExisting = false;
                foreach (var state in allPlayerStates.Values) {
                    if (state.name == playerName) {
                        foundExisting = true;
                        break;
                    }
                }

                // Add new player if they don't exist.
                if (!foundExisting) {
                    ClientState newPlayerState = gameObject.AddComponent<ClientState>();
                    newPlayerState.ClientId = allPlayerStates.Count + 1;
                    newPlayerState.name = playerName;
                    AddPlayerToLobby(newPlayerState);

                    LogInfo($"Added new player {playerName} with ID {newPlayerState.ClientId}");

                    // ! Broadcast updated lobby state to ALL clients.
                    BroadcastLobbyState();
                }
            }
        }

        /// <summary>
        /// Broadcasts the current lobby state to all clients.
        /// </summary>
        private void BroadcastLobbyState() {
            if (!isHost || networkClient == null || !networkClient.IsConnected) return;

            string stateMessage = "LOBBY_STATE:";
            foreach (var player in allPlayerStates) {
                // Format: ClientID:isReady:playerName,
                stateMessage += $"{player.Key}:{player.Value.isReady}:{player.Value.name},";
            }

            LogInfo($"Broadcasting state: {stateMessage}");
            networkClient.SendMessagePacket(stateMessage);
        }

        /// <summary>
        /// Parses the lobby state from a message.
        /// </summary>
        /// <param name="message">The message containing the lobby state.</param>
        private void ParseLobbyState(string message) {
            LogInfo($"Parsing lobby state: {message}");

            try {
                // ! Format: "LOBBY_STATE:1:true:PlayerName,2:false:OtherPlayer,"
                string[] parts = message.Substring(12).Split(',');

                foreach (string part in parts) {
                    if (string.IsNullOrEmpty(part)) continue;

                    string[] playerData = part.Split(':');
                    if (playerData.Length < 3) {
                        LogWarning($"Invalid player data format: {part}");
                        continue;
                    }

                    try {
                        int id = int.Parse(playerData[0]);
                        bool ready = bool.Parse(playerData[1]);
                        string playerName = playerData[2];

                        if (!allPlayerStates.ContainsKey(id)) {
                            // New player.
                            ClientState newState = gameObject.AddComponent<ClientState>();
                            newState.ClientId = id;
                            newState.isReady = ready;
                            newState.name = playerName;

                            // Check if this is our local player.
                            if (networkClient != null && networkClient.LocalPlayer != null &&
                                networkClient.LocalPlayer.name == playerName) {
                                LogInfo($"Found our local player with ID {id}");
                                localPlayerState = newState;
                            }

                            AddPlayerToLobby(newState);
                        }
                        else {
                            // Update existing player.
                            allPlayerStates[id].isReady = ready;
                            allPlayerStates[id].name = playerName;
                            UpdatePlayerReadyUI(id);
                        }
                    }
                    catch (Exception e) {
                        LogError($"Error parsing player data: {e.Message}");
                    }
                }

                // Update lobby state
                SetLobbyState(LobbyState.Waiting);
                UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
            }
            catch (Exception e) {
                LogError($"Error parsing lobby state: {e.Message}");
                SetLobbyState(LobbyState.Error);
                errorMessage = "Failed to parse lobby state";
                UpdateStatusText("Error: Failed to parse lobby state");
            }
        }

        /// <summary>
        /// Handles a player's ready state change.
        /// </summary>
        /// <param name="playerData">The player data.</param>
        /// <param name="isReady">Whether the player is ready.</param>
        private void HandlePlayerReadyState(PlayerData playerData, bool isReady) {
            LogInfo($"Player {playerData.name} ready state changed to {isReady}");

            // Find the player by name.
            foreach (var pair in allPlayerStates) {
                ClientState state = pair.Value;

                // Match by name since player tags are random.
                if (state.name == playerData.name) {
                    state.isReady = isReady;
                    UpdatePlayerReadyUI(state.ClientId);
                    LogInfo($"Updated player {playerData.name} ready state to {isReady}");
                    break;
                }
            }

            if (isHost) {
                BroadcastLobbyState();
                CheckAllPlayersReady();
            }
        }

        #endregion

        #region Player Management Methods

        /// <summary>
        /// Adds a player to the lobby.
        /// </summary>
        /// <param name="playerState">The player state to add.</param>
        public void AddPlayerToLobby(ClientState playerState) {
            if (allPlayerStates.ContainsKey(playerState.ClientId)) {
                LogWarning($"Player with ID {playerState.ClientId} already exists!");
                return;
            }

            allPlayerStates[playerState.ClientId] = playerState;

            if (playersContainer != null && playerCardPrefab != null) {
                GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();

                if (playerInLobby != null) {
                    playerInLobby.Setup(playerState);
                }
                else {
                    LogWarning($"PlayerInLobby component not found on player card!");
                }

                playerCards[playerState.ClientId] = playerCard;
            }
            else {
                LogWarning("Cannot create player card: playersContainer or playerCardPrefab is null");
            }

            // Update the status text
            UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
        }

        /// <summary>
        /// Updates the UI for a player's ready state.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        private void UpdatePlayerReadyUI(int clientId) {
            if (playerCards.TryGetValue(clientId, out GameObject playerCard)) {
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();
                if (playerInLobby != null) {
                    playerInLobby.RefreshUI();
                }
                else {
                    LogWarning($"PlayerInLobby component not found on player card for client {clientId}");
                }
            }
            else {
                LogWarning($"Player card not found for client {clientId}");
            }
        }

        /// <summary>
        /// Checks if all players are ready to start the game.
        /// </summary>
        public void CheckAllPlayersReady() {
            if (!isHost) return;

            LogInfo($"Checking if all {allPlayerStates.Count} players are ready (min: {minPlayersToStart})");

            if (allPlayerStates.Count < minPlayersToStart) {
                LogInfo("Not enough players to start");
                return;
            }

            bool allReady = true;
            foreach (var pair in allPlayerStates) {
                if (!pair.Value.isReady) {
                    allReady = false;
                    LogInfo($"Player {pair.Value.name} (ID: {pair.Key}) is not ready");
                    break;
                }
            }

            if (allReady) {
                LogInfo("All players ready, starting game!");
                StartGame();
            }
        }

        #endregion

        #region Game Start Methods

        /// <summary>
        /// Starts the game.
        /// </summary>
        private void StartGame() {
            SetLobbyState(LobbyState.Starting);

            // Cancel the timeout coroutine.
            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
            }

            if (networkClient != null) {
                networkClient.SendMessagePacket("START_GAME");
            }
            else {
                LogError("Cannot start game: NetworkClient is null");
                SetLobbyState(LobbyState.Error);
                errorMessage = "Cannot start game: NetworkClient is null";
                UpdateStatusText("Error: Cannot start game");
                return;
            }

            // Wait a moment to ensure the message is sent.
            startGameCoroutine = StartCoroutine(StartGameCoroutine());
        }

        /// <summary>
        /// Coroutine to load the game scene after a delay.
        /// </summary>
        private IEnumerator StartGameCoroutine() {
            UpdateStatusText("Starting game...");
            yield return new WaitForSeconds(0.5f);
            LoadGameScene();
        }

        /// <summary>
        /// Loads the game scene.
        /// </summary>
        private void LoadGameScene() {
            LogInfo($"Loading game scene: {gameSceneName}");

            try {
                SceneManager.LoadScene(gameSceneName);
            }
            catch (Exception e) {
                LogError($"Failed to load game scene: {e.Message}");
                SetLobbyState(LobbyState.Error);
                errorMessage = $"Failed to load game scene: {e.Message}";
                UpdateStatusText("Error: Failed to load game scene");
            }
        }

        /// <summary>
        /// Called when a START_GAME message is received.
        /// </summary>
        public void OnStartGameMessageReceived() {
            LogInfo("Start game message received, loading game scene");

            // Cancel the timeout coroutine.
            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
            }

            SetLobbyState(LobbyState.Starting);
            LoadGameScene();
        }

        /// <summary>
        /// Coroutine to handle lobby timeout.
        /// </summary>
        private IEnumerator LobbyTimeoutCoroutine() {
            while (lobbyTimeoutTimer > 0) {
                yield return new WaitForSeconds(1f);
                lobbyTimeoutTimer -= 1f;
            }

            // Timeout reached
            LogWarning("Lobby timeout reached");

            if (isHost) {
                // Host can force start if minimum players are met.
                if (allPlayerStates.Count >= minPlayersToStart) {
                    LogInfo("Timeout reached with enough players, starting game");
                    StartGame();
                }
                else {
                    LogWarning("Timeout reached without enough players");
                    SetLobbyState(LobbyState.Error);
                    errorMessage = "Timeout reached without enough players";
                    UpdateStatusText("Error: Not enough players joined");
                }
            }
            else {
                // Non-host clients just show an error.
                SetLobbyState(LobbyState.Error);
                errorMessage = "Lobby timed out";
                UpdateStatusText("Error: Lobby timed out");
            }
        }

        #endregion

        #region State Management Methods

        /// <summary>
        /// Sets the lobby state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        private void SetLobbyState(LobbyState newState) {
            LogInfo($"Changing lobby state from {currentState} to {newState}");
            currentState = newState;

            // Additional state-specific logic can be added here.
            switch (newState) {
                case LobbyState.Initializing:
                    // Nothing to do.
                    break;

                case LobbyState.Connecting:
                    // Nothing to do.
                    break;

                case LobbyState.Waiting:
                    // Nothing to do.
                    break;

                case LobbyState.Starting:
                    // Nothing to do.
                    break;

                case LobbyState.Error:
                    // Error handling.
                    LogError($"Lobby entered error state: {errorMessage}");
                    break;
            }
        }

        /// <summary>
        /// Updates the status text.
        /// </summary>
        /// <param name="message">The message to display.</param>
        private void UpdateStatusText(string message = null) {
            if (statusText == null) return;

            if (message != null) {
                statusText.text = message;
                return;
            }

            // Update based on current state.
            switch (currentState) {
                case LobbyState.Initializing:
                    statusText.text = "Initializing lobby...";
                    break;

                case LobbyState.Connecting:
                    statusText.text = "Connecting to lobby...";
                    break;

                case LobbyState.Waiting:
                    statusText.text = $"In Lobby - {allPlayerStates.Count} players";
                    break;

                case LobbyState.Starting:
                    statusText.text = "Starting game...";
                    break;

                case LobbyState.Error:
                    statusText.text = $"Error: {errorMessage}";
                    break;
            }
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message) {
            if (verboseLogging || message.Contains("error") || message.Contains("Error")) {
                Debug.Log($"[Lobby] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogWarning(string message) {
            Debug.LogWarning($"[Lobby] WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message) {
            Debug.LogError($"[Lobby] ERROR: {message}");
        }

        #endregion
    }
}