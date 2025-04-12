using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;
using UnityEngine.UI;

namespace Dyson.GPG222.Lobby
{
    public class Lobby : MonoBehaviour
    {
        public List<ClientState> players;
        public bool hasGameStarted;

        public GameObject playerLobbyUI;

        public Transform containerParent;
        
        public ClientState testPlayer;

        private GameObject playerUI;
        public ButtonColorChange _lobbyButton;
        private void PlayerJoinedLobby()
        {
            
        }
        
        public void AddPlayerToLobby(ClientState newPlayerState)
        {
            Debug.Log($"[Lobby] Adding player {newPlayerState.ClientId}");
            players.Add(newPlayerState);
            playerUI = Instantiate(playerLobbyUI, containerParent);
            PlayerInLobby display = playerUI.GetComponent<PlayerInLobby>();

            if (display != null)
            {
                display.Setup(newPlayerState);
                Debug.Log($"[Lobby] Player UI setup for {newPlayerState.ClientId}");
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