// LEO: Added namespace

using System.Numerics;

namespace Hamad.Scripts.Position
{
    public class PositionPacket : Packet
    {
        public float xPosition { get; private set; }
        public float yPosition { get; private set; }
        public float zPosition { get; private set; }

    
        public PositionPacket() : base(PacketType.None, null)
        {
            xPosition = 0;
            yPosition = 0;
            zPosition = 0;
        }
    
        // Leo: Added new constructors.
        public PositionPacket(PlayerData playerData, float xPosition, float yPosition, float zPosition) : base(
            PacketType.None, playerData)
        {
            this.playerData = playerData;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
        }
        
        public PositionPacket(PlayerData playerData, Vector3 position) : base(
            PacketType.None, playerData)
        {
            this.playerData = playerData;
            this.xPosition = position.X;
            this.yPosition = position.Y;
            this.zPosition = position.Z;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(xPosition);
            _binaryWriter.Write(yPosition);
            _binaryWriter.Write(zPosition);
            return EndSerialize();
        }

        public new PositionPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            //Position = _binaryReader.ReadInt32();
            xPosition = _binaryReader.ReadSingle();
            yPosition = _binaryReader.ReadSingle();
            zPosition = _binaryReader.ReadSingle();
            return this;
        }

    }
}
