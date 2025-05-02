using System;
using System.Collections.Generic;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Dyson.GPG222.Lobby
{
    public class Lobby : MonoBehaviour
    {
        [Header("Lobby Configuration")]
        [SerializeField] private string gameSceneName = "Gameplay";
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float initialJoinDelay = 0.5f;

        [Header("Player States")]
        public ClientState localPlayerState;
        private NetworkClient networkClient;
        private Dictionary<int, ClientState> allPlayerStates = new Dictionary<int, ClientState>();
        private Dictionary<int, GameObject> playerCards = new Dictionary<int, GameObject>();
        
        [Header("UI References")]
        [SerializeField] private Transform playersContainer;
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private TextMeshProUGUI statusText;

        private bool isHost;
        private bool initializedLobby = false;
        private float reconnectTimer = 0f;
        private float reconnectInterval = 5f;

        public ClientState LocalPlayerClientState => localPlayerState;

        private void Awake()
        {
            isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
            
            // Create local player state
            localPlayerState = gameObject.AddComponent<ClientState>();
            localPlayerState.isReady = false;
            
            // Find or create the NetworkClient
            networkClient = FindObjectOfType<NetworkClient>();
            if (networkClient == null)
            {
                Debug.LogError("Lobby: NetworkClient not found! Creating one...");
                GameObject networkObj = new GameObject("NetworkClient");
                networkClient = networkObj.AddComponent<NetworkClient>();
            }
        }
        
        private void Start()
        {
            if (statusText != null)
            {
                statusText.text = "Connecting to lobby...";
            }
            
            // Register event handlers for network messages
            if (networkClient != null)
            {
                PacketHandler packetHandler = networkClient.GetPacketHandler();
                if (packetHandler != null)
                {
                    packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyState;
                    packetHandler.OnMessageReceived += HandleLobbyMessage;
                }
                else
                {
                    Debug.LogError("Lobby: PacketHandler is null!");
                }
            }
            else
            {
                Debug.LogError("Lobby: NetworkClient is still null after Start!");
            }
            
            // Delay joining the lobby to ensure network is ready
            Invoke("DelayedJoinLobby", initialJoinDelay);
        }
        
        private void Update()
        {
            // Monitor connection status
            if (networkClient != null && !networkClient.IsConnected && initializedLobby)
            {
                reconnectTimer -= Time.deltaTime;
                
                if (statusText != null)
                {
                    statusText.text = $"Connection lost. Reconnecting in {Mathf.CeilToInt(reconnectTimer)}...";
                }
                
                if (reconnectTimer <= 0f)
                {
                    reconnectTimer = reconnectInterval;
                    networkClient.InitiateConnection();
                    
                    if (statusText != null)
                    {
                        statusText.text = "Attempting to reconnect...";
                    }
                }
            }
        }
        
        private void DelayedJoinLobby()
        {
            if (networkClient == null || !networkClient.IsConnected)
            {
                Debug.LogWarning("Lobby: Network not ready, retrying in 1 second...");
                Invoke("DelayedJoinLobby", 1.0f);
                return;
            }
            
            initializedLobby = true;
            
            if (isHost)
            {
                // Host sets up their own player first
                Debug.Log("Lobby: Host is setting up player with ID 1");
                localPlayerState.ClientId = 1;
                AddPlayerToLobby(localPlayerState);
                
                if (statusText != null)
                {
                    statusText.text = "Hosting lobby - Waiting for players...";
                }
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Joining lobby...";
                }
            }
            
            // Send lobby join request
            networkClient.JoinLobby();
            Debug.Log("Lobby: Sent lobby join request");
        }
        
        private void HandleLobbyMessage(string playerName, string message)
        {
            Debug.Log($"Lobby: Received message from {playerName}: {message}");
            
            if (message == "JOIN_LOBBY")
            {
                Debug.Log($"Lobby: Player {playerName} joined the lobby");
                if (isHost)
                {
                    // Find if player already exists
                    bool foundExisting = false;
                    foreach (var state in allPlayerStates.Values)
                    {
                        if (state.name == playerName)
                        {
                            foundExisting = true;
                            break;
                        }
                    }
                    
                    // Add new player if they don't exist
                    if (!foundExisting)
                    {
                        ClientState newPlayerState = gameObject.AddComponent<ClientState>();
                        newPlayerState.ClientId = allPlayerStates.Count + 1;
                        newPlayerState.name = playerName;
                        AddPlayerToLobby(newPlayerState);
                        
                        Debug.Log($"Lobby: Added new player {playerName} with ID {newPlayerState.ClientId}");
                        
                        // Broadcast updated lobby state
                        BroadcastLobbyState();
                    }
                }
            }
            else if (message == "START_GAME")
            {
                Debug.Log("Lobby: Received START_GAME message");
                OnStartGameMessageReceived();
            }
            else if (message.StartsWith("LOBBY_STATE:"))
            {
                if (!isHost)
                {
                    ParseLobbyState(message);
                }
            }
        }
        
        private void BroadcastLobbyState()
        {
            if (!isHost || networkClient == null || !networkClient.IsConnected) return;
            
            string stateMessage = "LOBBY_STATE:";
            foreach (var player in allPlayerStates)
            {
                // Format: ClientID:isReady:playerName,
                stateMessage += $"{player.Key}:{player.Value.isReady}:{player.Value.name},";
            }
            
            Debug.Log($"Lobby: Broadcasting state: {stateMessage}");
            networkClient.SendMessagePacket(stateMessage);
        }
        
        private void ParseLobbyState(string message)
        {
            Debug.Log($"Lobby: Parsing lobby state: {message}");
            
            // Format: "LOBBY_STATE:1:true:PlayerName,2:false:OtherPlayer,"
            string[] parts = message.Substring(12).Split(',');
            
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                string[] playerData = part.Split(':');
                if (playerData.Length >= 2)
                {
                    int id = int.Parse(playerData[0]);
                    bool ready = bool.Parse(playerData[1]);
                    string playerName = playerData.Length >= 3 ? playerData[2] : $"Player {id}";
                    
                    if (!allPlayerStates.ContainsKey(id))
                    {
                        // New player
                        ClientState newState = gameObject.AddComponent<ClientState>();
                        newState.ClientId = id;
                        newState.isReady = ready;
                        newState.name = playerName;
                        
                        // Check if this is our local player
                        if (networkClient != null && networkClient.LocalPlayer != null && 
                            networkClient.LocalPlayer.name == playerName)
                        {
                            Debug.Log($"Lobby: Found our local player with ID {id}");
                            localPlayerState = newState;
                        }
                        
                        AddPlayerToLobby(newState);
                    }
                    else
                    {
                        // Update existing player
                        allPlayerStates[id].isReady = ready;
                        allPlayerStates[id].name = playerName;
                        UpdatePlayerReadyUI(id);
                    }
                }
            }
            
            // Update the status text
            if (statusText != null)
            {
                statusText.text = $"In Lobby - {allPlayerStates.Count} players";
            }
        }
        
        private void HandlePlayerReadyState(PlayerData playerData, bool isReady)
        {
            Debug.Log($"Lobby: Player {playerData.name} ready state changed to {isReady}");
            
            // Find the player by name and tag
            foreach (var pair in allPlayerStates)
            {
                ClientState state = pair.Value;
                
                // Match by name since player tags are random
                if (state.name == playerData.name)
                {
                    state.isReady = isReady;
                    UpdatePlayerReadyUI(state.ClientId);
                    Debug.Log($"Lobby: Updated player {playerData.name} ready state to {isReady}");
                    break;
                }
            }
            
            if (isHost)
            {
                BroadcastLobbyState();
                CheckAllPlayersReady();
            }
        }
        
        private void UpdatePlayerReadyUI(int clientId)
        {
            if (playerCards.TryGetValue(clientId, out GameObject playerCard))
            {
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();
                if (playerInLobby != null)
                {
                    playerInLobby.RefreshUI();
                }
            }
        }
        
        public void AddPlayerToLobby(ClientState playerState)
        {
            if (allPlayerStates.ContainsKey(playerState.ClientId))
            {
                Debug.LogWarning($"Lobby: Player with ID {playerState.ClientId} already exists!");
                return;
            }
            
            allPlayerStates[playerState.ClientId] = playerState;
            
            if (playersContainer != null && playerCardPrefab != null)
            {
                GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
                PlayerInLobby playerInLobby = playerCard.GetComponent<PlayerInLobby>();
                
                if (playerInLobby != null)
                {
                    playerInLobby.Setup(playerState);
                }
                
                playerCards[playerState.ClientId] = playerCard;
            }
            
            // Update the status text
            if (statusText != null)
            {
                statusText.text = $"In Lobby - {allPlayerStates.Count} players";
            }
        }
        
        public void CheckAllPlayersReady()
        {
            if (!isHost) return;
            
            Debug.Log($"Lobby: Checking if all {allPlayerStates.Count} players are ready (min: {minPlayersToStart})");
            
            if (allPlayerStates.Count < minPlayersToStart)
            {
                Debug.Log("Lobby: Not enough players to start");
                return;
            }
            
            bool allReady = true;
            foreach (var pair in allPlayerStates)
            {
                if (!pair.Value.isReady)
                {
                    allReady = false;
                    Debug.Log($"Lobby: Player {pair.Value.name} (ID: {pair.Key}) is not ready");
                    break;
                }
            }
            
            if (allReady)
            {
                Debug.Log("Lobby: All players ready, starting game!");
                StartGame();
            }
        }
        
        private void StartGame()
        {
            if (networkClient != null)
            {
                networkClient.SendMessagePacket("START_GAME");
            }
            
            // Wait a moment to ensure the message is sent
            Invoke("LoadGameScene", 0.5f);
        }
        
        private void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }
        
        public void OnStartGameMessageReceived()
        {
            Debug.Log("Lobby: Start game message received, loading game scene");
            LoadGameScene();
        }
        
        private void OnDestroy()
        {
            if (networkClient != null)
            {
                PacketHandler packetHandler = networkClient.GetPacketHandler();
                if (packetHandler != null)
                {
                    packetHandler.OnPlayerReadyStateChanged -= HandlePlayerReadyState;
                    packetHandler.OnMessageReceived -= HandleLobbyMessage;
                }
            }
        }
    }
}