using Leonardo.Scripts.ClientRelated;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hamad.Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Header("' Player Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
    
        [Header("' UI References")]
        [SerializeField] private GameObject gameUI;
    
        [Header("' Network References")]
        [SerializeField] private NetworkClient networkClient;
    
        private static GameManager _instance;
        public static GameManager Instance => _instance;
    
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        
            // Ensure references are set.
            if (networkClient == null)
            {
                networkClient = FindObjectOfType<NetworkClient>();
            }
        }
    
        public void StartGame()
        {
            // Load the game scene additively to keep the network components.
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        
            // Enable game UI.
            if (gameUI != null)
            {
                gameUI.SetActive(true);
            }
        
            // Spawn players.
            SpawnPlayers();
        }
    
        private void SpawnPlayers()
        {
            if (networkClient == null || playerPrefab == null)
            {
                Debug.LogError("[GameManager] Cannot spawn players: Missing references");
                return;
            }
        
            // Local player is managed by the NetworkClient's PlayerManager.
            // This is handled automatically when the scene loads.
        
            Debug.Log("[GameManager] Players spawned");
        }
    }
}