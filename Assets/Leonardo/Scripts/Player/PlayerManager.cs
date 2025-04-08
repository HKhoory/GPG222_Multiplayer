using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hamad.Scripts;
using Hamad.Scripts.Position;
using Leonardo.Scripts.Controller;
using Leonardo.Scripts.Effects;

namespace Leonardo.Scripts.Player
{
    public class PlayerManager : MonoBehaviour
    {
        private GameObject _playerPrefab;
        private Dictionary<int, GameObject> _playerObjects = new Dictionary<int, GameObject>();
        private PlayerData _localPlayerData;
        
        public PlayerManager(GameObject playerPrefab, PlayerData localPlayerData)
        {
            _playerPrefab = playerPrefab;
            _localPlayerData = localPlayerData;
        }
        
        public GameObject GetLocalPlayerObject()
        {
            return _playerObjects.ContainsKey(_localPlayerData.tag) ? _playerObjects[_localPlayerData.tag] : null;
        }
        
        public void SpawnLocalPlayer()
        {
            Vector3 position = new Vector3(0, 1, 0);
            GameObject newPlayer = Object.Instantiate(_playerPrefab, position, Quaternion.identity);
            _playerObjects[_localPlayerData.tag] = newPlayer;
            
            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetLocalplayer(true);
            }
            
            newPlayer.name = $"PlayerManager.cs: LocalPlayer_{_localPlayerData.name}";
            Debug.LogWarning($"PlayerManager.cs: Local player: {_localPlayerData.name} spawned at {position}");
        }
        
        public void UpdateRemotePlayerPosition(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
            
            if (!_playerObjects.ContainsKey(playerTag))
            {
                CreateRemotePlayer(playerPos);
            }
            else
            {
                UpdateExistingRemotePlayer(playerPos);
            }
        }
        
        private void CreateRemotePlayer(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
            Vector3 position = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
    
            GameObject newPlayer = Object.Instantiate(_playerPrefab, position, Quaternion.identity);
            _playerObjects[playerTag] = newPlayer;
    
            var controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetLocalplayer(false);
            }
    
            var remoteController = newPlayer.AddComponent<RemotePlayerController>();
            if (remoteController != null)
            {
                remoteController.SetPlayerTag(playerTag);
            }
    
            newPlayer.name = $"PlayerManager.cs: RemotePlayer_{playerPos.playerData.name}";
    
            Debug.LogWarning($"PlayerManager.cs: Created remote player: {playerPos.playerData.name} with tag {playerTag}");
        }
        
        private void UpdateExistingRemotePlayer(PlayerPositionData playerPos)
        {
            int playerTag = playerPos.playerData.tag;
            Vector3 newPosition = new Vector3(playerPos.xPos, playerPos.yPos, playerPos.zPos);
            
            var remoteController = _playerObjects[playerTag].GetComponent<RemotePlayerController>();
            if (remoteController != null)
            {
                remoteController.SetPositionTarget(newPosition);
            }
            else
            {
                _playerObjects[playerTag].transform.position = newPosition;
            }
        }
        
        public void ApplyPushToPlayer(int playerTag, Vector3 force, string effectName)
        {
            if (!_playerObjects.ContainsKey(playerTag))
            {
                Debug.LogWarning($"PlayerManager.cs: Cannot push player {playerTag}, not found.");
                return;
            }

            GameObject playerObject = _playerObjects[playerTag];
            RemotePlayerController remoteController = playerObject.GetComponent<RemotePlayerController>();

            if (remoteController != null)
            {
                // Play effect if needed
                if (!string.IsNullOrEmpty(effectName) && EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect(effectName, playerObject.transform.position, playerObject.transform.rotation);
                }
        
                // Apply the push using our simulated physics
                StartCoroutine(SimulatePushTrajectory(remoteController, force));
        
                Debug.Log($"PlayerManager.cs: Applying simulated push to player {playerTag} with force {force}.");
            }
        }
        
        private IEnumerator SimulatePushTrajectory(RemotePlayerController controller, Vector3 force)
        {
            Vector3 startPosition = controller.transform.position;
            Vector3 velocity = force / 10f; // Scale down force to get a reasonable velocity
            Vector3 currentPosition = startPosition;
            float gravity = 9.8f;
            float drag = 0.5f;
    
            // Simulate physics for several frames to create a trajectory
            for (int i = 0; i < 15; i++) // 15 steps of simulation
            {
                // Apply gravity to vertical velocity
                velocity.y -= gravity * Time.fixedDeltaTime;
        
                // Apply drag to slow down movement over time
                velocity *= (1f - drag * Time.fixedDeltaTime);
        
                // Calculate new position
                currentPosition += velocity * Time.fixedDeltaTime;
        
                // Don't go below ground level (simple ground check)
                if (currentPosition.y < 0.5f)
                {
                    currentPosition.y = 0.5f;
                    velocity.y = Mathf.Abs(velocity.y) * 0.6f; // Bounce with reduced energy
                }
        
                // Set the target position
                controller.SetPositionTarget(currentPosition);
        
                yield return new WaitForFixedUpdate();
            }
        }
        
        public void RemovePlayer(int playerTag)
        {
            if (_playerObjects.ContainsKey(playerTag))
            {
                Object.Destroy(_playerObjects[playerTag]);
                _playerObjects.Remove(playerTag);
            }
        }
        
        public void CleanUp()
        {
            foreach (var player in _playerObjects.Values)
            {
                Object.Destroy(player);
            }
            
            _playerObjects.Clear();
        }
    }
}