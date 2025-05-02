using System.Collections;
using System.Diagnostics;
using __SAE.Leonardo.Scripts.ClientRelated;
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
        [SerializeField] private float pingUpdateInterval = 1.0f; // How often to measure ping (in seconds)
        [SerializeField] private float pingTimeoutDuration = 5.0f; // How long to wait before considering a ping lost
        
        private NetworkClient _client;
        private Stopwatch _stopwatch;
        private int _currentPing;
        private bool _isPingSent;
        private float _pingTimer;
        private float _timeoutTimer;
        
        [Header("Display Settings")]
        [SerializeField] private Color goodPingColor = Color.green;
        [SerializeField] private Color mediumPingColor = Color.yellow;
        [SerializeField] private Color badPingColor = Color.red;
        [SerializeField] private int mediumPingThreshold = 100;
        [SerializeField] private int badPingThreshold = 200;
        
        private void Start()
        {
            _client = FindObjectOfType<NetworkClient>();
            
            if (_client == null)
            {
                Debug.LogError("PingMeter: No NetworkClient component found in the scene!");
                return;
            }
            
            _stopwatch = new Stopwatch();
            _pingTimer = pingUpdateInterval;
            _timeoutTimer = pingTimeoutDuration;
            
            if (pingText != null)
            {
                pingText.text = "Ping: -- ms";
            }
            
            StartCoroutine(MeasurePingRoutine());
        }
        
        private void Update()
        {
            // Update ping timer
            _pingTimer -= Time.deltaTime;
            
            // Check for ping timeout
            if (_isPingSent)
            {
                _timeoutTimer -= Time.deltaTime;
                
                if (_timeoutTimer <= 0f)
                {
                    // Ping timed out
                    _stopwatch.Stop();
                    _isPingSent = false;
                    _timeoutTimer = pingTimeoutDuration;
                    
                    Debug.LogWarning("PingMeter: Ping timed out");
                    
                    // Update display to show high ping or disconnected
                    if (pingText != null)
                    {
                        pingText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(badPingColor)}>Ping: Timeout</color>";
                    }
                }
            }
        }
        
        /// <summary>
        /// Coroutine to measure ping at regular intervals.
        /// </summary>
        private IEnumerator MeasurePingRoutine()
        {
            while (true)
            {
                // Wait for interval
                yield return new WaitForSeconds(0.1f);
                
                // Only send ping if we're connected and not already waiting for a response
                if (!_isPingSent && _client.IsConnected && _pingTimer <= 0f)
                {
                    SendPing();
                    _pingTimer = pingUpdateInterval;
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
            _timeoutTimer = pingTimeoutDuration;
            
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
                // Determine color based on ping value
                Color pingColor = goodPingColor;
                
                if (_currentPing > badPingThreshold)
                {
                    pingColor = badPingColor;
                }
                else if (_currentPing > mediumPingThreshold)
                {
                    pingColor = mediumPingColor;
                }
                
                string colorHex = ColorUtility.ToHtmlStringRGB(pingColor);
                pingText.text = $"<color=#{colorHex}>Ping: {_currentPing} ms</color>";
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