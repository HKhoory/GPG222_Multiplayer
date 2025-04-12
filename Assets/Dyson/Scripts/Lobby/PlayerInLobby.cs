using System.Collections;
using System.Collections.Generic;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInLobby : MonoBehaviour
{
    public GameObject playerIdText;

    public void Setup(ClientState clientState)
    {
        playerIdText.GetComponent<TextMeshPro>().text = clientState.ClientId.ToString();
      //  Invoke("GoToGame", 2.0f);
    }

    public void GoToGame()
    {
        // SceneManager.LoadScene("Scenes/Client");
    }
}
