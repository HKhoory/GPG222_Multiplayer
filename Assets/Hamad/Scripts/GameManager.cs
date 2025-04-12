using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    //game waits for the players to join and start the game

    //once the game starts, two lists will be there, one for the players, and one for the players who lost

    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject[] players;
    [SerializeField] private GameObject[] lostPlayers;

    [SerializeField] private bool restartGame;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        //set restart to false
        restartGame = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
