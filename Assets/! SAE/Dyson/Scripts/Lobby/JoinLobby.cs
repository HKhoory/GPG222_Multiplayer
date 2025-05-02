using __SAE.Leonardo.Scripts.ClientRelated;
using Dyson_GPG222_Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace __SAE.Dyson.Scripts.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        [Header("- Player Settings")]
        [SerializeField] private TMP_InputField playerNameInput;

        [SerializeField] private string defaultNamePrefix = "Player";

        [Header("- UI References")]
        [SerializeField] private GameObject connectionPanel;

        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("- Network Settings")]
        private bool isHost;

        private string serverIp = "127.0.0.1";

        [Header("- Debug Settings")]
        [SerializeField] private bool verboseLogging = false;

        private NetworkClient _networkClient;
        private Server _server;

        private void Awake() {
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

            // If the player name input field is empty, set it to a default value.
            if (playerNameInput != null) {
                string randomNumber = Random.Range(1000, 10000).ToString();
                playerNameInput.text = defaultNamePrefix + randomNumber;
            }

            LogInfo("JoinLobby initialized");
        }

        public void CreateServer() {
            UpdateStatus("Starting server...");

            string playerName = GetPlayerName();


            isHost = true;
            serverIp = "127.0.0.1";

            // Instead of PlayerPrefs, we'll pass these values directly to NetworkClient.
            LogInfo("Player set as host, server IP: 127.0.0.1");

            // Start the server.
            if (_server != null) {
                _server.enabled = true;
                _server.StartServer();
                LogInfo("Server started successfully");
            }
            else {
                LogError("Server component not found!");
                UpdateStatus("Error: Server component not found!");
                return;
            }

            // Connect to the server as a client.
            if (_networkClient != null) {
                _networkClient.SetHostStatus(isHost, serverIp);
                _networkClient.InitiateConnection();
                LogInfo("Initiated client connection");
            }
            else {
                LogError("NetworkClient component not found!");
                UpdateStatus("Error: NetworkClient component not found!");
                return;
            }

            _networkClient.SetPlayerName(playerName);

            // Switch panels.
            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            LogInfo("Switched to lobby panel");
        }

        public void JoinLobbyButton() {
            UpdateStatus("Connecting to server...");

            string playerName = GetPlayerName();


            isHost = false;

            if (ipAddressInput != null) {
                serverIp = ipAddressInput.text;
                if (string.IsNullOrEmpty(serverIp))
                    serverIp = "127.0.0.1";
            }

            LogInfo($"Player set as client, server IP: {serverIp}");

            if (_networkClient != null) {
                _networkClient.SetHostStatus(isHost, serverIp);
                _networkClient.InitiateConnection();
                LogInfo("Initiated client connection");
            }
            else {
                LogError("NetworkClient component not found!");
                UpdateStatus("Error: NetworkClient component not found!");
                return;
            }

            _networkClient.SetPlayerName(playerName);

            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            LogInfo("Switched to lobby panel");
        }

        private void UpdateStatus(string message) {
            if (statusText != null) {
                statusText.text = message;
            }

            LogInfo($"Status: {message}");
        }

        private string GetPlayerName() {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text)) {
                return playerNameInput.text;
            }

            return defaultNamePrefix + Random.Range(1000, 10000);
        }

        #region Logging Methods

        private void LogInfo(string message) {
            if (verboseLogging) {
                Debug.Log($"[JoinLobby] {message}");
            }
        }

        private void LogError(string message) {
            Debug.LogError($"[JoinLobby] ERROR: {message}");
        }

        #endregion

        private void OnGUI() {
            GUI.Label(new Rect(10, 10, 200, 30), isHost ? "MODE: HOST" : "MODE: CLIENT");
            GUI.Label(new Rect(10, 40, 200, 30), $"Server IP: {serverIp}");
        }
    }
}