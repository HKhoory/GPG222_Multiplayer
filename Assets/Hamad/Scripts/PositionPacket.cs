using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionPacket : Packet
{
    public Vector3 Position { get; private set; }

    /*
    public PositionPacket() : base(PacketType.None, null)
    {
        Position = new Vector3(0, 0, 0);
    }
    */

    public byte[] Serialize()
    {
        BeginSerialize();
        //_binaryWriter.Write(Position);
        return EndSerialize();
    }

    public new PositionPacket Deserialize(byte[] buffer)
    {
        base.Deserialize(buffer);
        //Position = _binaryReader.ReadInt32();
        return this;
    }

}
