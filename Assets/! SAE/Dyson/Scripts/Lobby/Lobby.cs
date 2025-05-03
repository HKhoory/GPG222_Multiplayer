using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using __SAE.Leonardo.Scripts.ClientRelated;
using __SAE.Leonardo.Scripts.Packets;
using __SAE.Leonardo.Scripts.Player;
using Dyson.Scripts.Lobby;
using Hamad.Scripts;
using Leonardo.Scripts.Networking;
using TMPro;
using UnityEngine;

namespace __SAE.Dyson.Scripts.Lobby
{
    /// <summary>
    /// Manages the multiplayer lobby, including player connections, ready states,
    /// and game start conditions.
    /// </summary>
    public class Lobby : MonoBehaviour
    {
        [Header("- Lobby Configuration")]
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float initialJoinDelay = 0.5f;
        [SerializeField] private float lobbyTimeout = 300f;

        [Header("- UI References")]
        [SerializeField] private Transform playersContainer;
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI timeoutText;

        [Header("- Gameplay Settings")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject lobbyPanel;

        private GameplayManager _gameplayManager;
        private ClientState localPlayerState;
        private NetworkClient networkClient;
        private Dictionary<int, ClientState> allPlayerStates = new Dictionary<int, ClientState>();
        private Dictionary<int, GameObject> playerCards = new Dictionary<int, GameObject>();

        private bool isHost;
        private bool initializedLobby = false;
        private float reconnectTimer = 0f;
        private float reconnectInterval = 5f;
        private float lobbyTimeoutTimer;

        /// <summary>
        /// Represents the possible states of the lobby.
        /// </summary>
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

        private Coroutine startGameCoroutine;
        private Coroutine lobbyTimeoutCoroutine;
        private Coroutine reconnectCoroutine;
        private bool gameplayStarted = false;

        /// <summary>
        /// Gets the state object for the local player.
        /// </summary>
        public ClientState LocalPlayerState => localPlayerState;

        /// <summary>
        /// Gets the current operational state of the lobby.
        /// </summary>
        public LobbyState CurrentState => currentState;

        /// <summary>
        /// Gets the last recorded error message, if any.
        /// </summary>
        public string ErrorMessage => errorMessage;

        /// <summary>
        /// Gets a value indicating whether this instance is the host.
        /// </summary>
        public bool IsHost => isHost;

        /// <summary>
        /// Gets the current number of players in the lobby.
        /// </summary>
        public int PlayerCount => allPlayerStates.Count;

        /// <summary>
        /// Gets the minimum number of players required to start the game.
        /// </summary>
        public int MinPlayersToStart => minPlayersToStart;

        private void Awake() {
            InitializeFields();
        }

        private void Start() {
            SetLobbyState(LobbyState.Connecting);
            UpdateStatusText();

            RegisterEventHandlers();

            _gameplayManager = FindObjectOfType<GameplayManager>();
            if (_gameplayManager == null) {
                GameObject gameplayManagerObj = new GameObject("GameplayManager");
                _gameplayManager = gameplayManagerObj.AddComponent<GameplayManager>();
            }

            lobbyTimeoutTimer = lobbyTimeout;
            lobbyTimeoutCoroutine = StartCoroutine(LobbyTimeoutCoroutine());

            Invoke(nameof(DelayedJoinLobby), initialJoinDelay);
        }

        private void Update() {
            if (timeoutText != null && currentState == LobbyState.Waiting) {
                timeoutText.text = $"Timeout: {Mathf.CeilToInt(lobbyTimeoutTimer)}s";
                lobbyTimeoutTimer -= Time.deltaTime;
            }

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
        }

        private void InitializeFields() {
            localPlayerState = new ClientState();

            networkClient = FindObjectOfType<NetworkClient>();
            if (networkClient == null) {
                GameObject networkObj = new GameObject("NetworkClient_FromLobby");
                networkClient = networkObj.AddComponent<NetworkClient>();
            }
        }

        private void RegisterEventHandlers() {
            if (networkClient != null) {
                StartCoroutine(WaitForPacketHandlerAndRegister());
            }
        }

        private IEnumerator WaitForPacketHandlerAndRegister() {
            while (networkClient == null || networkClient.GetPacketHandler() == null) {
                if (networkClient == null && Time.timeSinceLevelLoad > 1.0f) {
                    yield break;
                }

                yield return null;
            }

            PacketHandler packetHandler = networkClient.GetPacketHandler();

            // First remove any existing handlers to avoid duplicates
            packetHandler.OnPlayerReadyStateChanged -= HandlePlayerReadyState;
            packetHandler.OnMessageReceived -= HandleLobbyMessage;
            packetHandler.OnLobbyStateReceived -= HandleLobbyState;

            // Then add our handlers
            packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyState;
            packetHandler.OnMessageReceived += HandleLobbyMessage;
            packetHandler.OnLobbyStateReceived += HandleLobbyState;
            
            Debug.Log("Lobby: Event handlers registered successfully");
        }

        private void UnregisterEventHandlers() {
            if (networkClient != null) {
                PacketHandler packetHandler = networkClient.GetPacketHandler();
                if (packetHandler != null) {
                    packetHandler.OnPlayerReadyStateChanged -= HandlePlayerReadyState;
                    packetHandler.OnMessageReceived -= HandleLobbyMessage;
                    packetHandler.OnLobbyStateReceived -= HandleLobbyState;
                }
            }
        }

        private void DelayedJoinLobby() {
            if (networkClient == null || !networkClient.IsConnected) {
                Invoke(nameof(DelayedJoinLobby), 1.0f);
                return;
            }

            initializedLobby = true;

            if (networkClient?.LocalPlayer != null) {
                localPlayerState.name = networkClient.LocalPlayer.name;
                localPlayerState.ClientId = networkClient.LocalPlayer.tag;

                if (networkClient.IsHost) {
                    isHost = true;
                    localPlayerState.ClientId = 1;
                }
                else {
                    isHost = false;
                }
            }
            else {
                SetLobbyState(LobbyState.Error);
                errorMessage = "Failed to initialize local player data.";
                UpdateStatusText();
                return;
            }

            if (isHost) {
                localPlayerState.ClientId = 1;

                if (!allPlayerStates.ContainsKey(localPlayerState.ClientId)) {
                    AddPlayerToLobby(localPlayerState);
                }
                else {
                    allPlayerStates[localPlayerState.ClientId] = localPlayerState;
                    UpdatePlayerReadyUI(localPlayerState.ClientId);
                }

                SetLobbyState(LobbyState.Waiting);
                UpdateStatusText("Hosting lobby - Waiting for players...");
            }
            else {
                SetLobbyState(LobbyState.Connecting);
                UpdateStatusText("Joining lobby...");
            }

            networkClient.JoinLobby();
        }

        private void HandleDisconnect() {
            reconnectTimer -= Time.deltaTime;

            UpdateStatusText($"Connection lost. Reconnecting in {Mathf.CeilToInt(reconnectTimer)}...");

            if (reconnectTimer <= 0f) {
                reconnectTimer = reconnectInterval;
                if (networkClient != null) {
                    networkClient.InitiateConnection();
                    UpdateStatusText("Attempting to reconnect...");
                }
                else {
                    UpdateStatusText("Cannot reconnect: NetworkClient is missing.");
                    SetLobbyState(LobbyState.Error);
                    errorMessage = "Network connection lost permanently.";
                }
            }
        }

        private void HandleLobbyState(List<LobbyStatePacket.LobbyPlayerInfo> players) {
            if (players == null) return;

            Debug.Log($"Lobby: Received lobby state with {players.Count} players");
            
            HashSet<int> receivedPlayerIds = new HashSet<int>();
            foreach (var playerInfo in players) {
                if (playerInfo == null) continue;
                receivedPlayerIds.Add(playerInfo.ClientId);

                string localPlayerName = networkClient?.LocalPlayer?.name;
                bool isThisLocalPlayer = (localPlayerName != null && localPlayerName == playerInfo.PlayerName);

                if (isThisLocalPlayer && localPlayerState != null) {
                    bool localPlayerChanged = false;
                    if (localPlayerState.ClientId != playerInfo.ClientId && playerInfo.ClientId != 0) {
                        localPlayerState.ClientId = playerInfo.ClientId;
                        localPlayerChanged = true;
                    }

                    if (localPlayerState.isReady != playerInfo.IsReady) {
                        localPlayerState.isReady = playerInfo.IsReady;
                        localPlayerChanged = true;
                    }

                    if (localPlayerState.name != playerInfo.PlayerName) {
                        localPlayerState.name = playerInfo.PlayerName;
                        localPlayerChanged = true;
                    }

                    if (!allPlayerStates.ContainsKey(localPlayerState.ClientId)) {
                        if (localPlayerState.ClientId != 0) {
                            AddPlayerToLobby(localPlayerState);
                        }
                    }
                    else {
                        allPlayerStates[localPlayerState.ClientId] = localPlayerState;
                        if (localPlayerChanged) {
                            UpdatePlayerReadyUI(localPlayerState.ClientId);
                        }
                    }
                }
                else if (!allPlayerStates.ContainsKey(playerInfo.ClientId)) {
                    ClientState newState = new ClientState();
                    newState.ClientId = playerInfo.ClientId;
                    newState.isReady = playerInfo.IsReady;
                    newState.name = playerInfo.PlayerName;
                    AddPlayerToLobby(newState);
                }
                else {
                    ClientState existingState = allPlayerStates[playerInfo.ClientId];
                    bool changed = false;
                    if (existingState.name != playerInfo.PlayerName) {
                        existingState.name = playerInfo.PlayerName;
                        changed = true;
                    }

                    if (existingState.isReady != playerInfo.IsReady) {
                        existingState.isReady = playerInfo.IsReady;
                        changed = true;
                    }

                    if (changed) {
                        UpdatePlayerReadyUI(playerInfo.ClientId);
                    }
                }
            }

            List<int> currentKnownIds = new List<int>(allPlayerStates.Keys);
            foreach (int knownId in currentKnownIds) {
                if (localPlayerState != null && knownId == localPlayerState.ClientId) continue;

                if (!receivedPlayerIds.Contains(knownId)) {
                    if (playerCards.TryGetValue(knownId, out GameObject card)) {
                        Destroy(card);
                        playerCards.Remove(knownId);
                    }

                    allPlayerStates.Remove(knownId);
                }
            }

            SetLobbyState(LobbyState.Waiting);
            UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
        }

        private void HandleLobbyMessage(string playerName, string message) {
            Debug.Log($"Lobby: Received message from {playerName}: {message}");
            
            if (message == "JOIN_LOBBY") {
                HandlePlayerJoin(playerName);
            }
            else if (message == "START_GAME") {
                OnStartGameMessageReceived();
            }
            else if (message == "REQUEST_LOBBY_STATE" && isHost) {
                BroadcastLobbyState();
            }
            else if (message.StartsWith("LOBBY_STATE:")) {
                if (!isHost) {
                    ParseLobbyState(message);
                }
            }
        }

        private void HandlePlayerJoin(string playerName) {
            if (isHost) {
                if (networkClient?.LocalPlayer != null && playerName == networkClient.LocalPlayer.name) {
                    return;
                }

                bool foundExisting = false;
                foreach (var state in allPlayerStates.Values) {
                    if (state.name == playerName) {
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting) {
                    ClientState newPlayerState = new ClientState();
                    newPlayerState.ClientId = allPlayerStates.Count > 0 ? allPlayerStates.Keys.Max() + 1 : 2;
                    newPlayerState.name = playerName;
                    AddPlayerToLobby(newPlayerState);
                    BroadcastLobbyState();
                }
            }
        }

        private void BroadcastLobbyState() {
            if (!isHost || networkClient == null || !networkClient.IsConnected) return;

            string stateMessage = "LOBBY_STATE:";
            foreach (var player in allPlayerStates) {
                stateMessage += $"{player.Key}:{player.Value.isReady}:{player.Value.name},";
            }

            networkClient.SendMessagePacket(stateMessage);
            Debug.Log($"Lobby: Host broadcasting lobby state with {allPlayerStates.Count} players");
        }

        private void ParseLobbyState(string message) {
            try {
                string[] parts = message.Substring(12).Split(',');
                Debug.Log($"Lobby: Parsing lobby state with {parts.Length} parts");

                foreach (string part in parts) {
                    if (string.IsNullOrEmpty(part)) continue;

                    string[] playerData = part.Split(':');
                    if (playerData.Length < 3) {
                        continue;
                    }

                    try {
                        int id = int.Parse(playerData[0]);
                        bool ready = bool.Parse(playerData[1]);
                        string playerName = playerData[2];

                        if (!allPlayerStates.ContainsKey(id)) {
                            ClientState newState = new ClientState();
                            newState.ClientId = id;
                            newState.isReady = ready;
                            newState.name = playerName;

                            if (networkClient?.LocalPlayer != null && networkClient.LocalPlayer.name == playerName) {
                                localPlayerState = newState;
                            }

                            AddPlayerToLobby(newState);
                        }
                        else {
                            allPlayerStates[id].isReady = ready;
                            allPlayerStates[id].name = playerName;
                            UpdatePlayerReadyUI(id);
                        }
                    }
                    catch (Exception) {
                        // Error parsing individual player data - ignore this entry
                    }
                }

                SetLobbyState(LobbyState.Waiting);
                UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
            }
            catch (Exception e) {
                Debug.LogError($"Lobby: Failed to parse lobby state: {e.Message}");
                SetLobbyState(LobbyState.Error);
                errorMessage = "Failed to parse lobby state";
                UpdateStatusText("Error: Failed to parse lobby state");
            }
        }

        private void HandlePlayerReadyState(PlayerData playerData, bool isReady) {
            if (playerData == null) return;

            Debug.Log($"Lobby: Player {playerData.name} ready state changed to {isReady}");
            
            foreach (var pair in allPlayerStates) {
                ClientState state = pair.Value;
                if (state.name == playerData.name) {
                    state.isReady = isReady;
                    UpdatePlayerReadyUI(state.ClientId);
                    break;
                }
            }

            if (isHost) {
                BroadcastLobbyState();
                CheckAllPlayersReady();
            }
        }

        public void AddPlayerToLobby(ClientState playerState) {
            if (playerState == null || playerState.ClientId == 0) return;

            if (allPlayerStates.ContainsKey(playerState.ClientId)) {
                allPlayerStates[playerState.ClientId] = playerState;
                UpdatePlayerReadyUI(playerState.ClientId);
                return;
            }

            allPlayerStates[playerState.ClientId] = playerState;

            if (playersContainer != null && playerCardPrefab != null) {
                GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();

                if (playerInLobby != null) {
                    playerInLobby.Setup(playerState);
                }

                playerCards[playerState.ClientId] = playerCard;
            }

            UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
            Debug.Log($"Lobby: Added player {playerState.name} (ID: {playerState.ClientId}) to lobby");
        }

        private void UpdatePlayerReadyUI(int clientId) {
            if (playerCards.TryGetValue(clientId, out GameObject playerCard)) {
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();
                if (playerInLobby != null) {
                    if (allPlayerStates.TryGetValue(clientId, out ClientState state)) {
                        playerInLobby.Setup(state);
                        playerInLobby.RefreshUI();
                    }
                }
            }
        }

        public void CheckAllPlayersReady() {
            if (!isHost) return;

            if (allPlayerStates.Count < minPlayersToStart) {
                Debug.Log($"Lobby: Not enough players to start ({allPlayerStates.Count}/{minPlayersToStart})");
                return;
            }

            bool allReady = true;
            foreach (var pair in allPlayerStates) {
                if (!pair.Value.isReady) {
                    allReady = false;
                    break;
                }
            }

            if (allReady && !gameplayStarted) {
                Debug.Log("Lobby: All players are ready, starting game");
                StartGame();
            }
        }

        private void StartGame() {
            if (gameplayStarted) {
                Debug.LogWarning("Lobby: Game already started");
                return;
            }

            SetLobbyState(LobbyState.Starting);

            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
            }

            if (networkClient != null && networkClient.LocalPlayer != null) {
                GameStartPacket gameStartPacket = new GameStartPacket(networkClient.LocalPlayer);
                byte[] data = gameStartPacket.Serialize();
                networkClient.GetConnection().SendData(data);
                Debug.Log("Lobby: Sent GameStart packet to server");
            }
            else {
                SetLobbyState(LobbyState.Error);
                errorMessage = "Cannot start game: NetworkClient is null";
                UpdateStatusText("Error: Cannot start game");
                return;
            }

            startGameCoroutine = StartCoroutine(StartGameCoroutine());
        }

        private IEnumerator StartGameCoroutine() {
            UpdateStatusText("Starting game...");
            yield return new WaitForSeconds(1.0f);
            StartGameplayInLobbyScene();
        }

        private void StartGameplayInLobbyScene() {
            Debug.Log("Lobby: Starting gameplay in lobby scene");

            // Only run this once
            if (gameplayStarted) {
                Debug.Log("Lobby: Gameplay already started, skipping");
                return;
            }

            gameplayStarted = true;

            // Find the GameplayManager and start gameplay
            _gameplayManager = FindObjectOfType<GameplayManager>();
            if (_gameplayManager != null) {
                Debug.Log("Lobby: Starting gameplay via GameplayManager");
                _gameplayManager.StartGameplay();
            }
            else {
                Debug.LogError("Lobby: GameplayManager not found");
                
                // Fallback method if GameplayManager is not available
                Debug.Log("Lobby: Using fallback method to start gameplay");

                // Hide lobby UI
                if (lobbyPanel != null) {
                    lobbyPanel.SetActive(false);
                    Debug.Log("Lobby: Disabled lobby panel");
                }

                // Show gameplay UI
                if (gameplayPanel != null) {
                    gameplayPanel.SetActive(true);
                    Debug.Log("Lobby: Enabled gameplay panel");
                }

                // Spawn players
                if (networkClient != null) {
                    Debug.Log("Lobby: Getting PlayerManager from NetworkClient");
                    var playerManager = networkClient.GetPlayerManager();
                    if (playerManager != null) {
                        Debug.Log("Lobby: Setting gameplay active in PlayerManager");
                        playerManager.SetGameplayActive(true);

                        Debug.Log("Lobby: Spawning local player");
                        playerManager.SpawnLocalPlayer();

                        // Enable abilities if the player has an ability manager
                        Debug.Log("Lobby: Getting local player object");
                        GameObject localPlayer = playerManager.GetLocalPlayerObject();
                        if (localPlayer != null) {
                            Debug.Log("Lobby: Finding AbilityManager on local player");
                            __SAE.Leonardo.Scripts.Abilities.AbilityManager abilityManager =
                                localPlayer.GetComponent<__SAE.Leonardo.Scripts.Abilities.AbilityManager>();
                            if (abilityManager != null) {
                                Debug.Log("Lobby: Enabling abilities");
                                abilityManager.SetAbilitiesEnabled(true);
                            }
                            else {
                                Debug.LogError("Lobby: No AbilityManager found on local player");
                            }
                        }
                        else {
                            Debug.LogError("Lobby: Local player object not found");
                        }
                    }
                    else {
                        Debug.LogError("Lobby: PlayerManager not found");
                    }
                }
                else {
                    Debug.LogError("Lobby: NetworkClient not found");
                }
            }

            Debug.Log("Lobby: Gameplay started successfully");
        }

        /// <summary>
        /// Called when a START_GAME message is received from the network.
        /// Initiates gameplay in the lobby scene for clients.
        /// </summary>
        public void OnStartGameMessageReceived() {
            Debug.Log("Lobby: START_GAME message received");

            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
                Debug.Log("Lobby: Stopped lobby timeout coroutine");
            }

            SetLobbyState(LobbyState.Starting);
            Debug.Log("Lobby: Set lobby state to Starting");

            // For clients, this is the critical path to start the game
            StartGameplayInLobbyScene();
        }

        private IEnumerator LobbyTimeoutCoroutine() {
            while (lobbyTimeoutTimer > 0) {
                yield return new WaitForSeconds(1f);
                lobbyTimeoutTimer -= 1f;
            }

            if (isHost) {
                if (allPlayerStates.Count >= minPlayersToStart) {
                    Debug.Log("Lobby: Timeout reached with enough players, starting game");
                    StartGame();
                }
                else {
                    Debug.LogWarning("Lobby: Timeout reached without enough players");
                    SetLobbyState(LobbyState.Error);
                    errorMessage = "Timeout reached without enough players";
                    UpdateStatusText("Error: Not enough players joined");
                }
            }
            else {
                Debug.LogWarning("Lobby: Lobby timed out as client");
                SetLobbyState(LobbyState.Error);
                errorMessage = "Lobby timed out";
                UpdateStatusText("Error: Lobby timed out");
            }
        }

        private void SetLobbyState(LobbyState newState) {
            if (currentState == newState) return;
            
            Debug.Log($"Lobby: State changing from {currentState} to {newState}");
            currentState = newState;
        }

        private void UpdateStatusText(string message = null) {
            if (statusText == null) return;

            if (message != null) {
                statusText.text = message;
                return;
            }

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
    }
}