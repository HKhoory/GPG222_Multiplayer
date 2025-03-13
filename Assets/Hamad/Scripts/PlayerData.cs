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


        public Quaternion rotation { get; set; }

        public PlayerData(string name, int tag)
        {
            this.name = name;
            this.tag = tag;
            //this.position = position;

        }
    }


}