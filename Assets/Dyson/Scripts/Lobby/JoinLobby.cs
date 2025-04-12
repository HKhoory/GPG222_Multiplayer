using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Hamad.Scripts;
using Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Networking;
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
        private NetworkConnection _networkConnection;
        private PlayerData _playerData;
        public ButtonColorChange _lobbyButton;
        private void Start()
        {
           // clientPlayer = new ClientState();
            
            if (_lobby == null)
            {
                _lobby = FindObjectOfType<Lobby>();
            }
            
            _lobbyPacket = new LobbyPacket();

        }

        public void JoinLobbyButton()
        {
            joinLobbyCanvas.SetActive(false);
            lobbyCanvas.SetActive(true);
            Debug.Log("Who joined my lobby?: " + clientPlayer.ClientId);

           // _lobbyPacket.SendLobbyPacket();
            if (_lobby != null)
            {
                _lobby.AddPlayerToLobby(clientPlayer);
                Debug.Log(clientPlayer);
                Debug.Log(clientPlayer.ClientId);
                Debug.Log("Player added to lobby");
                Invoke("StartGame", 2);   
            }
        }

        public void StartGame()
        {
            Debug.Log("Transitioning to gameplay");
            SceneManager.LoadScene("Scenes/Client");
        }
    }
}
