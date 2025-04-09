using System;
using System.Net.Sockets;
using UnityEngine;

namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// This class holds state information for connected clients during async operations.
    /// </summary>
    public class ClientState : MonoBehaviour

    {
    /// <summary>
    /// The TcpClient instance for this connection.
    /// </summary>
    public TcpClient Client;

    /// <summary>
    /// The unique ID assigned to this client.
    /// </summary>
    public int ClientId;

    /// <summary>
    /// Buffer used for receiving data from this client.
    /// </summary>
    public byte[] Buffer;
    
    /// <summary>
    /// Boolean to check if the player is ready in the Lobby
    /// </summary>
    public bool isReady;
    }
}
