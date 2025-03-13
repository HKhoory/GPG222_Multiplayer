using Hamad;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionData
{

    public PlayerData playerData;
    public Vector3 pos;

    public float xPos { get; set; }
    public float yPos { get; set; }
    public float zPos { get; set; }

    public PlayerPositionData(PlayerData playerData, float xPos, float yPos, float zPos)
    {
        this.xPos = xPos;
        this.yPos = yPos;
        this.zPos = zPos;
    }

    public PlayerPositionData(PlayerData playerData, Vector3 posIndex)
    {
        this.playerData = playerData;
        this.pos = posIndex;
    }

}
