using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Leonardo.Scripts
{
    /// <summary>
    /// This is a placeholder to test the functionality of my Client-related script(s).
    /// </summary>
    public class ServerPlaceholder : MonoBehaviour
    {
        [Header("- Server Settings")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private int serverPort = 2121;

        private Socket serverSocket;
        private List<Socket> clients = new List<Socket>();
        
        private Dictionary<int, PlayerData> playersInSession = new Dictionary<int, PlayerData>();

        private void Start()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
            serverSocket.Listen(10);
            serverSocket.Blocking = false;
            Debug.Log("ServerPlaceholder.cs: Waiting for players...");
        }

        private void Update()
        {
            
            // Handle new plaeyers.
            try
            {
                Socket newClient = serverSocket.Accept();
                clients.Add(newClient);
                newClient.Blocking = false;
                Debug.Log("ServerPlaceholder.cs: Player connected!!");
            }
            catch (SocketException socketException)
            {
                Debug.LogWarning(socketException.Message);
                throw;
            }
            
            
        }
    }
}