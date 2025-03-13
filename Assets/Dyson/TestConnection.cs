using System.Collections;
using System.Collections.Generic;
using Dyson_GPG222_Server;
using UnityEngine;

namespace Dyson_GPG222_Server
{
    public class TestConnection : MonoBehaviour
    {
        public void RunServer()
        {
            Server.Start(4, 26950);
        }
    }
}
