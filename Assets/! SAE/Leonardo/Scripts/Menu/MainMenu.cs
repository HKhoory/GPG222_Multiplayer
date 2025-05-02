using Dyson_GPG222_Server;
using Dyson.GPG222.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Leonardo.Scripts.Menu
{
    public class MainMenu : MonoBehaviour
    {
        public Button createServerButton;
        public Button joinServerButton;
        public TMP_InputField ipAddressInput;
        public string lobbySceneName = "LobbyScene";
    
        void Start()
        {
            createServerButton.onClick.AddListener(CreateServer);
            joinServerButton.onClick.AddListener(JoinServer);
        
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
        }
    
        void CreateServer()
        {
            PlayerPrefs.SetInt("IsHost", 1);
            PlayerPrefs.SetString("ServerIP", "127.0.0.1");
            SceneManager.LoadScene(lobbySceneName);
        }
    
        void JoinServer()
        {
            PlayerPrefs.SetInt("IsHost", 0);
            PlayerPrefs.SetString("ServerIP", ipAddressInput.text);
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}