using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dyson_GPG222_Server;
using Leonardo.Scripts.ClientRelated;

namespace Dyson.GPG222.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        [Header("- UI References")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;
        
        private NetworkClient _networkClient;
        private Server _server;
        
        private void Awake()
        {
            // Find required components.
            _server = FindObjectOfType<Server>();
            _networkClient = FindObjectOfType<NetworkClient>();
            
            // Set default IP.
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
            
            // Add button listeners.
            if (hostButton != null)
                hostButton.onClick.AddListener(CreateServer);
                
            if (joinButton != null)
                joinButton.onClick.AddListener(JoinLobbyButton);
            
            // Initialize panels.
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
                
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
                
            LogInfo("JoinLobby initialized");
        }

        public void CreateServer()
        {
            UpdateStatus("Starting server...");
            
            // Save host status and IP.
            PlayerPrefs.SetInt("IsHost", 1);
            PlayerPrefs.SetString("ServerIP", "127.0.0.1");
            PlayerPrefs.Save();
            
            LogInfo("Player set as host, server IP: 127.0.0.1");
    
            // Start the server.
            if (_server != null)
            {
                _server.enabled = true;
                _server.StartServer();
                LogInfo("Server started successfully");
            }
            else
            {
                LogError("Server component not found!");
                UpdateStatus("Error: Server component not found!");
                return;
            }
    
            // Connect to the server as a client.
            if (_networkClient != null)
            {
                _networkClient.InitiateConnection();
                LogInfo("Initiated client connection");
            }
            else
            {
                LogError("NetworkClient component not found!");
                UpdateStatus("Error: NetworkClient component not found!");
                return;
            }
    
            // Switch panels.
            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            LogInfo("Switched to lobby panel");
        }

        public void JoinLobbyButton()
        {
            UpdateStatus("Connecting to server...");
            
            // Save client status.
            PlayerPrefs.SetInt("IsHost", 0);
            
            // Get IP address from input field.
            string ip = "127.0.0.1";
            if (ipAddressInput != null)
            {
                ip = ipAddressInput.text;
                if (string.IsNullOrEmpty(ip))
                    ip = "127.0.0.1";
            }
            
            PlayerPrefs.SetString("ServerIP", ip);
            PlayerPrefs.Save();
            
            LogInfo($"Player set as client, server IP: {ip}");
    
            // Connect to the server.
            if (_networkClient != null)
            {
                _networkClient.InitiateConnection();
                LogInfo("Initiated client connection");
            }
            else
            {
                LogError("NetworkClient component not found!");
                UpdateStatus("Error: NetworkClient component not found!");
                return;
            }
    
            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            LogInfo("Switched to lobby panel");
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            LogInfo($"Status: {message}");
        }
        
        #region Logging Methods
        private void LogInfo(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[JoinLobby] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[JoinLobby] ERROR: {message}");
        }
        #endregion
    }
}