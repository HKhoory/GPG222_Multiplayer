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
        
        public void HandlePushEvent(int playerTag, Vector3 force, string effectName)
        {
            Debug.Log($"PlayerManager.cs: Received push event for player {playerTag}, local player is {_localPlayerData.tag}");
    
            if (playerTag == _localPlayerData.tag)
            {
                Debug.Log($"PlayerManager.cs: Push is for local player, applying force {force}");
                GameObject localPlayer = GetLocalPlayerObject();
                if (localPlayer != null)
                {
                    PlayerController controller = localPlayer.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.ApplyPushForce(force, effectName);
                    }
                    else
                    {
                        Debug.LogError("PlayerManager.cs: Local player has no PlayerController component");
                    }
                }
                else
                {
                    Debug.LogError("PlayerManager.cs: Could not find local player object");
                }
            }
    
            if (_playerObjects.ContainsKey(playerTag))
            {
                GameObject playerObject = _playerObjects[playerTag];
                if (!string.IsNullOrEmpty(effectName) && EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect(effectName, playerObject.transform.position, playerObject.transform.rotation);
                }
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