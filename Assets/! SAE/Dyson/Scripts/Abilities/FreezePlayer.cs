using System;
using System.Collections;
using System.Collections.Generic;
using __SAE.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Abilities;
using UnityEngine;

namespace Dyson.GPG222.Abilities
{
    public class FreezePlayer : AbilityBase
    {
        private LayerMask layerMask;
        private float freezeDuration;
        private Rigidbody _rb;
        private RaycastHit hit;
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
            
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit,
                    Mathf.Infinity, layerMask))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance,
                    Color.yellow);
                Debug.Log("Did Hit");
                StartCoroutine(FreezeCoroutine(freezeDuration));
                if (hit.rigidbody != null)
                {
                    hit.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    PlayEffect(hit.point, Quaternion.identity);
                    return true;
                }
              /*  // Only send network event.
                int targetPlayerTag = remotePlayer.PlayerTag;
                NetworkClient networkClient = FindObjectOfType<NetworkClient>();
                if (networkClient != null && targetPlayerTag != -1)
                {
                    networkClient.SendFreezeEvent(targetPlayerTag, freezeDuration, effectName);
                    // Play effect locally.
                    PlayEffect(collider.transform.position, collider.transform.rotation);
                    Debug.Log($"BasicPush.cs: Sent network push event for player with tag {targetPlayerTag}.");
                }
                else
                {
                    Debug.LogWarning(
                        $"BasicPush.cs: Could not send network push event - NetworkClient or player tag not found.");
                } */
            }
            else
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000,
                    Color.white);
                Debug.Log("Did not Hit");
            }
            return false;
        }
        
        private IEnumerator FreezeCoroutine(float freezeDuration)
        {
            if (hit.rigidbody != null)
            {
                hit.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }

            yield return new WaitForSeconds(freezeDuration);

            if (hit.rigidbody != null)
            {
                hit.rigidbody.constraints = RigidbodyConstraints.None;
            }
        }
    }
}