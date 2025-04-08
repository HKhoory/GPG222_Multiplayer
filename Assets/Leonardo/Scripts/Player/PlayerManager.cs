using System.Collections.Generic;
using UnityEngine;
using Hamad.Scripts;
using Hamad.Scripts.Position;
using Leonardo.Scripts.Controller;

namespace Leonardo.Scripts.Player
{
    public class PlayerManager
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
            
            newPlayer.AddComponent<RemotePlayerController>();
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
        
        public void ApplyPushToPlayer(int playerTag, Vector3 force)
        {
            if (!_playerObjects.ContainsKey(playerTag))
            {
                Debug.LogWarning($"PlayerManager.cs: Cannot push player {playerTag}, not found.");
                return;
            }
    
            GameObject playerObject = _playerObjects[playerTag];
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
    
            if (rb != null)
            {
                Debug.Log($"PlayerManager.cs: Applying push force {force} to player {playerTag}.");
                rb.AddForce(force, ForceMode.Impulse);
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