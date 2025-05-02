using System;
using System.Collections.Generic;
using _Studens.Leonardo.Scripts.ClientRelated;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dyson.GPG222.Lobby
{
    public class Lobby : MonoBehaviour
    {
        [Header("Lobby Configuration")]
        [SerializeField] private string gameSceneName = "Gameplay";
        [SerializeField] private int minPlayersToStart = 2;

        [Header("Player States")]
        public ClientState localPlayerState;
        private NetworkClient networkClient;
        private Dictionary<int, ClientState> allPlayerStates = new Dictionary<int, ClientState>();
        private Dictionary<int, GameObject> playerCards = new Dictionary<int, GameObject>();
        
        [Header("UI References")]
        [SerializeField] private Transform playersContainer;
        [SerializeField] private GameObject playerCardPrefab;

        private bool isHost;
        public ClientState localPlayerClientState => localPlayerState;
        public ClientState testPlayer => localPlayerState;

        private void Awake()
        {
            isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
            localPlayerState = gameObject.AddComponent<ClientState>();
            networkClient = FindObjectOfType<NetworkClient>();
        }
        
        private void Start()
        {
            if (networkClient != null)
            {
                PacketHandler packetHandler = networkClient.GetPacketHandler();
                if (packetHandler != null)
                {
                    packetHandler.OnPlayerReadyStateChanged += HandlePlayerReadyState;
                    packetHandler.OnMessageReceived += HandleLobbyMessage;
                }
            }
            
            Invoke("DelayedJoinLobby", 0.5f);
        }
        
        private void DelayedJoinLobby()
        {
            if (isHost)
            {
                localPlayerState.ClientId = 1;
                AddPlayerToLobby(localPlayerState);
            }
            
            if (networkClient != null)
            {
                networkClient.JoinLobby();
            }
        }
        
        private void HandleLobbyMessage(string playerName, string message)
        {
            if (message == "JOIN_LOBBY")
            {
                Debug.Log($"Player {playerName} joined the lobby");
                if (isHost)
                {
                    ClientState newPlayerState = gameObject.AddComponent<ClientState>();
                    newPlayerState.ClientId = allPlayerStates.Count + 1;
                    AddPlayerToLobby(newPlayerState);
                    BroadcastLobbyState();
                }
            }
            else if (message == "PLAYER_READY")
            {
                // Leo: this is handled by the OnPlayerReadyStateChanged event.
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
            if (!isHost || networkClient == null) return;
            
            string stateMessage = "LOBBY_STATE:";
            foreach (var player in allPlayerStates)
            {
                stateMessage += $"{player.Key}:{player.Value.isReady},";
            }
            
            networkClient.SendMessagePacket(stateMessage);
        }
        
        private void ParseLobbyState(string message)
        {
            // Leo: the format is: "LOBBY_STATE:1:true,2:false,"
            string[] parts = message.Substring(12).Split(',');
            
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                string[] playerData = part.Split(':');
                if (playerData.Length == 2)
                {
                    int id = int.Parse(playerData[0]);
                    bool ready = bool.Parse(playerData[1]);
                    
                    if (!allPlayerStates.ContainsKey(id))
                    {
                        ClientState newState = gameObject.AddComponent<ClientState>();
                        newState.ClientId = id;
                        newState.isReady = ready;
                        AddPlayerToLobby(newState);
                    }
                    else
                    {
                        allPlayerStates[id].isReady = ready;
                        UpdatePlayerReadyUI(id);
                    }
                }
            }
        }
        
        private void HandlePlayerReadyState(PlayerData playerData, bool isReady)
        {
            int playerTag = playerData.tag;
            
            foreach (var pair in allPlayerStates)
            {
                ClientState state = pair.Value;
                if (state.ClientId == playerTag)
                {
                    state.isReady = isReady;
                    UpdatePlayerReadyUI(state.ClientId);
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
        }
        
        public void CheckAllPlayersReady()
        {
            if (!isHost || allPlayerStates.Count < minPlayersToStart)
            {
                return;
            }
            
            bool allReady = true;
            foreach (var pair in allPlayerStates)
            {
                if (!pair.Value.isReady)
                {
                    allReady = false;
                    break;
                }
            }
            
            if (allReady)
            {
                StartGame();
            }
        }
        
        private void StartGame()
        {
            Debug.Log("Lobby.cs: All players ready, starting game!");
            if (networkClient != null)
            {
                networkClient.SendMessagePacket("START_GAME");
            }
            
            SceneManager.LoadScene(gameSceneName);
        }
        
        public void OnStartGameMessageReceived()
        {
            SceneManager.LoadScene(gameSceneName);
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