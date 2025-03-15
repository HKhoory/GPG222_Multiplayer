using Dyson_GPG222_Server;
using TMPro;
using UnityEngine;

namespace Dyson.Deprecated
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;

        public GameObject startMenu;
        public TMP_InputField usernameField;
    
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.Log("Instance already exist");
                Destroy(this);
            }
        }

        public void ConnectToServer()
        {
            startMenu.SetActive(false);
            usernameField.interactable = false;
            UnityClient.instance.ConnectToServer();
        }
    }
}
