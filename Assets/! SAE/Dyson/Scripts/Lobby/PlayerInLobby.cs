using __SAE.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dyson.Scripts.Lobby
{
    public class PlayerInLobby : MonoBehaviour
    {
        [Header("- UI Elements")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image readyStatusImage;
        [SerializeField] private GameObject hostIndicator;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("- Ready Status Colors")]
        [SerializeField] private Color notReadyColor = Color.red;
        [SerializeField] private Color readyColor = Color.green;
        
        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;
        
        private ClientState _clientState;
        private __SAE.Dyson.Scripts.Lobby.Lobby _lobby;
        private bool _isLocalPlayer = false;
        
        public void Setup(ClientState clientState)
        {
            _lobby = FindObjectOfType<__SAE.Dyson.Scripts.Lobby.Lobby>();
            _clientState = clientState;
            
            // Check if this is the local player.
            if (_lobby != null && _lobby.LocalPlayerState != null && 
                _clientState.ClientId == _lobby.LocalPlayerState.ClientId)
            {
                _isLocalPlayer = true;
                LogInfo($"Set up as local player with ID {_clientState.ClientId}");
            }
            
            // Set player name.
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
            
            // Set host indicator.
            if (hostIndicator != null)
            {
                bool isHost = clientState.ClientId == 1;
                hostIndicator.SetActive(isHost);
            }
            
            // Update ready status visual.
            UpdateReadyStatus(clientState.isReady);
            
            LogInfo($"Player entry set up for {clientState.name} (ID: {clientState.ClientId})");
        }
        
        public void UpdateReadyStatus(bool isReady)
        {
            if (readyStatusImage != null)
            {
                readyStatusImage.color = isReady ? readyColor : notReadyColor;
                LogInfo($"Updated ready status visual to {(isReady ? "Ready" : "Not Ready")}");
            }
            
            // Update status text to match ready state.
            if (statusText != null)
            {
                statusText.text = isReady ? "Ready" : "Not Ready";
                statusText.color = isReady ? readyColor : notReadyColor;
            }
            
            // If this is the local player and they are ready, we might need to disable the ready button.
            if (_isLocalPlayer && isReady)
            {
                var readyButton = FindObjectOfType<ButtonColorChange>();
                if (readyButton != null)
                {
                    readyButton.isPlayerReady = true;
                    
                    // Change the button text to "Ready" if it has a text component.
                    Button button = readyButton.GetComponent<Button>();
                    if (button != null)
                    {
                        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                        {
                            buttonText.text = "Ready";
                        }
                    }
                    
                    LogInfo("Updated ready button for local player");
                }
            }
        }
        
        public void RefreshUI()
        {
            if (_clientState != null)
            {
                // Update player name.
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
                
                // Update ready status.
                UpdateReadyStatus(_clientState.isReady);
                
                LogInfo($"Refreshed UI for player {_clientState.name}");
            }
        }
        
        #region Logging Methods
        private void LogInfo(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[PlayerInLobby] {message}");
            }
        }
        #endregion
    }
}