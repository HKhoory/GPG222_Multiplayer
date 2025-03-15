using System.Collections;
using System.Collections.Generic;
using Hamad.Scripts;
using UnityEngine;

public class RotationPacket : Packet
{
    public float xRotation { get; private set; }
    public float yRotation { get; private set; }
    public float zRotation { get; private set; }


    public RotationPacket() : base(PacketType.None, null)
    {
        xRotation = 0;
        yRotation = 0;
        zRotation = 0;
    }

    // Leo: Added new constructors.
    public RotationPacket(PlayerData playerData, float xRotation, float yRotation, float zRotation) : base(
        PacketType.None, playerData)
    {
        this.playerData = playerData;
        this.xRotation = xRotation;
        this.yRotation = yRotation;
        this.zRotation = zRotation;
    }
        
    public RotationPacket(PlayerData playerData, Vector3 rotation) : base(
        PacketType.None, playerData)
    {
        this.playerData = playerData;
        this.xRotation = rotation.x;
        this.yRotation = rotation.y;
        this.zRotation = rotation.z;
    }
    
    public byte[] Serialize()
    {
        BeginSerialize();
        _binaryWriter.Write(xRotation);
        _binaryWriter.Write(yRotation);
        _binaryWriter.Write(zRotation);
        return EndSerialize();
    }

    public new RotationPacket Deserialize(byte[] buffer)
    {
        base.Deserialize(buffer);
        xRotation = _binaryReader.ReadSingle();
        yRotation = _binaryReader.ReadSingle();
        zRotation = _binaryReader.ReadSingle();
        return this;
    }
}
