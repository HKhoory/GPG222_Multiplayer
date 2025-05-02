using System.Collections;
using System.Diagnostics;
using _Studens.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Leonardo.Scripts.Networking
{
    /// <summary>
    /// A simple ping meter that measures round-trip time to the server.
    /// </summary>
    public class PingMeter : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI pingText;
        
        [Header("Settings")]
        [SerializeField] private float pingUpdateInterval = .25f; // How often to measure ping (in seconds)
        
        private NetworkClient _client;
        private Stopwatch _stopwatch;
        private int _currentPing;
        private bool _isPingSent;
        
        private void Start()
        {
            _client = FindObjectOfType<NetworkClient>();
            
            if (_client == null)
            {
                Debug.LogError("PingMeter: No Client component found in the scene!");
                return;
            }
            
            _stopwatch = new Stopwatch();
            StartCoroutine(MeasurePingRoutine());
        }
        
        /// <summary>
        /// Coroutine to measure ping at regular intervals.
        /// </summary>
        private IEnumerator MeasurePingRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(pingUpdateInterval);
                
                if (!_isPingSent && _client.IsConnected)
                {
                    SendPing();
                }
            }
        }
        
        /// <summary>
        /// Sends a ping message to the server and starts the timer.
        /// </summary>
        private void SendPing()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            _isPingSent = true;
            
            _client.SendPingPacket();
        }
        
        /// <summary>
        /// Called when a ping response is received from the server.
        /// </summary>
        public void OnPingResponse()
        {
            if (!_isPingSent) return;
            
            _stopwatch.Stop();
            _currentPing = (int)_stopwatch.ElapsedMilliseconds;
            _isPingSent = false;
            
            UpdatePingDisplay();
        }
        
        /// <summary>
        /// Updates the ping display on screen.
        /// </summary>
        private void UpdatePingDisplay()
        {
            if (pingText != null)
            {
                // Good ping (green).
                string colorHex = "#00FF00"; 
                
                // Really bad ping.
                if (_currentPing > 200)
                    colorHex = "#FF0000"; 
                // Medium ping/
                else if (_currentPing > 100)
                    colorHex = "#FFFF00"; 
                
                pingText.text = $"<color={colorHex}>Ping: {_currentPing} ms</color>";
            }
        }
        
        /// <summary>
        /// Gets the current ping value.
        /// </summary>
        public int GetCurrentPing()
        {
            return _currentPing;
        }
    }
}