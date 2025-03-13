using Hamad;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public class PlayersPositionDataPacket : Packet
{
    public List<PlayerPositionData> PlayerPositionData { get; private set; }

    public PlayersPositionDataPacket() : base(PacketType.None, null)
    {
        PlayerPositionData = new List<PlayerPositionData>();
    }

    public PlayersPositionDataPacket(PlayerData playerData, List<PlayerPositionData> playerPositionData) : base(PacketType.PlayersPositionData, playerData)
    {
        this.playerData = playerData;
        this.PlayerPositionData = playerPositionData;
    }

    public byte[] Serialize()
    {
        BeginSerialize();
        _binaryWriter.Write(PlayerPositionData.Count);

        for (int i = 0; i < PlayerPositionData.Count; i++)
        {
            _binaryWriter.Write(PlayerPositionData[i].playerData.name);
            _binaryWriter.Write(PlayerPositionData[i].playerData.tag);
            _binaryWriter.Write(PlayerPositionData[i].posIndex);
        }
        return EndSerialize();
    }

    public new PlayersPositionDataPacket Deserialize(byte[] buffer)
    {
        base.Deserialize(buffer);
        int playerPositionDataListCount = _binaryReader.ReadInt32();

        PlayerPositionData = new List<PlayerPositionData>(playerPositionDataListCount);

        for (int i = 0; i < playerPositionDataListCount; i++)
        {

            //PlayerData pData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64());
            PlayerData pData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32());
            PlayerPositionData ppData = new PlayerPositionData(pData, _binaryReader.ReadInt32());
            PlayerPositionData.Add(ppData);

        }

        return this;


    }
}
