using UnityEngine;

namespace Hamad.Scripts.Rotation
{
    public class PlayerRotationData
    {
        public PlayerData playerData;
        public Vector3 rot;

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

        public PlayerRotationData(PlayerData playerData, Vector3 rotIndex)
        {
            this.playerData = playerData;
            this.rot = rotIndex;
            
            this.xRot = rotIndex.x;
            this.yRot = rotIndex.y;
            this.zRot = rotIndex.z;
        }
    }
}
