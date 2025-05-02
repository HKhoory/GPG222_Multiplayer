using System.Collections;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dyson.Scripts.Lobby
{
    public class ButtonColorChange : MonoBehaviour
    {
        [Header("Button Settings")]
        public Button readyButton;
        public Color readyColor = Color.green;
        public Color notReadyColor = Color.red;
    
        [Header("References")]
        public ClientState playerClientState;
    
        [Header("UI Elements")]
        public TextMeshProUGUI buttonText;
    
        [HideInInspector]
        public bool isPlayerReady = false;
    
        private NetworkClient _networkClient;
        private GPG222.Lobby.Lobby _lobby;
        private bool _buttonClicked = false;
    
        void Start()
        {
            if (readyButton == null)
            {
                readyButton = GetComponent<Button>();
            }
        
            if (buttonText == null && readyButton != null)
            {
                buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        
            if (readyButton != null)
            {
                Image buttonImage = readyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = notReadyColor;
                }
            }
        
            _lobby = FindObjectOfType<GPG222.Lobby.Lobby>();
            _networkClient = FindObjectOfType<NetworkClient>();
        
            if (_lobby != null)
            {
                playerClientState = _lobby.LocalPlayerClientState;
            }
        
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(SetPlayerReady);
            }
        
            StartCoroutine(CheckPlayerReady());
        }
    
        private IEnumerator CheckPlayerReady()
        {
            yield return new WaitForSeconds(1.0f);
        
            if (playerClientState != null && playerClientState.isReady)
            {
                UpdateReadyUI(true);
            }
        }
    
        void SetPlayerReady()
        {
            if (_buttonClicked) return;
            _buttonClicked = true;
        
            isPlayerReady = true;
        
            if (playerClientState != null)
            {
                playerClientState.isReady = true;
            }
        
            UpdateReadyUI(true);
        
            if (_networkClient != null)
            {
                _networkClient.SendPlayerReadyState(true);
                Debug.Log($"ButtonColorChange: Sent ready state (true) to server");
            }
            else
            {
                Debug.LogError("ButtonColorChange: NetworkClient not found!");
            }
        
            if (PlayerPrefs.GetInt("IsHost", 0) == 1 && _lobby != null)
            {
                _lobby.CheckAllPlayersReady();
            }
        }
    
        private void UpdateReadyUI(bool ready)
        {
            Image buttonImage = readyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = ready ? readyColor : notReadyColor;
            }
        
            if (buttonText != null)
            {
                buttonText.text = ready ? "Ready" : "Ready?";
            }
        
            if (ready)
            {
                readyButton.interactable = false;
            }
        }
    
        void OnDestroy()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(SetPlayerReady);
            }
        }
    }
}