using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hamad.Scripts.Blockade
{
    public class BlockadePacket : Packet
    {
        public int PlayerTag { get; private set; }
        public byte blockade { get; private set; }

        public float xPos { get; private set; }
        public float yPos { get; private set; }
        public float zPos { get; private set; }
        public string EffectName { get; private set; }

        public BlockadePacket() : base(PacketType.None, null)
        {
            blockade = 0x01;
            xPos = 0;
            yPos = 0;
            zPos = 0;
        }

        public BlockadePacket(PlayerData playerData, byte blockadePacket)
        {
            this.playerData = playerData;
            blockade = blockadePacket;
        }

        public BlockadePacket(PlayerData playerData, byte blockadePacket, float xPositon, float yPositon, float Zposition, int playerTag, string effectName)
        {
            this.playerData = playerData;
            blockade = blockadePacket;
            PlayerTag = playerTag;
            xPositon = xPos;
            yPositon = yPos;
            Zposition = zPos;
            EffectName = effectName;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(blockade);
            _binaryWriter.Write(xPos);
            _binaryWriter.Write(yPos);
            _binaryWriter.Write(zPos);
            _binaryWriter.Write(PlayerTag);
            _binaryWriter.Write(EffectName);
            return EndSerialize();
        }

        public new BlockadePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            blockade = _binaryReader.ReadByte();
            PlayerTag = _binaryReader.ReadInt32();
            xPos = _binaryReader.ReadSingle();
            yPos = _binaryReader.ReadSingle();
            zPos = _binaryReader.ReadSingle();
            EffectName = _binaryReader.ReadString();
            return this;
        }


    }

}