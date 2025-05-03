using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using __SAE.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Abilities;

namespace Hamad.Scripts.Blockade
{
    public class Blockade : AbilityBase
    {

        private LayerMask layerMask;
        private float blockadeDuration;
        private Rigidbody _rb;
        //gameobject for blockade

        private void Awake()
        {
            layerMask = LayerMask.GetMask("Player");
        }

        void Update()
        {
            //UpdateCooldown(Time.deltaTime);
            
            //TryActivateAbility(transform);
        }

        //private IEnumerator BlockadeAbility(float duration)
        //{
//
        //}


    }

}