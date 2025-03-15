using System.Net.Sockets;
using UnityEngine;

namespace Leonardo.Scripts
{
    public class ClientState
    {
        public TcpClient Client;
        public int ClientId;
        public byte[] Buffer;
    }
}
