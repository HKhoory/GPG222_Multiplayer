using Leonardo.Scripts.ClientRelated;
using Dyson_GPG222_Server;
using Leonardo.Scripts.ClientRelated;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dyson.GPG222.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        [Header("- UI References")]
        public GameObject joinLobbyPanel;
        public GameObject lobbyPanel;
        public TMP_InputField ipAddressInput;
        public Button hostButton;
        public Button joinButton;
        
        [Header("- References")]
        private NetworkClient _networkClient;
        private Lobby _lobby;
        private Server _server;
        
        private void Awake()
        {
            _server = FindObjectOfType<Server>();
            _networkClient = FindObjectOfType<NetworkClient>();
            _lobby = FindObjectOfType<Lobby>();
            
            if (hostButton != null)
                hostButton.onClick.AddListener(CreateServer);
                
            if (joinButton != null)
                joinButton.onClick.AddListener(JoinLobbyButton);
                
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
        }

        public void CreateServer()
        {
            PlayerPrefs.SetInt("IsHost", 1);
            PlayerPrefs.SetString("ServerIP", "127.0.0.1");
            PlayerPrefs.Save();
    
            if (_server != null)
            {
                _server.enabled = true;
                _server.StartServer();
                Debug.Log("Server started successfully!");
            }
            else
            {
                Debug.LogError("Server component not found!");
            }
    
            if (_networkClient != null)
            {
                _networkClient.InitiateConnection();
            }
    
            joinLobbyPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }

        public void JoinLobbyButton()
        {
            PlayerPrefs.SetInt("IsHost", 0);
    
            if (ipAddressInput != null)
            {
                string ip = ipAddressInput.text;
                if (string.IsNullOrEmpty(ip))
                    ip = "127.0.0.1";
            
                PlayerPrefs.SetString("ServerIP", ip);
                PlayerPrefs.Save();
            }
    
            if (_networkClient != null)
            {
                _networkClient.InitiateConnection();
            }
    
            joinLobbyPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }
    }
}