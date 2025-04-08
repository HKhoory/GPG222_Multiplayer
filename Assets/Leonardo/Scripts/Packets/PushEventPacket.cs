using Hamad.Scripts;
using UnityEngine;

namespace Leonardo.Scripts.Packets
{
    public class PushEventPacket : Packet
    {
        public int TargetPlayerTag { get; private set; }
        public float ForceX { get; private set; }
        public float ForceY { get; private set; }
        public float ForceZ { get; private set; }
        public string EffectName { get; private set; }

        public PushEventPacket() : base(PacketType.PushEvent, null)
        {
        }

        public PushEventPacket(PlayerData playerData, int targetPlayerTag, Vector3 force, string effectName) 
            : base(PacketType.PushEvent, playerData)
        {
            this.playerData = playerData;
            TargetPlayerTag = targetPlayerTag;
            ForceX = force.x;
            ForceY = force.y;
            ForceZ = force.z;
            EffectName = effectName ?? string.Empty;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(TargetPlayerTag);
            _binaryWriter.Write(ForceX);
            _binaryWriter.Write(ForceY);
            _binaryWriter.Write(ForceZ);
            _binaryWriter.Write(EffectName);
            return EndSerialize();
        }
    
        public new PushEventPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            TargetPlayerTag = _binaryReader.ReadInt32();
            ForceX = _binaryReader.ReadSingle();
            ForceY = _binaryReader.ReadSingle();
            ForceZ = _binaryReader.ReadSingle();
            EffectName = _binaryReader.ReadString();
            return this;
        }
    }
}