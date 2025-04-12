using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;

namespace Dyson.GPG222.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        public GameObject joinLobbyCanvas;
        public GameObject lobbyCanvas;
        public List<ClientState> players;
        public ClientState playersId;
        public Lobby _lobby;
        public TcpClient client;
        public LobbyPacket _lobbyPacket;
        private void Start()
        {
            playersId = new ClientState();
            
            if (_lobby == null)
            {
                _lobby = FindObjectOfType<Lobby>();
            }
        }

        public void JoinLobbyButton()
        {
            joinLobbyCanvas.SetActive(false);
            lobbyCanvas.SetActive(true);
            Debug.Log("Who joined my lobby?: " + playersId.ClientId);

            _lobbyPacket.SendLobbyPacket();
            if (_lobby != null)
            {
                _lobby.AddPlayerToLobby(playersId, client, playersId.ClientId);
                Debug.Log(playersId);
                Debug.Log(playersId.ClientId);
                Debug.Log("Player added to lobby");
            }
        }
    }
}
