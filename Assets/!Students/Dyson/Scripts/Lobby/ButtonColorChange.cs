using System.Collections;
using System.Collections.Generic;
using _Studens.Leonardo.Scripts.ClientRelated;
using Dyson.GPG222.Lobby;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;
using UnityEngine.UI;

public class ButtonColorChange : MonoBehaviour
{
    public Button readyButton;
    public bool isPlayerReady = false; 
    public ClientState playerClientState;
    
    void Start()
    {
        readyButton.onClick.AddListener(SetPlayerReady);
        
        Lobby lobby = FindObjectOfType<Lobby>();
        if (lobby != null)
        {
            playerClientState = lobby.localPlayerState;
        }
    }

    void SetPlayerReady()
    {
        Color readyColor = Color.green;
        readyButton.GetComponent<Image>().color = readyColor;
        
        isPlayerReady = true;
        if (playerClientState != null)
        {
            playerClientState.isReady = true;
        }

        NetworkClient networkClient = FindObjectOfType<NetworkClient>();
        if (networkClient != null)
        {
            networkClient.SendPlayerReadyState(true);
        }

        if (PlayerPrefs.GetInt("IsHost", 0) == 1)
        {
            Lobby lobby = FindObjectOfType<Lobby>();
            if (lobby != null)
            {
                lobby.CheckAllPlayersReady();
            }
        }
        
    }
}