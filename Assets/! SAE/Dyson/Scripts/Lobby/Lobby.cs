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
        #region Serialized Fields

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

        #endregion

        #region Private Fields

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

        #endregion

        #region Public Properties

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

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Called when the script instance is being loaded. Initializes fields.
        /// </summary>
        private void Awake() {
            InitializeFields();
        }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
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

        /// <summary>
        /// Called every frame. Updates timers and handles disconnection checks.
        /// </summary>
        private void Update() {
            if (timeoutText != null && currentState == LobbyState.Waiting) {
                timeoutText.text = $"Timeout: {Mathf.CeilToInt(lobbyTimeoutTimer)}s";
                lobbyTimeoutTimer -= Time.deltaTime;
            }

            if (networkClient != null && !networkClient.IsConnected && initializedLobby) {
                HandleDisconnect();
            }
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed. Unregisters event handlers and stops coroutines.
        /// </summary>
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

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes fields and references needed by the Lobby script.
        /// </summary>
        private void InitializeFields() {
            localPlayerState = new ClientState();

            networkClient = FindObjectOfType<NetworkClient>();
            if (networkClient == null) {
                GameObject networkObj = new GameObject("NetworkClient_FromLobby");
                networkClient = networkObj.AddComponent<NetworkClient>();
            }
        }

        /// <summary>
        /// Subscribes lobby methods to relevant network events from the PacketHandler.
        /// </summary>
        private void RegisterEventHandlers() {
            if (networkClient != null) {
                StartCoroutine(WaitForPacketHandlerAndRegister());
            }
        }

        /// <summary>
        /// Coroutine that waits until the PacketHandler is available and then registers event handlers.
        /// </summary>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator WaitForPacketHandlerAndRegister() {
            while (networkClient == null || networkClient.GetPacketHandler() == null) {
                if (networkClient == null && Time.timeSinceLevelLoad > 1.0f) {
                    yield break;
                }

                yield return null;
            }

            PacketHandler packetHandler = networkClient.GetPacketHandler();

            packetHandler.OnPlayerReadyStateChanged -= HandlePlayerReadyState;
            packetHandler.OnMessageReceived -= HandleLobbyMessage;
            packetHandler.OnLobbyStateReceived -= HandleLobbyState;

            packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyState;
            packetHandler.OnMessageReceived += HandleLobbyMessage;
            packetHandler.OnLobbyStateReceived += HandleLobbyState;
        }

        /// <summary>
        /// Unsubscribes lobby methods from network events to prevent memory leaks.
        /// </summary>
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

        #endregion

        #region Lobby Connection Methods

        /// <summary>
        /// Attempts to join the lobby after a delay to ensure the network connection is established.
        /// Determines host status and sends the join request.
        /// </summary>
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

        /// <summary>
        /// Handles the logic when a network disconnection is detected, potentially attempting reconnection.
        /// </summary>
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

        #endregion

        #region Message Handling Methods

        /// <summary>
        /// Processes the complete lobby state received from the server, updating local state and UI.
        /// </summary>
        /// <param name="players">A list of player information objects representing the current lobby state.</param>
        private void HandleLobbyState(List<LobbyStatePacket.LobbyPlayerInfo> players) {
            if (players == null) return;

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

            UpdateStatusText($"In Lobby - {allPlayerStates.Count} players");
        }

        /// <summary>
        /// Handles generic lobby-related messages received from the network.
        /// Dispatches actions based on message content (e.g., JOIN_LOBBY, START_GAME).
        /// </summary>
        /// <param name="playerName">The name of the player who sent the message.</param>
        /// <param name="message">The message content.</param>
        private void HandleLobbyMessage(string playerName, string message) {
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

        /// <summary>
        /// Handles the logic when a message indicates a new player has joined.
        /// If this instance is the host, it adds the player and broadcasts the updated state.
        /// </summary>
        /// <param name="playerName">The name of the player who joined.</param>
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

        /// <summary>
        /// Broadcasts the current lobby state (list of players and statuses) to all connected clients.
        /// Only executed by the host.
        /// </summary>
        private void BroadcastLobbyState() {
            if (!isHost || networkClient == null || !networkClient.IsConnected) return;

            string stateMessage = "LOBBY_STATE:";
            foreach (var player in allPlayerStates) {
                stateMessage += $"{player.Key}:{player.Value.isReady}:{player.Value.name},";
            }

            networkClient.SendMessagePacket(stateMessage);
        }

        /// <summary>
        /// Parses the lobby state information received within a generic message string.
        /// Updates the local lobby state based on the parsed data. Used by clients.
        /// </summary>
        /// <param name="message">The message string containing the lobby state data.</param>
        private void ParseLobbyState(string message) {
            try {
                string[] parts = message.Substring(12).Split(',');

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
            catch (Exception) {
                SetLobbyState(LobbyState.Error);
                errorMessage = "Failed to parse lobby state";
                UpdateStatusText("Error: Failed to parse lobby state");
            }
        }

        /// <summary>
        /// Handles updates to a player's ready status received from the network.
        /// Updates the internal state and UI, and triggers checks if the host.
        /// </summary>
        /// <param name="playerData">The PlayerData object associated with the status change.</param>
        /// <param name="isReady">The new ready status.</param>
        private void HandlePlayerReadyState(PlayerData playerData, bool isReady) {
            if (playerData == null) return;

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

        #endregion

        #region Player Management Methods

        /// <summary>
        /// Adds a player's state to the internal dictionary and creates the corresponding UI element.
        /// </summary>
        /// <param name="playerState">The ClientState object representing the player to add.</param>
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
        }

        /// <summary>
        /// Refreshes the UI element (player card) associated with a specific client ID.
        /// </summary>
        /// <param name="clientId">The client ID of the player whose UI needs updating.</param>
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

        /// <summary>
        /// Checks if the minimum number of players have joined and if all players are ready.
        /// If conditions are met, initiates the game start sequence. Only executed by the host.
        /// </summary>
        public void CheckAllPlayersReady() {
            if (!isHost) return;

            if (allPlayerStates.Count < minPlayersToStart) {
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
                StartGame();
            }
        }

        #endregion

        #region Game Start Methods

        /// <summary>
        /// Initiates the process of starting the game by changing state and notifying clients.
        /// </summary>
        private void StartGame() {
            SetLobbyState(LobbyState.Starting);

            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
            }

            if (networkClient != null) {
                networkClient.SendMessagePacket("START_GAME");
            }
            else {
                SetLobbyState(LobbyState.Error);
                errorMessage = "Cannot start game: NetworkClient is null";
                UpdateStatusText("Error: Cannot start game");
                return;
            }

            startGameCoroutine = StartCoroutine(StartGameCoroutine());
        }

        /// <summary>
        /// Coroutine that waits briefly before starting gameplay in the current scene.
        /// </summary>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator StartGameCoroutine() {
            UpdateStatusText("Starting game...");
            yield return new WaitForSeconds(1.0f);

            StartGameplayInLobbyScene();
        }

        /// <summary>
        /// Spawns the player PREFABS in the lobby scene, for players to control them later.
        /// </summary>
        private void StartGameplayInLobbyScene() {
            Debug.Log("StartGameplayInLobbyScene: Starting...");

            // Only run this once
            if (gameplayStarted) {
                Debug.Log("StartGameplayInLobbyScene: Already started, returning");
                return;
            }

            gameplayStarted = true;

            Debug.Log("StartGameplayInLobbyScene: Finding GameplayManager");
            _gameplayManager = FindFirstObjectByType<GameplayManager>();

            if (_gameplayManager != null) {
                Debug.Log("StartGameplayInLobbyScene: Found GameplayManager, calling StartGameplay()");
                _gameplayManager.StartGameplay();
                Debug.Log("StartGameplayInLobbyScene: StartGameplay() completed");
            }
            else {
                Debug.LogError("StartGameplayInLobbyScene: GameplayManager not found, using fallback method");

                // Fallback method if GameplayManager is not available.
                Debug.Log("StartGameplayInLobbyScene: Using fallback method");

                // Hide lobby UI
                if (lobbyPanel != null) {
                    lobbyPanel.SetActive(false);
                    Debug.Log("StartGameplayInLobbyScene: Disabled lobby panel");
                }

                // Show gameplay UI
                if (gameplayPanel != null) {
                    gameplayPanel.SetActive(true);
                    Debug.Log("StartGameplayInLobbyScene: Enabled gameplay panel");
                }

                // Spawn players
                if (networkClient != null) {
                    Debug.Log("StartGameplayInLobbyScene: NetworkClient found, getting PlayerManager");
                    var playerManager = networkClient.GetPlayerManager();
                    if (playerManager != null) {
                        Debug.Log("StartGameplayInLobbyScene: PlayerManager found, setting gameplay active");
                        playerManager.SetGameplayActive(true);

                        Debug.Log("StartGameplayInLobbyScene: Spawning local player");
                        playerManager.SpawnLocalPlayer();

                        // Enable abilities if the player has an ability manager
                        Debug.Log("StartGameplayInLobbyScene: Getting local player object");
                        GameObject localPlayer = playerManager.GetLocalPlayerObject();
                        if (localPlayer != null) {
                            Debug.Log("StartGameplayInLobbyScene: Local player found, finding AbilityManager");
                            __SAE.Leonardo.Scripts.Abilities.AbilityManager abilityManager =
                                localPlayer.GetComponent<__SAE.Leonardo.Scripts.Abilities.AbilityManager>();
                            if (abilityManager != null) {
                                Debug.Log("StartGameplayInLobbyScene: AbilityManager found, enabling abilities");
                                abilityManager.SetAbilitiesEnabled(true);
                                Debug.Log("StartGameplayInLobbyScene: Abilities enabled");
                            }
                            else {
                                Debug.LogError("StartGameplayInLobbyScene: No AbilityManager found on local player");
                            }
                        }
                        else {
                            Debug.LogError("StartGameplayInLobbyScene: Local player object not found");
                        }
                    }
                    else {
                        Debug.LogError("StartGameplayInLobbyScene: PlayerManager not found");
                    }
                }
                else {
                    Debug.LogError("StartGameplayInLobbyScene: NetworkClient not found");
                }
            }

            Debug.Log("StartGameplayInLobbyScene: Completed");
        }

        /// <summary>
        /// Called when a START_GAME message is received from the network. Initiates gameplay in the lobby scene.
        /// </summary>
        public void OnStartGameMessageReceived() {
            Debug.Log("START_GAME message received by Lobby");

            if (lobbyTimeoutCoroutine != null) {
                StopCoroutine(lobbyTimeoutCoroutine);
                lobbyTimeoutCoroutine = null;
                Debug.Log("Stopped lobby timeout coroutine");
            }

            SetLobbyState(LobbyState.Starting);
            Debug.Log("Set lobby state to Starting");

            // Start gameplay in the lobby scene
            StartGameplayInLobbyScene();
        }

        /// <summary>
        /// Coroutine that handles the lobby timeout logic. If time expires, the host may start the game or an error occurs.
        /// </summary>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator LobbyTimeoutCoroutine() {
            while (lobbyTimeoutTimer > 0) {
                yield return new WaitForSeconds(1f);
                lobbyTimeoutTimer -= 1f;
            }

            if (isHost) {
                if (allPlayerStates.Count >= minPlayersToStart) {
                    StartGame();
                }
                else {
                    SetLobbyState(LobbyState.Error);
                    errorMessage = "Timeout reached without enough players";
                    UpdateStatusText("Error: Not enough players joined");
                }
            }
            else {
                SetLobbyState(LobbyState.Error);
                errorMessage = "Lobby timed out";
                UpdateStatusText("Error: Lobby timed out");
            }
        }

        #endregion

        #region State Management Methods

        /// <summary>
        /// Sets the internal state of the lobby.
        /// </summary>
        /// <param name="newState">The new LobbyState to transition to.</param>
        private void SetLobbyState(LobbyState newState) {
            currentState = newState;

            switch (newState) {
                case LobbyState.Initializing:
                    break;
                case LobbyState.Connecting:
                    break;
                case LobbyState.Waiting:
                    break;
                case LobbyState.Starting:
                    break;
                case LobbyState.Error:
                    break;
            }
        }

        /// <summary>
        /// Updates the status text displayed in the UI, either with a specific message or based on the current lobby state.
        /// </summary>
        /// <param name="message">An optional specific message to display. If null, uses a default message for the current state.</param>
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

        #endregion
    }
}