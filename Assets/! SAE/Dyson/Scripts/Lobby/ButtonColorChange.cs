using System.Collections;
using __SAE.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dyson.Scripts.Lobby
{
    /// <summary>
    /// Manages the ready button state in the multiplayer lobby.
    /// Handles visual feedback and network communication for player ready state.
    /// </summary>
    public class ButtonColorChange : MonoBehaviour
    {
        #region Serialized Fields
        [Header("- Button Settings")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color notReadyColor = Color.red;
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField] private bool useColorTransition = true;
    
        [Header("- References")]
        [SerializeField] private ClientState playerClientState;
    
        [Header("- UI Elements")]
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image buttonImage;
        
        [Header("- Audio")]
        [SerializeField] private AudioClip readySound;
        [SerializeField] private AudioSource audioSource;
        
        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;
        #endregion
    
        #region Private Fields
        [HideInInspector]
        public bool isPlayerReady = false;
    
        private NetworkClient _networkClient;
        private Lobby _lobby;
        private bool _buttonClicked = false;
        private bool _hasSetupCompleted = false;
        private Color _targetColor;
        private Color _currentColor;
        private Coroutine _colorTransitionCoroutine;
        #endregion
    
        #region Unity Lifecycle Methods
        void Start()
        {
            InitializeComponents();
            StartCoroutine(CheckPlayerReady());
        }
        
        void Update()
        {
            if (!_hasSetupCompleted) return;
            
            if (useColorTransition && buttonImage != null)
            {
                buttonImage.color = Color.Lerp(buttonImage.color, _targetColor, Time.deltaTime * transitionSpeed);
            }
        }
        
        void OnDestroy()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(SetPlayerReady);
            }
            
            if (_colorTransitionCoroutine != null)
            {
                StopCoroutine(_colorTransitionCoroutine);
            }
        }
        #endregion
    
        #region Initialization Methods
        /// <summary>
        /// Initializes all components and references.
        /// </summary>
        private void InitializeComponents()
        {
            if (readyButton == null)
            {
                readyButton = GetComponent<Button>();
                if (readyButton == null)
                {
                    LogError("Ready button not found!");
                    return;
                }
            }
            
            if (buttonText == null && readyButton != null)
            {
                buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText == null)
                {
                    LogWarning("Button text not found!");
                }
            }
            
            if (buttonImage == null && readyButton != null)
            {
                buttonImage = readyButton.GetComponent<Image>();
                if (buttonImage == null)
                {
                    LogWarning("Button image not found!");
                }
                else
                {
                    buttonImage.color = notReadyColor;
                    _currentColor = notReadyColor;
                    _targetColor = notReadyColor;
                }
            }
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && readySound != null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            _lobby = FindObjectOfType<Lobby>();
            if (_lobby != null)
            {
                playerClientState = _lobby.LocalPlayerClientState;
                LogInfo("Found lobby and local player client state");
            }
            else
            {
                LogWarning("Lobby not found!");
            }
            
            _networkClient = FindObjectOfType<NetworkClient>();
            if (_networkClient == null)
            {
                LogError("NetworkClient not found!");
            }
            
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(SetPlayerReady);
                LogInfo("Added click listener to ready button");
            }
            
            _hasSetupCompleted = true;
        }
        
        /// <summary>
        /// Checks if the player is already ready when joining the lobby.
        /// </summary>
        private IEnumerator CheckPlayerReady()
        {
            yield return new WaitForSeconds(1.0f);
            
            if (playerClientState != null && playerClientState.isReady)
            {
                UpdateReadyUI(true);
                LogInfo("Player already ready, updating UI");
            }
        }
        #endregion
    
        #region Ready State Methods
        /// <summary>
        /// Sets the player as ready and updates the UI.
        /// </summary>
        public void SetPlayerReady()
        {
            if (_buttonClicked)
            {
                LogInfo("Button already clicked, ignoring");
                return;
            }
            
            if (_networkClient == null)
            {
                LogError("Cannot set ready state: NetworkClient not found!");
                return;
            }
            
            _buttonClicked = true;
            isPlayerReady = true;
            
            if (playerClientState != null)
            {
                playerClientState.isReady = true;
                LogInfo("Updated local player ready state to true");
            }
            else
            {
                LogWarning("PlayerClientState is null!");
            }
            
            UpdateReadyUI(true);
            
            try
            {
                _networkClient.SendPlayerReadyState(true);
                LogInfo("Sent ready state (true) to server");
            }
            catch (System.Exception e)
            {
                LogError($"Failed to send ready state: {e.Message}");
            }
            
            if (audioSource != null && readySound != null)
            {
                audioSource.PlayOneShot(readySound);
            }
            
            if (PlayerPrefs.GetInt("IsHost", 0) == 1 && _lobby != null)
            {
                _lobby.CheckAllPlayersReady();
            }
            
            if (statusText != null)
            {
                statusText.text = "Waiting for other players...";
            }
        }
    
        /// <summary>
        /// Updates the UI to reflect the player's ready state.
        /// </summary>
        /// <param name="ready">Whether the player is ready.</param>
        private void UpdateReadyUI(bool ready)
        {
            if (buttonImage != null)
            {
                if (useColorTransition)
                {
                    _targetColor = ready ? readyColor : notReadyColor;
                }
                else
                {
                    buttonImage.color = ready ? readyColor : notReadyColor;
                }
            }
            
            if (buttonText != null)
            {
                buttonText.text = ready ? "Ready" : "Ready?";
            }
            
            if (ready && readyButton != null)
            {
                readyButton.interactable = false;
                
                if (buttonImage != null && !useColorTransition)
                {
                    buttonImage.color = readyColor;
                }
            }
            
            LogInfo($"Updated ready UI: ready={ready}");
        }
        
        /// <summary>
        /// Resets the button to not ready state (for debugging or game restart).
        /// </summary>
        public void ResetButton()
        {
            _buttonClicked = false;
            isPlayerReady = false;
            
            if (playerClientState != null)
            {
                playerClientState.isReady = false;
            }
            
            UpdateReadyUI(false);
            
            if (readyButton != null)
            {
                readyButton.interactable = true;
            }
            
            LogInfo("Reset button to not ready state");
        }
        #endregion
        
        #region Logging Methods
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogInfo(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[ButtonColorChange] {message}");
            }
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ButtonColorChange] WARNING: {message}");
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogError(string message)
        {
            Debug.LogError($"[ButtonColorChange] ERROR: {message}");
        }
        #endregion
    }
}