using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hamad;
using Hamad.Scripts;

public class PlayerRotationData
{
    public PlayerData playerData;
    public Vector3 pos;

    public float xRot { get; set; }
    public float yRot { get; set; }
    public float zRot { get; set; }

    public PlayerRotationData(PlayerData playerData, float xRot, float yRot, float zRot)
    {
        this.playerData = playerData;
        this.xRot = xRot;
        this.yRot = yRot;
        this.zRot = zRot;
    }

    public PlayerRotationData(PlayerData playerData, Vector3 posIndex)
    {
        this.playerData = playerData;
        this.pos = posIndex;
    }
}
