using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dyson.GPG222.Lobby
{
    public class Lobby : MonoBehaviour
    {
        [Header("- Lobby Configuration")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private int minPlayersToStart = 2;

        [Header("- Player States")]
        public ClientState localPlayerState;
        private NetworkClient networkClient;
        private List<ClientState> allPlayerStates = new List<ClientState>();
        
        [Header("- UI References")]
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
                
                networkClient.JoinLobby();
            }
            
            if (isHost)
            {
                AddPlayerToLobby(localPlayerState);
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
                    AddPlayerToLobby(newPlayerState);
                }
            }
        }
        
        private void HandlePlayerReadyState(PlayerData playerData, bool isReady)
        {
            foreach (ClientState state in allPlayerStates)
            {
                state.isReady = isReady;
            }
            
            if (isHost)
            {
                CheckAllPlayersReady();
            }
        }
        
        public void AddPlayerToLobby(ClientState playerState)
        {
            allPlayerStates.Add(playerState);
            
            if (playersContainer != null && playerCardPrefab != null)
            {
                GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            }
        }
        
        public void CheckAllPlayersReady()
        {
            if (!isHost || allPlayerStates.Count < minPlayersToStart)
            {
                return;
            }
            
            bool allReady = true;
            foreach (ClientState state in allPlayerStates)
            {
                if (!state.isReady)
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
            Debug.Log("All players ready, starting game!");
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