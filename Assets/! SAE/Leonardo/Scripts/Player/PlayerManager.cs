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
        private Dictionary<int, GameObject> _playerObjects = new();
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
                Debug.LogError("PlayerManager: _localPlayerData is null! Make sure Initialize is called first.");
                return null;
            }

            return _playerObjects.ContainsKey(_localPlayerData.tag) ? _playerObjects[_localPlayerData.tag] : null;
        }

        public void SpawnLocalPlayer() {
            Debug.Log($"PlayerManager.SpawnLocalPlayer: Starting for player {_localPlayerData?.name}");

            // Check if _localPlayerData is null.
            if (_localPlayerData == null) {
                Debug.LogError("PlayerManager.SpawnLocalPlayer: _localPlayerData is null!");
                return;
            }

            // Check if we already have this player spawned (but force respawn for gameplay transition).
            if (_playerObjects.ContainsKey(_localPlayerData.tag) && _isGameplayActive) {
                Debug.LogWarning(
                    $"PlayerManager.SpawnLocalPlayer: Local player {_localPlayerData.name} already spawned");
                return;
            }

            // If we previously had this player, remove it to respawn fresh.
            if (_playerObjects.ContainsKey(_localPlayerData.tag)) {
                GameObject oldPlayer = _playerObjects[_localPlayerData.tag];
                Destroy(oldPlayer);
                _playerObjects.Remove(_localPlayerData.tag);
                Debug.Log($"PlayerManager.SpawnLocalPlayer: Removed existing player to respawn");
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            // Try to get a spawn point
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

            if (_playerPrefab == null) {
                Debug.LogError("PlayerManager.SpawnLocalPlayer: _playerPrefab is null!");
                return;
            }

            Debug.Log($"PlayerManager.SpawnLocalPlayer: Instantiating player at {position}");
            GameObject newPlayer = Instantiate(_playerPrefab, position, rotation);
            if (newPlayer == null) {
                Debug.LogError("PlayerManager.SpawnLocalPlayer: Failed to instantiate player!");
                return;
            }

            _playerObjects[_localPlayerData.tag] = newPlayer;

            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null) {
                controller.SetLocalplayer(true);
            }
            else {
                Debug.LogError("No PlayerController found on player prefab!");
            }

            newPlayer.name = $"LocalPlayer_{_localPlayerData.name}";

            // Ensure gameplay state is active
            SetGameplayActive(true);
            Debug.Log(
                $"PlayerManager.SpawnLocalPlayer: Local player {_localPlayerData.name} spawned successfully at {position}");
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

            GameObject newPlayer = Instantiate(_playerPrefab, position, Quaternion.identity);
            _playerObjects[playerTag] = newPlayer;

            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null) {
                controller.SetLocalplayer(false);
            }

            var remoteController = newPlayer.AddComponent<RemotePlayerController>();
            if (remoteController != null) {
                remoteController.SetPlayerTag(playerTag);
            }

            newPlayer.name = $"RemotePlayer_{playerPos.playerData.name}";

            Debug.Log($"PlayerManager.cs: Created remote player: {playerPos.playerData.name} with tag {playerTag}");
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
            if (playerTag == _localPlayerData.tag) {
                GameObject localPlayer = GetLocalPlayerObject();
                if (localPlayer != null) {
                    PlayerController controller = localPlayer.GetComponent<PlayerController>();
                    if (controller != null) {
                        controller.ApplyPushForce(force, effectName);
                    }
                    else {
                        Debug.LogError("PlayerManager.cs: Local player has no PlayerController component");
                    }
                }
                else {
                    Debug.LogError("PlayerManager.cs: Could not find local player object");
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
                Destroy(_playerObjects[playerTag]);
                _playerObjects.Remove(playerTag);
            }
        }

        public void CleanUp() {
            foreach (var player in _playerObjects.Values) {
                Destroy(player);
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