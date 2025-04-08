using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dyson.GPG222.Lobby
{
    public class JoinLobby : MonoBehaviour
    {
        public GameObject joinLobbyCanvas;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void JoinLobbyButton()
        {
            joinLobbyCanvas.SetActive(false);
        }
    }
}
