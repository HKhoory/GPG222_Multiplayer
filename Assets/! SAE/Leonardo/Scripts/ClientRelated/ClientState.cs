using System.Net.Sockets;

namespace __SAE.Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// This class holds state information for connected clients during async operations.
    /// </summary>
    public class ClientState // No longer inherits MonoBehaviour
    {
        /// <summary>
        /// The TcpClient instance for this connection. (May not be needed for Lobby UI)
        /// </summary>
        public TcpClient Client;

        /// <summary>
        /// The unique ID assigned to this client.
        /// </summary>
        public int ClientId;

        /// <summary>
        /// Buffer used for receiving data from this client. (May not be needed for Lobby UI)
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        /// Boolean to check if the player is ready in the lobby.
        /// </summary>
        public bool isReady;

        /// <summary>
        /// The name of the player associated with this state.
        /// </summary>
        public string name { get; set; } 
    }
}