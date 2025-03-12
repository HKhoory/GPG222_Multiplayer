using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Packet : MonoBehaviour
{
    protected MemoryStream _memoryStreamWriter;
    protected BinaryWriter _binaryWriter;

    protected MemoryStream _memoryStreamReader;
    protected BinaryReader _binaryReader;

    public enum PacketType
    {
        None,
        Message,
        Id,
        Color,
        xPos,
        yPos,
        zPos
    }

    public PacketType packetType;
    //public PlayerData playerData;

    public Packet()
    {
        packetType = PacketType.None;
        //playerData = null;
    }

    public Packet(PacketType packetType)
    {
        this.packetType = packetType;
        //have one for playerData too
    }

    protected void BeginSerialize()
    {
        _memoryStreamWriter = new MemoryStream();
        _binaryWriter = new BinaryWriter(_memoryStreamWriter);
        _binaryWriter.Write((int)packetType);
        //have one for playerData ID, maybe position, and color
        //tag as well
    }

    protected byte[] EndSerialize()
    {
        return _memoryStreamWriter.ToArray();
    }

    public void Deserialize(byte[] buffer)
    {
        //_memoryStreamReader = new MemoryStream(buffer);
        //_binaryReader = new BinaryReader(_memoryStreamReader);

        packetType = (PacketType)_binaryReader.ReadInt32();
        //playerData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32());

    }

}
