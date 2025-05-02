using System;
using System.Diagnostics;
using System.Net.Sockets;
using UnityEngine;

namespace Leonardo.Scripts.Networking
{
    public class NetworkConnection : MonoBehaviour
    {
        private Socket _socket;
        private readonly string ipAddress;
        private readonly int port;
        private bool _isConnected;
        
        public event Action OnConnected;
        public event Action<byte[]> OnDataReceived;
        public event Action OnDisconnected;
        
        public bool IsConnected => _isConnected;

        // Heartbeat settings
        private float heartbeatInterval = 5f;
        private float heartbeatTimer = 0f;
        private float lastHeartbeatTime = 0f;
        private float heartbeatTimeout = 10f;
        private bool useHeartbeatSystem = false;


        public NetworkConnection(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            ResetHeartbeat();
        }
        
        public bool Connect()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(ipAddress, port);
                _socket.Blocking = false;
                
                if (_socket.Connected)
                {
                    _isConnected = true;
                    ResetHeartbeat();
                    OnConnected?.Invoke();
                    UnityEngine.Debug.Log($"NetworkConnection: Successfully connected to {ipAddress}:{port}");
                    return true;
                }
            }
            catch (SocketException e)
            {
                UnityEngine.Debug.LogWarning($"NetworkConnection: Connection error: {e.Message}");
            }
            
            return false;
        }
        
        public void Update()
        {
            if (!_isConnected || _socket == null)
                return;

            if (useHeartbeatSystem) 
            {
                // Update heartbeat timer
                heartbeatTimer -= Time.deltaTime;
                float timeSinceLastHeartbeat = Time.time - lastHeartbeatTime;
            
                // Send heartbeat if timer expired
                if (heartbeatTimer <= 0)
                {
                    SendHeartbeat();
                    heartbeatTimer = heartbeatInterval;
                }
            
                // Check for heartbeat timeout
                if (timeSinceLastHeartbeat > heartbeatTimeout)
                {
                    UnityEngine.Debug.LogWarning("NetworkConnection: Heartbeat timeout, disconnecting");
                    Disconnect();
                    return;
                }
            }

            // Check for incoming data
            if (_socket != null && _socket.Connected)
            {
                try
                {
                    if (_socket.Available > 0)
                    {
                        byte[] buffer = new byte[_socket.Available];
                        _socket.Receive(buffer);
                        
                        if (buffer.Length > 0)
                        {
                            OnDataReceived?.Invoke(buffer);
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                    {
                        UnityEngine.Debug.LogError($"NetworkConnection.cs: {e.Message}");
                        Disconnect();
                    }
                }
            }
            else if (_isConnected)
            {
                // Socket disconnected but we haven't updated our state
                Disconnect();
            }
        }
        
        public void SendData(byte[] data)
        {
            if (!_isConnected || _socket == null) return;
            
            try
            {
                _socket.Send(data);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    UnityEngine.Debug.LogError($"NetworkConnection.cs: Send error: {e.Message}");

                    if (e.SocketErrorCode == SocketError.ConnectionAborted || 
                        e.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        UnityEngine.Debug.LogWarning("NetworkConnection: Server disconnected");
                        Disconnect();
                    }
                }
            }
        }

        private void SendHeartbeat()
        {
            // This would be implemented in NetworkClient, which would call
            // its SendHeartBeat() method when this is triggered
        }

        // Called when a heartbeat is received
        public void CheckHeartbeat()
        {
            // Reset the heartbeat timer
            ResetHeartbeat();
        }
        
        private void ResetHeartbeat()
        {
            heartbeatTimer = heartbeatInterval;
            lastHeartbeatTime = Time.time;
        }
        
        public void Disconnect()
        {
            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
                    else
                    {
                        _socket.Close();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"NetworkConnection: Error during disconnect: {e.Message}");
                }
                
                _socket = null;
            }
            
            if (_isConnected)
            {
                _isConnected = false;
                OnDisconnected?.Invoke();
                UnityEngine.Debug.Log("NetworkConnection: Disconnected from server");
            }
        }
    }
}