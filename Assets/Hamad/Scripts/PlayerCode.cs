using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCode : MonoBehaviour
{

    [SerializeField] private Rigidbody _rb;

    [SerializeField] private int playerIndex;

    [SerializeField] private float moveSpeed;

    [SerializeField] private Vector2 movementInput; //if we are going to use the new input system, else no need for Vector2


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        //_rb.velocity = new Vector3(movementInput.x * moveSpeed, _rb.velocity.y, movementInput.y * moveSpeed;
    }

    void Update()
    {
        
    }


    //public void OnMove(InputAction.CallbackContext x) => movementInput = x.ReadValue<Vector2>();
    //might need to edit the above one so it takes two separate float values instead of vector2

    public int GetPlayerIndex()
    {
        return playerIndex;
    }




}
