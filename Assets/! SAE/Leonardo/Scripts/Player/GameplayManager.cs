using __SAE.Leonardo.Scripts.Abilities;
using __SAE.Leonardo.Scripts.ClientRelated;
using UnityEngine;

namespace __SAE.Leonardo.Scripts.Player
{
    /// <summary>
    /// Manages the transition between lobby and gameplay states.
    /// </summary>
    public class GameplayManager : MonoBehaviour
    {
        [Header("- UI References")]
        [SerializeField] private GameObject lobbyUI;
        [SerializeField] private GameObject gameplayUI;
        
        [Header("- Gameplay Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool createSpawnPointsIfMissing = true;
        [SerializeField] private int spawnPointCount = 4;
        [SerializeField] private float spawnCircleRadius = 5f;
        
        [Header("- References")]
        [SerializeField] private NetworkClient networkClient;
        
        private bool _isGameplayActive = false;
        private SpawnPointsManager _spawnPointsManager;
        
        private static GameplayManager _instance;
        public static GameplayManager Instance => _instance;
        
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
            
            _spawnPointsManager = GetComponent<SpawnPointsManager>();
            if (_spawnPointsManager == null)
            {
                _spawnPointsManager = gameObject.AddComponent<SpawnPointsManager>();
            }
            
            // Find network client if not assigned.
            if (networkClient == null)
            {
                networkClient = FindObjectOfType<NetworkClient>();
            }
            
            // Set initial UI state.
            SetGameplayActive(false);
        }
        
        private void Start()
        {
            // If we have spawn points in the inspector, add them to the manager.
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Register the spawn points with the SpawnPointsManager
                RegisterSpawnPoints();
            }
        }

        private void RegisterSpawnPoints()
        {
            if (_spawnPointsManager != null && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Todo: do this normally later.
                var spawnPointsField = typeof(SpawnPointsManager).GetField("spawnPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (spawnPointsField != null)
                {
                    var existingSpawnPoints = spawnPointsField.GetValue(_spawnPointsManager) as System.Collections.Generic.List<Transform>;
                    if (existingSpawnPoints != null)
                    {
                        // Clear existing spawn points if any.
                        existingSpawnPoints.Clear();
                        
                        // Add our spawn points.
                        foreach (var point in spawnPoints)
                        {
                            if (point != null)
                            {
                                existingSpawnPoints.Add(point);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("GameplayManager: Could not access spawnPoints field in SpawnPointsManager");
                }
            }
        }
        
        /// <summary>
        /// Activates gameplay mode.
        /// </summary>
        public void StartGameplay()
        {
            if (_isGameplayActive)
            {
                Debug.LogWarning("GameplayManager: Gameplay is already active");
                return;
            }
            
            // Set the gameplay state.
            SetGameplayActive(true);
            
            // Activate gameplay in the NetworkClient.
            if (networkClient != null)
            {
                networkClient.ActivateGameplay();
                
                // Enable abilities on all players.
                EnablePlayerAbilities();
            }
            else
            {
                Debug.LogError("GameplayManager: Cannot start gameplay - NetworkClient not found");
            }
            
            Debug.Log("GameplayManager: Gameplay started");
        }
        
        /// <summary>
        /// Enables abilities on all player objects.
        /// </summary>
        private void EnablePlayerAbilities()
        {
            if (networkClient == null || networkClient.GetPlayerManager() == null)
                return;
            
            // Enable abilities on local player.
            GameObject localPlayer = networkClient.GetPlayerManager().GetLocalPlayerObject();
            if (localPlayer != null)
            {
                AbilityManager abilityManager = localPlayer.GetComponent<AbilityManager>();
                if (abilityManager != null)
                {
                    abilityManager.SetAbilitiesEnabled(true);
                    Debug.Log("GameplayManager: Enabled abilities on local player");
                }
            }
        }
        
        /// <summary>
        /// Sets whether gameplay is active and updates the UI accordingly.
        /// </summary>
        /// <param name="active">Whether gameplay should be active.</param>
        public void SetGameplayActive(bool active)
        {
            _isGameplayActive = active;
            
            // Update UI.
            if (lobbyUI != null) lobbyUI.SetActive(!active);
            if (gameplayUI != null) gameplayUI.SetActive(active);
            
            // Update player manager state.
            if (networkClient != null && networkClient.GetPlayerManager() != null)
            {
                networkClient.GetPlayerManager().SetGameplayActive(active);
            }
            
            Debug.Log($"GameplayManager: Gameplay active state set to {active}");
        }
        
        /// <summary>
        /// Checks if gameplay is currently active.
        /// </summary>
        /// <returns>True if gameplay is active, otherwise false.</returns>
        public bool IsGameplayActive()
        {
            return _isGameplayActive;
        }
        
        /// <summary>
        /// Resets the game state back to lobby.
        /// </summary>
        public void ResetToLobby()
        {
            // Cleanup existing players.
            if (networkClient != null && networkClient.GetPlayerManager() != null)
            {
                networkClient.GetPlayerManager().CleanUp();
            }
            
            // Set state back to lobby.
            SetGameplayActive(false);
            
            Debug.Log("GameplayManager: Reset to lobby state");
        }
    }
}