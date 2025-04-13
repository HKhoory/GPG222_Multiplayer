using System.Collections;
using System.Collections.Generic;
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
        playerClientState = FindObjectOfType<Lobby>().localPlayerClientState;
    }

    void SetPlayerReady()
    {
        Color readyColor = Color.green;
        readyButton.GetComponent<Image>().color = readyColor;
        isPlayerReady = true;
        playerClientState.isReady = true;

        // Leo: tell the server player is ready
        FindObjectOfType<NetworkClient>().SendMessagePacket("PLAYER_READY");

        // Leo: check if all ready (host only).
        if (PlayerPrefs.GetInt("IsHost", 0) == 1)
        {
            FindObjectOfType<Lobby>().CheckAllPlayersReady();
        }
    }
}