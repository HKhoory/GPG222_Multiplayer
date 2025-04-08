using System;
using System.Collections;
using System.Collections.Generic;
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

        private void Start()
        {
            playersId = new ClientState();
        }

        public void JoinLobbyButton()
        {
            joinLobbyCanvas.SetActive(false);
            lobbyCanvas.SetActive(true);
            Debug.Log("Who joined my lobby?: " + playersId.ClientId);
        }
    }
}
