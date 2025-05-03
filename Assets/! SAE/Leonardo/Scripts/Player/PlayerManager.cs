using System.Collections.Generic;
using Hamad.Scripts;
using Hamad.Scripts.Position;
using Leonardo.Scripts.Controller;
using Leonardo.Scripts.Effects;
using UnityEngine;

namespace __SAE.Leonardo.Scripts.Player
{
    public class PlayerManager : MonoBehaviour
    {
        private GameObject _playerPrefab;
        private Dictionary<int, GameObject> _playerObjects = new Dictionary<int, GameObject>();
        private PlayerData _localPlayerData;
        private bool _isGameplayActive = false;

        public void Initialize(GameObject playerPrefab, PlayerData localPlayerData) {
            _playerPrefab = playerPrefab;
            _localPlayerData = localPlayerData;
            Debug.Log($"PlayerManager initialized with player: {localPlayerData?.name}");
        }

        private void Awake() {
            _playerObjects = new Dictionary<int, GameObject>();
        }

        public GameObject GetLocalPlayerObject() {
            if (_localPlayerData == null) {
                Debug.LogError("PlayerManager: _localPlayerData is null! Make sure Initialize is called pendejo!");
                return null;
            }
    
            return _playerObjects.ContainsKey(_localPlayerData.tag) ? _playerObjects[_localPlayerData.tag] : null;
        }

        public void SpawnLocalPlayer() {
            // Check if we already have this player spawned
            if (_playerObjects.ContainsKey(_localPlayerData.tag)) {
                Debug.LogWarning($"PlayerManager.cs: Local player {_localPlayerData.name} already spawned");
                return;
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            // Try to get a spawn point from the SpawnPointsManager
            SpawnPointsManager spawnPointsManager = SpawnPointsManager.Instance;
            if (spawnPointsManager != null) {
                Transform spawnPoint = spawnPointsManager.GetSpawnPointById(_localPlayerData.tag);
                if (spawnPoint != null) {
                    position = spawnPoint.position;
                    rotation = spawnPoint.rotation;
                }
                else {
                    position = new Vector3(0, 1, 0);
                }
            }
            else {
                position = new Vector3(0, 1, 0);
            }

            GameObject newPlayer = Object.Instantiate(_playerPrefab, position, rotation);
            _playerObjects[_localPlayerData.tag] = newPlayer;

            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null) {
                controller.SetLocalplayer(true);
            }

            newPlayer.name = $"PlayerManager.cs: LocalPlayer_{_localPlayerData.name}";
            Debug.Log($"PlayerManager.cs: Local player: {_localPlayerData.name} spawned at {position}");

            // Activate gameplay state
            SetGameplayActive(true);
        }

        //Hamad: adding function if the game resets, (really stupid way but I wanna see if it works)
        public void RespawnLocalPlayer(PlayerPositionData playerPos) {
            Vector3 position = new Vector3(Random.Range(-2f, 2f), 1, Random.Range(-2f, 2f));
            GameObject localPlayer = GetLocalPlayerObject();
            localPlayer.transform.position = new Vector3(0, 1, 0);
        }

        public void UpdateRemotePlayerPosition(PlayerPositionData playerPos) {
            int playerTag = playerPos.playerData.tag;

            if (!_playerObjects.ContainsKey(playerTag)) {
                CreateRemotePlayer(playerPos);
            }
            else {
                UpdateExistingRemotePlayer(playerPos);
            }
        }

        private void CreateRemotePlayer(PlayerPositionData playerPos) {
            int playerTag = playerPos.playerData.tag;
            Vector3 position = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);

            // If we're not in gameplay mode, try to use a spawn point instead
            if (!_isGameplayActive) {
                SpawnPointsManager spawnPointsManager = SpawnPointsManager.Instance;
                if (spawnPointsManager != null) {
                    Transform spawnPoint = spawnPointsManager.GetSpawnPointById(playerTag);
                    if (spawnPoint != null) {
                        position = spawnPoint.position;
                    }
                }
            }

