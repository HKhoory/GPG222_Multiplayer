using System;
using System.Collections;
using System.Collections.Generic;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;

namespace Dyson.GPG222.Lobby
{
    public class Lobby : MonoBehaviour
    {
        public List<ClientState> players;
        public bool hasGameStarted;

        public GameObject newPlayer;

        public Transform containerParent;
        // Update is called once per frame

        private void Start()
        {
            players = new List<ClientState>();
        }

        void Update()
        {
            PlayerJoinedLobby();
        }

        private void PlayerJoinedLobby()
        {
            Instantiate(newPlayer, containerParent);
        }

        private void CreateLobby()
        {
            string lobbyName = "TestLobby";
            int maxPlayers = 4;
        }
    }
}