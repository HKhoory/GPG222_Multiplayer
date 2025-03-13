using Hamad;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionData
{

    public PlayerData playerData;
    public int posIndex;

    public float xPos { get; set; }
    public float yPos { get; set; }
    public float zPos { get; set; }

    public PlayerPositionData(float xPos, float yPos, float zPos)
    {
        this.xPos = xPos;
        this.yPos = yPos;
        this.zPos = zPos;
    }

    public PlayerPositionData(PlayerData playerData, int posIndex)
    {
        this.playerData = playerData;
        this.posIndex = posIndex;
    }

}
