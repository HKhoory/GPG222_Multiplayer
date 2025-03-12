using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Leonardo.Scripts
{
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private int port = 8080;
        
        private Socket socket;
        public static NetworkManager instance {get; private set;}
        private Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();
        private PlayerData localPlayer;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Try to connect to server.
            try
            {
                localPlayer = new PlayerData("Player", Random.Range(0, 9999), Vector3.zero, Quaternion.identity);
                socket.Connect(ipAddress, port);
                socket.Blocking = false;
                Debug.Log("Connected to server! Yay!");
                
                // Send the player data.
            }
            catch (SocketException socketException)
            {
                Debug.LogWarning($"NetworkManager.cs, socket exception: {socketException.Message}");
            }
            
        }
    }
}
