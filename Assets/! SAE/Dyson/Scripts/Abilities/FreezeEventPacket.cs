using Hamad.Scripts;
using UnityEngine;

namespace Leonardo.Scripts.Packets
{
    public class FreezeEventPacket : Packet
    {
        public int TargetPlayerTag { get; private set; }
        public float FreezeDuration { get; private set; }
        public string EffectName { get; private set; }

        public FreezeEventPacket() : base(PacketType.FreezeEvent, null)
        {
        }

        public FreezeEventPacket(PlayerData playerData, int targetPlayerTag, float freezeDuration, string effectName) 
            : base(PacketType.FreezeEvent, playerData)
        {
            this.playerData = playerData;
            TargetPlayerTag = targetPlayerTag;
            FreezeDuration = freezeDuration;
            EffectName = effectName ?? string.Empty;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(TargetPlayerTag);
            _binaryWriter.Write(FreezeDuration);
            _binaryWriter.Write(EffectName);
            return EndSerialize();
        }
    
        public new FreezeEventPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            TargetPlayerTag = _binaryReader.ReadInt32();
            FreezeDuration = _binaryReader.ReadSingle();
            EffectName = _binaryReader.ReadString();
            return this;
        }
    }
}