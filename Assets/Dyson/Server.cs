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

        private TcpListener tcpListener;
        // Start is called before the first frame update
        void Start()
        {
            Port = 4200;
        }

        private void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            
            // Keep listening if new clients join 
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }

        // Function called on JoinServer button
        
        public void JoinServer()
        {
            Debug.Log("Server started");
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }
    }
}
