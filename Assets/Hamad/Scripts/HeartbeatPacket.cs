using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hamad.Scripts.Heartbeat
{

    public class HeartbeatPacket : Packet
    {

        public byte heartbeat {  get; private set; }

        public HeartbeatPacket() : base(PacketType.None, null)
        {
            heartbeat = 0x01;
        }

        public HeartbeatPacket(PlayerData playerData, byte playerHeartbeat)
        {
            this.playerData = playerData;
            heartbeat = playerHeartbeat;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(heartbeat);
            return EndSerialize();
        }

        public new HeartbeatPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            heartbeat = _binaryReader.ReadByte();
            return this;
        }

    }

}