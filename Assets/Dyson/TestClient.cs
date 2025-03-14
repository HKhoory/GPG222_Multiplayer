using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Hamad.Scripts;
using Hamad.Scripts.Message;
using Hamad.Scripts.Position;
using Unity.Collections.LowLevel.Unsafe;

namespace Dyson_GPG222_Server
{
    public class TestClient
    {
        public static int dataBufferSize = 4096;
        public int id;
        public TCP tcp;

        public TestClient(int clientId)
        {
            id = clientId;
            tcp = new TCP(id);
        }
        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            private byte[] receiveBuffer;

            public TCP(int id)
            {
                this.id = id;
            }

            public void Connect(TcpClient socket)
            {
                this.socket = socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        // LEO: This is where the disconnect logic goes.
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    // LEO: Parse data as package.
                    Packet basePacket = new Packet();
                    basePacket.Deserialize(data);

                    // LEO: Do logic for the specific packet.
                    switch (basePacket.packetType)
                    {
                        // Handle chat messages.
                        case Packet.PacketType.Message:
                            MessagePacket messagePacket = new MessagePacket().Deserialize(data);
                            Debug.LogWarning($"TestClient.cs: received message from: {messagePacket.playerData.name} = {messagePacket.Message}");
                            break;
                        
                        // Color packages.
                        case Packet.PacketType.Color:
                            // I don't think we'll end up using color but whatever logic for these packages goes here.
                            break;
                        
                        case Packet.PacketType.PlayersPositionData:
                            PlayersPositionDataPacket playersPositionDataPacket = new PlayersPositionDataPacket().Deserialize(data);
                            Debug.LogWarning($"Received positions for {playersPositionDataPacket.PlayerPositionData.Count} players.");
                            
                            // TODO: Add sync logic here later.
                            
                            break;
                        
                        case Packet.PacketType.PlayersRotationData:
                            PlayersRotationDataPacket playersRotationDataPacket = new PlayersRotationDataPacket().Deserialize(data);
                            Debug.LogWarning($"Received rotations for {playersRotationDataPacket.PlayerRotationData.Count} players.");
                        
                            // TODO: Add sync logic here later.
                            
                            break;
                        
                        default:
                            Debug.LogWarning($"TestClient.cs: received unknown packet type: {basePacket.packetType}");
                            break;
                    }
                    
                    // Check for next piece of data.
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                    // LEO: Handle disconnect and/or cleanup here later.
                }
            }
        }
    }
}
