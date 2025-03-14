// LEO: Added namespace
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
