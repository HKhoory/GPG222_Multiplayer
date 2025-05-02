using Dyson.GPG222.Lobby;
using Dyson.Scripts.Lobby;
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
    private bool _isLocalPlayer = false;
    
    public void Setup(ClientState clientState)
    {
        _lobby = FindObjectOfType<Lobby>();
        _clientState = clientState;
        
        // Check if this is the local player
        if (_lobby != null && _lobby.LocalPlayerClientState != null && 
            _clientState.ClientId == _lobby.LocalPlayerClientState.ClientId)
        {
            _isLocalPlayer = true;
        }
        
        if (playerNameText != null)
        {
            string playerName = string.IsNullOrEmpty(clientState.name) ? 
                               $"Player {clientState.ClientId}" : clientState.name;
                               
            if (_isLocalPlayer)
            {
                playerName += " (You)";
            }
            
            playerNameText.text = playerName;
        }
        
        UpdateReadyStatus(clientState.isReady);
    }
    
    public void UpdateReadyStatus(bool isReady)
    {
        if (readyStatusImage != null)
        {
            readyStatusImage.color = isReady ? readyColor : notReadyColor;
        }
        
        // If this is the local player and they are ready, we might need to disable the ready button
        if (_isLocalPlayer && isReady)
        {
            // Find button in parent and disable it
            ButtonColorChange readyButton = GetComponentInParent<ButtonColorChange>();
            if (readyButton != null)
            {
                readyButton.isPlayerReady = true;
                
                // Change the button text to "Ready" if it has a text component
                Button button = readyButton.GetComponent<Button>();
                if (button != null)
                {
                    TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = "Ready";
                    }
                }
            }
        }
    }
    
    public void RefreshUI()
    {
        if (_clientState != null)
        {
            if (playerNameText != null)
            {
                string playerName = string.IsNullOrEmpty(_clientState.name) ? 
                                   $"Player {_clientState.ClientId}" : _clientState.name;
                                   
                if (_isLocalPlayer)
                {
                    playerName += " (You)";
                }
                
                playerNameText.text = playerName;
            }
            
            UpdateReadyStatus(_clientState.isReady);
        }
    }
}