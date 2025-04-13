using System.Collections;
using System.Collections.Generic;
using Dyson.GPG222.Lobby;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;
using UnityEngine.UI;

public class ButtonColorChange : MonoBehaviour
{
    public Button button;

    public bool isButtonPressed = false;

    public ClientState _client;
    
    void Start()
    {
        button.onClick.AddListener(ChangeColor);
        _client = FindObjectOfType<Lobby>().testPlayer;
    }

    void ChangeColor()
    {
        {
            Color newColor = Color.green;
            button.GetComponent<Image>().color = newColor;
            isButtonPressed = true;
            _client.isReady = true;
    
            // Leo: tell the server player is ready.
            FindObjectOfType<NetworkClient>().SendMessagePacket("PLAYER_READY");
    
            // Check if all ready (host only).
            if (PlayerPrefs.GetInt("IsHost", 0) == 1)
            {
                FindObjectOfType<Lobby>().CheckAllPlayersReady();
            }
        }
    }
}
