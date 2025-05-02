using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Hamad.Scripts.Restart
{
    public class RestartPacket : Packet
    {

        public bool isReset { get; private set; }

        public RestartPacket() : base(PacketType.None, null)
        {
            isReset = false;
        }

        public RestartPacket(PlayerData playerData, bool paused)
        {
            this.playerData = playerData;
            paused = isReset;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(isReset);
            return EndSerialize();
        }

        public new RestartPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            isReset = _binaryReader.ReadBoolean();
            return this;
        }


    }
}