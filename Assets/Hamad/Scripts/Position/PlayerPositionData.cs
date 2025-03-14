using UnityEngine;

// LEO: Added namespace.
namespace Hamad.Scripts.Position
{
    public class PlayerPositionData
    {

        public PlayerData playerData;
        public Vector3 pos;

        public float xPos { get; set; }
        public float yPos { get; set; }
        public float zPos { get; set; }

        public PlayerPositionData(PlayerData playerData, float xPos, float yPos, float zPos)
        {
            this.playerData = playerData;
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
}
