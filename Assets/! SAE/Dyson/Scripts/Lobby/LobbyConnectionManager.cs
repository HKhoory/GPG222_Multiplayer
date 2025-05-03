using __SAE.Leonardo.Scripts.ClientRelated;
using TMPro; 
using UnityEngine;
using UnityEngine.UI; 

namespace __SAE.Dyson.Scripts.Lobby 
{
    /// <summary>
    /// Manages the initial connection UI, allowing the user to host or join a lobby.
    /// Handles setting player name, server IP, and initiating the connection process.
    /// </summary>
    public class LobbyConnectionManager : MonoBehaviour
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

        private NetworkClient _networkClient;
        private Server _server;

        private bool _isWaitingToConnectAfterHosting = false;
        private float _connectDelayTimer = 0f;
        private string _pendingPlayerNameForConnect = "";
        private const float ServerStartConnectDelay = 0.5f;

        /// <summary>
        /// Called when the script instance is being loaded. Finds components and sets up listeners.
        /// </summary>
        private void Awake() {
            _server = FindObjectOfType<Server>();
            _networkClient = FindObjectOfType<NetworkClient>();

            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";

            if (hostButton != null)
                hostButton.onClick.AddListener(CreateServer);

            if (joinButton != null)
                joinButton.onClick.AddListener(JoinLobbyButton);

            if (connectionPanel != null)
                connectionPanel.SetActive(true);

            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);

            if (playerNameInput != null && string.IsNullOrEmpty(playerNameInput.text)) {
                string randomNumber = Random.Range(1000, 10000).ToString();
                playerNameInput.text = defaultNamePrefix + randomNumber;
            }
        }

        /// <summary>
        /// Called every frame. Handles the timed delay for connecting the client after hosting.
        /// </summary>
        private void Update()
        {
            if (_isWaitingToConnectAfterHosting)
            {
                _connectDelayTimer -= Time.deltaTime;
                if (_connectDelayTimer <= 0f) {
                    _isWaitingToConnectAfterHosting = false;
                    ConnectClientAfterHosting();
                }
            }
        }

        /// <summary>
        /// Initiates the server creation process when the host button is clicked.
        /// Starts the server and sets up a delayed client connection.
        /// </summary>
        public void CreateServer() {
            UpdateStatus("Starting server...");

            string playerName = GetPlayerName();

            isHost = true;
            serverIp = "127.0.0.1";

            if (_server != null) {
                _server.enabled = true;
                _server.StartServer();

                _pendingPlayerNameForConnect = playerName;
                _connectDelayTimer = ServerStartConnectDelay;
                _isWaitingToConnectAfterHosting = true; 
            }
            else {
                UpdateStatus("Error: Server component not found!");
            }
        }

        /// <summary>
        /// Connects the local client after the server has been started and the initial delay has passed.
        /// </summary>
        private void ConnectClientAfterHosting() {
            if (_networkClient != null) {
                _networkClient.SetPlayerName(_pendingPlayerNameForConnect); 
                _networkClient.SetHostStatus(isHost, serverIp);
                _networkClient.InitiateConnection();

                if (connectionPanel != null) connectionPanel.SetActive(false);
                if (lobbyPanel != null) lobbyPanel.SetActive(true);
            }
            else {
                UpdateStatus("Error: NetworkClient component not found!");
            }
        }

        /// <summary>
        /// Initiates the client joining process when the join button is clicked.
        /// Sets the client status and connects to the specified server IP.
        /// </summary>
        public void JoinLobbyButton() {
            UpdateStatus("Connecting to server...");

            string playerName = GetPlayerName();

            isHost = false;

            if (ipAddressInput != null) {
                serverIp = ipAddressInput.text;
                if (string.IsNullOrEmpty(serverIp))
                    serverIp = "127.0.0.1";
            }

            if (_networkClient != null) {
                _networkClient.SetPlayerName(playerName);
                _networkClient.SetHostStatus(isHost, serverIp);
                _networkClient.InitiateConnection();
            }
            else {
                UpdateStatus("Error: NetworkClient component not found!");
                return;
            }

            if (connectionPanel != null) connectionPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
        }

        /// <summary>
        /// Updates the status text displayed in the UI.
        /// </summary>
        /// <param name="message">The message to display.</param>
        private void UpdateStatus(string message) {
            if (statusText != null) {
                statusText.text = message;
            }
        }

        /// <summary>
        /// Retrieves the player name from the input field or generates a default name.
        /// </summary>
        /// <returns>The player's name.</returns>
        private string GetPlayerName() {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text)) {
                return playerNameInput.text;
            }
            string randomNumber = Random.Range(1000, 10000).ToString();
            return defaultNamePrefix + randomNumber;
        }

        /// <summary>
        /// Displays simple debug information on the screen using the legacy GUI system.
        /// </summary>
        private void OnGUI() {
            GUI.Label(new Rect(10, 10, 200, 30), isHost ? "MODE: HOST" : "MODE: CLIENT");
            GUI.Label(new Rect(10, 40, 200, 30), $"Server IP: {serverIp}");
        }
    }
}
