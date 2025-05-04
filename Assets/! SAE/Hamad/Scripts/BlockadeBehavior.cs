using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockadeBehavior : MonoBehaviour
{
    [SerializeField] private float blockadeLife;

    // Update is called once per frame
    void Update()
    {
        blockadeLife -= Time.deltaTime;
        if (blockadeLife < 0 )
        {
            Destroy(gameObject);
        }
    }
}
