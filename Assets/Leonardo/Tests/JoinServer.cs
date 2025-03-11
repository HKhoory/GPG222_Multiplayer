using Unity.Netcode;
using UnityEngine;

namespace Leonardo.Tests
{
    public class JoinServer : MonoBehaviour
    {
        public void Join()
        {
            NetworkManager.Singleton.StartClient();
        }
    }
}