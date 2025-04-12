using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Hamad.Scripts.Pause
{
    public class PausePacket : Packet
    {

        public bool isPaused { get; private set; }

        public PausePacket(): base(PacketType.None, null)
        {
            isPaused = false;
        }

        public PausePacket(PlayerData playerData, bool paused)
        {
            this.playerData = playerData;
            paused = isPaused;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(isPaused);
            return EndSerialize();
        }

        public new PausePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            isPaused = _binaryReader.ReadBoolean();
            return this;
        }


    }
}