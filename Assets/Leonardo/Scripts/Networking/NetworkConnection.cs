using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Leonardo.Scripts.Networking
{
    public class NetworkConnection
    {
        private Socket _socket;
        private readonly string ipAddress;
        private readonly int port;
        private bool _isConnected;
        
        public event Action OnConnected;
        public event Action<byte[]> OnDataReceived;
        public event Action OnDisconnected;
        
        public bool IsConnected => _isConnected;

        //Hamad: Adding variables for HeartBeat
        
        private float heartbeatInterval = 5f;


        public NetworkConnection(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
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
                    OnConnected?.Invoke();
                    return true;
                }
            }
            catch (SocketException e)
            {
                UnityEngine.Debug.LogWarning($"Connection error: {e.Message}");
            }
            
            return false;
        }
        
        public void Update()
        {
            heartbeatInterval -= 0.01f; //deducting the interval (time.deltatime dude)

            if (_socket != null && _socket.Connected && _socket.Available > 0)
            {
                try
                {
                    byte[] buffer = new byte[_socket.Available];
                    _socket.Receive(buffer);
                    
                    if (buffer.Length > 0)
                    {
                        OnDataReceived?.Invoke(buffer);
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
                    UnityEngine.Debug.LogError($"NetworkConnection.cs: {e.Message}");

                    //Hamad: adding in Mustafa's code until I figure out integrating heartbeat
                    //need to add that heartbeat wasn't detected for around 5 seconds

                    if (e.SocketErrorCode == SocketError.ConnectionAborted || e.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        Console.WriteLine("Server disconnected");
                        _socket.Close();
                        return;
                    }

                }
            }
        }


        //Gets called if the heartbeat exists
        public void CheckHeartbeat()
        {

            if (heartbeatInterval <= 0)
            {

                Disconnect();

            }

            else
            {
                heartbeatInterval = 5f;
            }



        }
        
        
        public void Disconnect()
        {
            if (_socket != null && _socket.Connected)
            {
                _socket.Close();
                _isConnected = false;
                OnDisconnected?.Invoke();
            }

        }

        

    }
}