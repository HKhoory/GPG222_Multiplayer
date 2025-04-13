using System.Collections;
using System.Collections.Generic;
using Dyson.GPG222.Lobby;
using Leonardo.Scripts.ClientRelated;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInLobby : MonoBehaviour
{
    public GameObject playerIdText;
    public Lobby _Lobby;
    public void Setup(ClientState clientState)
    {
        _Lobby = FindObjectOfType<Lobby>();
        clientState = _Lobby.testPlayer;
        playerIdText.GetComponent<TextMeshPro>().text = clientState.ClientId.ToString();
      //  Invoke("GoToGame", 2.0f);
    }

    public void GoToGame()
    {
        SceneManager.LoadScene("Scenes/Client");
    }
}
