/*using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Leonardo.Scripts;
using UnityEngine;

namespace Mustafa
{
    public class Server : MonoBehaviour
    {
        [SerializeField] string ipAddress;
        [SerializeField] int port;
        Socket server;

        List<Socket> clients = new List<Socket>();

        PlayerData serverPayerData;
        //public List<PlayerColorData> playerColorData = new List<PlayerColorData>();

        void Start()
        {
            serverPayerData = new PlayerData("SERVER", Random.Range(0,9999), Vector3.zero, Quaternion.identity);

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));
            server.Listen(1000);
            Debug.LogError("Waiting for connection...");
            server.Blocking = false;
        }

        void Update()
        {
            try
            {
                clients.Add(server.Accept());

                /*if (playerColorData.Count > 0)
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        byte[] buffer = new PlayersColorDataPacket(serverPayerData, playerColorData).Serialize();
                        clients[i].Send(buffer);
                    }
                }#1#

                Debug.LogError("Client connected!");
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError(e.ToString());
                }
            }

            try
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Available > 0)
                    {
                        byte[] buffer = new byte[clients[i].Available];
                        clients[i].Receive(buffer);

                        /*BasePacket bp = new BasePacket();
                        bp.Deserialize(buffer);

                        switch (bp.packetType)
                        {
                            case BasePacket.PacketType.Color:
                                ColorPacket cp = new ColorPacket().Deserialize(buffer);
                                playerColorData.Add(new PlayerColorData(bp.playerData, cp.ColorIndex));
                                break;
                        }#1#

                        for (int j = 0; j < clients.Count; j++)
                        {
                            if (i == j)
                                continue;

                            clients[j].Send(buffer);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogError(e.ToString());
                }
            }
        }
    }
}*/