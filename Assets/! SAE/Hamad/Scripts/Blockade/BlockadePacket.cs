using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hamad.Scripts.Blockade
{
    public class BlockadePacket : Packet
    {
        public int PlayerTag { get; private set; }
        public float blockadeDuration { get; private set; }
        public string EffectName { get; private set; }

        public BlockadePacket() : base(PacketType.None, null)
        {
            
        }

        public BlockadePacket(PlayerData playerData, float blockadePacket, int playerTag, string effectName)
        {
            this.playerData = playerData;
            blockadeDuration = blockadePacket;
            PlayerTag = playerTag;
            EffectName = effectName;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(blockadeDuration);
            _binaryWriter.Write(PlayerTag);
            _binaryWriter.Write(EffectName);
            return EndSerialize();
        }

        public new BlockadePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            blockadeDuration = _binaryReader.ReadByte();
            PlayerTag = _binaryReader.ReadInt32();
            EffectName = _binaryReader.ReadString();
            return this;
        }


    }

}