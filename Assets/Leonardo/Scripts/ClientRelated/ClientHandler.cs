using System.Net.Sockets;

namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// This class manages an individual client connection on the server.
    /// </summary>
    public class ClientHandler
    {
        /// <summary>
        /// The unique ID assigned to this client.
        /// </summary>
        public int Id {get; private set;}
        
        /// <summary>
        /// The TcpClient socket for this client connection.
        /// </summary>
        public TcpClient Socket {get; private set;}

        /// <summary>
        /// Gets wether the client is currently connected or not.
        /// </summary>
        public bool Connected => Socket != null && Socket.Connected;

        /// <summary>
        /// Creates a new client handler with the specified ID and socket.
        /// </summary>
        /// <param name="id">Client ID.</param>
        /// <param name="socket">The client socket.</param>
        public ClientHandler(int id, TcpClient socket)
        {
            Id = id;
            Socket = socket;
        }
    }
}
