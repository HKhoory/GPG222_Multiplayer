/*using UnityEngine;
using UnityEngine.UI;

namespace Leonardo.Scripts.ClientRelated.Debug
{
    public class DebugUI : MonoBehaviour
    {
        public Text debugText;
        private Client _client;

        void Start()
        {
            _client = FindObjectOfType<Client>();
            if (!debugText) debugText = GetComponent<Text>();
        }

        void Update()
        {
            if (_client && debugText)
            {
                string info = $"Local Player: {(_client.localPlayer != null ? _client.localPlayer.name + " (Tag: " + _client.localPlayer.tag + ")" : "None")}\n";
                info += $"Players in scene: {_client.playerObjects.Count}\n";
            
                foreach (var player in _client.playerObjects)
                {
                    info += $"- Player {player.Key}: {player.Value.name} at {player.Value.transform.position}\n";
                }
            
                debugText.text = info;
            }
        }
    }
}*/