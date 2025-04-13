using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Dyson_GPG222_Server;
using Dyson.GPG222.Lobby.Dyson.GPG222.Lobby;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
using Leonardo.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

namespace Dyson.GPG222.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        public GameObject joinLobbyCanvas;
        public GameObject lobbyCanvas;
        public List<ClientState> players;
        public ClientState clientPlayer;
        public Lobby _lobby;
        public TcpClient client;
        public LobbyPacket _lobbyPacket;
        public NetworkConnection _networkConnection;
        [SerializeField] private NetworkClient _networkClient;
        private PlayerData _playerData;
        public ButtonColorChange _lobbyButton;
        public Server _server;
        public PlayerManager _playerManager;

        private void Awake()
        {
            _server = FindObjectOfType<Server>();
        }

        private void Start()
        {
           // clientPlayer = new ClientState();
           _networkClient = FindObjectOfType<NetworkClient>();
            if (_lobby == null)
            {
                _lobby = FindObjectOfType<Lobby>();
            }
            
            _lobbyPacket = new LobbyPacket(_playerData);
        }

        public void JoinLobbyButton()
        {
            joinLobbyCanvas.SetActive(false);
            lobbyCanvas.SetActive(true);
            Debug.Log("Who joined my lobby?: " + clientPlayer.ClientId);

            _playerData = new PlayerData("Testing", 1);
            _lobbyPacket = new LobbyPacket(_playerData);
            _networkClient.OnLobbyConnected();
            if (_lobby != null)
            {
                _lobby.AddPlayerToLobby(clientPlayer);
                Debug.Log(clientPlayer);
                Debug.Log(clientPlayer.ClientId);
                Debug.Log("Player added to lobby");
               // Invoke("StartGame", 2);
            }
        }

        public void StartGame()
        {
            Debug.Log("Transitioning to gameplay");
            SceneManager.LoadScene("Scenes/Client");
        }
    }
}
