using Dyson.GPG222.Lobby;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInLobby : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public Image readyStatusImage;
    
    [Header("Ready Status Colors")]
    public Color notReadyColor = Color.red;
    public Color readyColor = Color.green;
    
    private ClientState _clientState;
    private Lobby _lobby;
    
    public void Setup(ClientState clientState)
    {
        _lobby = FindObjectOfType<Lobby>();
        _clientState = clientState;
        
        if (playerNameText != null)
        {
            playerNameText.text = "Player " + clientState.ClientId.ToString();
        }
        UpdateReadyStatus(clientState.isReady);
        
        //  Invoke("GoToGame", 2.0f);
    }
    
    public void UpdateReadyStatus(bool isReady)
    {
        if (readyStatusImage != null)
        {
            readyStatusImage.color = isReady ? readyColor : notReadyColor;
        }
    }
    
    public void RefreshUI()
    {
        if (_clientState != null)
        {
            UpdateReadyStatus(_clientState.isReady);
        }
    }
}


