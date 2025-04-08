using System.Collections;
using System.Diagnostics;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Leonardo.Scripts.PingMeter
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
        
        private Client client;
        private Stopwatch stopwatch;
        private int currentPing;
        private bool isPingSent;
        
        private void Start()
        {
            // Find the client in the scene
            client = FindObjectOfType<Client>();
            
            if (client == null)
            {
                Debug.LogError("PingMeter: No Client component found in the scene!");
                return;
            }
            
            // Initialize the stopwatch
            stopwatch = new Stopwatch();
            
            // Start measuring ping regularly
            StartCoroutine(MeasurePingRoutine());
        }
        
        /// <summary>
        /// Coroutine to measure ping at regular intervals.
        /// </summary>
        private IEnumerator MeasurePingRoutine()
        {
            while (true)
            {
                // Wait for the specified interval
                yield return new WaitForSeconds(pingUpdateInterval);
                
                // Only send a new ping if we're not waiting for a response
                if (!isPingSent && client.IsConnected)
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
            stopwatch.Reset();
            stopwatch.Start();
            isPingSent = true;
            
            // Send a ping message to the server
            client.SendPingPacket();
        }
        
        /// <summary>
        /// Called when a ping response is received from the server.
        /// </summary>
        public void OnPingResponse()
        {
            if (!isPingSent) return;
            
            stopwatch.Stop();
            currentPing = (int)stopwatch.ElapsedMilliseconds;
            isPingSent = false;
            
            // Update the UI
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
                if (currentPing > 200)
                    colorHex = "#FF0000"; 
                // Medium ping/
                else if (currentPing > 100)
                    colorHex = "#FFFF00"; 
                
                pingText.text = $"<color={colorHex}>Ping: {currentPing} ms</color>";
            }
        }
        
        /// <summary>
        /// Gets the current ping value.
        /// </summary>
        public int GetCurrentPing()
        {
            return currentPing;
        }
    }
}