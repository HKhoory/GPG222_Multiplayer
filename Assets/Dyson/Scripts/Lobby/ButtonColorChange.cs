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
    
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(ChangeColor);
        _client = FindObjectOfType<Lobby>().testPlayer;
    }

    void ChangeColor()
    {
        Color newColor = isButtonPressed ? Color.red : Color.green;
        button.GetComponent<Image>().color = newColor;
        isButtonPressed = true;
        _client.isReady = true;
    }
}
