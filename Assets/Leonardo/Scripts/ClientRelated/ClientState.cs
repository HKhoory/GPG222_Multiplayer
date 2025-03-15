using System.Net.Sockets;

namespace Leonardo.Scripts.ClientRelated
{
    public class ClientState
    {
        public TcpClient Client;
        public int ClientId;
        public byte[] Buffer;
    }
}
