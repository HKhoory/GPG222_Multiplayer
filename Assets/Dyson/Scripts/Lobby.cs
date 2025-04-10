using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
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
        
        public ClientState testPlayer;
        

        private void PlayerJoinedLobby()
        {
            
        }
        
        public void AddPlayerToLobby(ClientState newPlayerState, TcpClient client, int playerId)
        {
            players.Add(newPlayerState);
            testPlayer.Client = client;
            testPlayer.ClientId = playerId;
            testPlayer.isReady = false;
            GameObject playerUI = Instantiate(newPlayer, containerParent);

            PlayerInLobby display = playerUI.GetComponent<PlayerInLobby>();

            if (display != null)
            {
                display.Setup(newPlayerState);
            }
        }
        public void ReadyButton()
        {
            testPlayer.isReady = true;
        }

        private void CreateLobby()
        {
            string lobbyName = "TestLobby";
            int maxPlayers = 4;
        }
    }
}