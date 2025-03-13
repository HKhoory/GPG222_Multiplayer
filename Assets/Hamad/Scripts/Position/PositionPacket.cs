using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        xPosition = _binaryReader.ReadInt64();
        yPosition = _binaryReader.ReadInt64();
        zPosition = _binaryReader.ReadInt64();
        return this;
    }

}
