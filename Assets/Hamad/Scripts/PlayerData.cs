using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hamad
{
    public class PlayerData
    {
        public string name { get; private set; }
        public int tag { get; private set; }
        public Vector3 position { get; set; }

        public float xPos { get; set; }
        public float yPos { get; set; }
        public float zPos { get; set; }
        public Quaternion rotation { get; set; }

        public PlayerData(string name, int tag, float xPos, float yPos, float zPos)
        {
            this.name = name;
            this.tag = tag;
            //this.position = position;
            this.xPos = xPos;
            this.yPos = yPos;
            this.zPos = zPos;
        }
    }


}