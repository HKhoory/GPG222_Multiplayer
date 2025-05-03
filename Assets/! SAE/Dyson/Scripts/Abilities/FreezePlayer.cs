using System;
using System.Collections;
using System.Collections.Generic;
using Leonardo.Scripts.Abilities;
using UnityEngine;

namespace Dyson.GPG222.Abilities
{
    public class FreezePlayer : AbilityBase
    {
        private LayerMask layerMask;

        private void Awake()
        {
            layerMask = LayerMask.GetMask("Player");
        }
        
        private void Update()
        {
            UpdateCooldown(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                TryActivateAbility(transform);
            }
        }
        protected override bool ExecuteAbilityEffect(Transform playerTransform)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit,
                    Mathf.Infinity, layerMask))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance,
                    Color.yellow);
                Debug.Log("Did Hit");
                if (hit.rigidbody != null)
                {
                    hit.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    PlayEffect(hit.point, Quaternion.identity);
                    return true;
                }
            }
            else
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000,
                    Color.white);
                Debug.Log("Did not Hit");
            }
            return false;
        }
    }
}