            GameObject newPlayer = Object.Instantiate(_playerPrefab, position, Quaternion.identity);
            _playerObjects[playerTag] = newPlayer;

            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null) {
                controller.SetLocalplayer(false);
            }

            var remoteController = newPlayer.AddComponent<RemotePlayerController>();
            if (remoteController != null) {
                remoteController.SetPlayerTag(playerTag);
            }

            newPlayer.name = $"PlayerManager.cs: RemotePlayer_{playerPos.playerData.name}";

            Debug.LogWarning(
                $"PlayerManager.cs: Created remote player: {playerPos.playerData.name} with tag {playerTag}");
        }

        private void UpdateExistingRemotePlayer(PlayerPositionData playerPos) {
            int playerTag = playerPos.playerData.tag;
            Vector3 newPosition = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);

            var remoteController = _playerObjects[playerTag].GetComponent<RemotePlayerController>();
            if (remoteController != null) {
                remoteController.SetPositionTarget(newPosition);
            }
            else {
                _playerObjects[playerTag].transform.position = newPosition;
            }
        }

        public void HandlePushEvent(int playerTag, Vector3 force, string effectName) {
            //Debug.Log($"PlayerManager.cs: Received push event for player {playerTag}, local player is {_localPlayerData.tag}");

            if (playerTag == _localPlayerData.tag) {
                //Debug.Log($"PlayerManager.cs: Push is for local player, applying force {force}");
                GameObject localPlayer = GetLocalPlayerObject();
                if (localPlayer != null) {
                    PlayerController controller = localPlayer.GetComponent<PlayerController>();
                    if (controller != null) {
                        controller.ApplyPushForce(force, effectName);
                    }
                    else {
                        //Debug.LogError("PlayerManager.cs: Local player has no PlayerController component");
                    }
                }
                else {
                    //Debug.LogError("PlayerManager.cs: Could not find local player object");
                }
            }

            if (_playerObjects.ContainsKey(playerTag)) {
                GameObject playerObject = _playerObjects[playerTag];
                if (!string.IsNullOrEmpty(effectName) && EffectManager.Instance != null) {
                    EffectManager.Instance.PlayEffect(effectName, playerObject.transform.position,
                        playerObject.transform.rotation);
                }
            }
        }
        
        //Dyson: Add function to handle freeze event

        public void HandleFreezeEvent(int playerTag, float freezeDuration, string effectName) {
            if (playerTag == _localPlayerData.tag) {
                GameObject localPlayer = GetLocalPlayerObject();
                if (localPlayer != null) {
                    PlayerController controller = localPlayer.GetComponent<PlayerController>();
                    if (controller != null) {
                        controller.ApplyFreeze(freezeDuration, effectName);
                    }
                    else {
                        Debug.LogWarning("PlayerManager.cs: Local player has no PlayerController component.");
                    }
                }
                else {
                    Debug.LogWarning("PlayerManager.cs: Could not find local player object.");
                }
            }
        }

        public void RemovePlayer(int playerTag) {
            if (_playerObjects.ContainsKey(playerTag)) {
                Object.Destroy(_playerObjects[playerTag]);
                _playerObjects.Remove(playerTag);
            }
        }

        public void CleanUp() {
            foreach (var player in _playerObjects.Values) {
                Object.Destroy(player);
            }

            _playerObjects.Clear();
            _isGameplayActive = false;
        }

        /// <summary>
        /// Sets whether gameplay is active. This affects how players are spawned and positioned.
        /// </summary>
        /// <param name="active">Whether gameplay is active.</param>
        public void SetGameplayActive(bool active) {
            _isGameplayActive = active;
            Debug.Log($"PlayerManager.cs: Gameplay active state set to {active}");
        }

        /// <summary>
        /// Checks if gameplay is currently active.
        /// </summary>
        /// <returns>True if gameplay is active, otherwise false.</returns>
        public bool IsGameplayActive() {
            return _isGameplayActive;
        }
    }
}