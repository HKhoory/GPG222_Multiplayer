using System.Net.Sockets;

namespace Leonardo.Scripts.ClientRelated
{
    public class ClientHandler
    {
        public int Id {get; private set;}
        public TcpClient Socket {get; private set;}

        public ClientHandler(int id, TcpClient socket)
        {
            Id = id;
            Socket = socket;
        }
        
        public bool Connected => Socket != null && Socket.Connected;
    }
}
