using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

namespace Dyson_GPG222_Server
{
    public class Server : MonoBehaviour
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }

        private static TcpListener tcpListener;

        public static Dictionary<int, TestClient> clients = new Dictionary<int, TestClient>();
        // Start is called before the first frame update
        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;
            Debug.Log( $"Server started on {Port}");
            InitializeServerData();
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            
            // Keep listening if new clients join 
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
             Debug.Log(client.Client.RemoteEndPoint + " : Player connecting");
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }
            
            Debug.Log(client.Client.RemoteEndPoint + " : Server full");
        }

        private static void InitializeServerData()
        {
            for (int i = 0; i <= MaxPlayers; i++)
            {
                clients.Add(i, new TestClient(i));
            }
        }
    }
}
