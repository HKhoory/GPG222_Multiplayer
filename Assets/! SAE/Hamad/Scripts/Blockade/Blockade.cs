using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using __SAE.Leonardo.Scripts.ClientRelated;
using Leonardo.Scripts.Abilities;
using Leonardo.Scripts.Controller;

namespace Hamad.Scripts.Blockade
{
    public class Blockade : AbilityBase
    {

        private LayerMask layerMask;
        private int playerTag;
        private float blockadeDuration;
        private bool isActive;
        private bool meshRenderer;
        [SerializeField] private BoxCollider _coll;
        [SerializeField] private MeshRenderer _mesh;
            
        private void Awake()
        {
            layerMask = LayerMask.GetMask("Player");
            _coll = GetComponent<BoxCollider>();
            _mesh = GetComponent<MeshRenderer>();
            _coll.isTrigger = false;
            _mesh.enabled = false;
        }

        void Update()
        {
            UpdateCooldown(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryActivateAbility(transform);
            }
        }

        protected override bool ExecuteAbilityEffect(Transform playerTransform)
        {

                NetworkClient networkClient = FindObjectOfType<NetworkClient>();
                if (networkClient != null)
                {
                    networkClient.SendBlockade(playerTag, blockadeDuration, effectName);
                    // Play effect locally.
                    Debug.Log($"FreezePlayer.cs: Sent network freeze event for player with tag {playerTag}.");
                }
                else
                {
                    Debug.LogWarning(
                        $"FreezePlayer.cs: Could not send freeze event - NetworkClient or player tag not found.");
                }
                StartCoroutine(BlockadeAbility(blockadeDuration));


                return false;
            
        }

        private IEnumerator BlockadeAbility(float duration)
        {
            //activate
            _coll.isTrigger = true;
            _mesh.enabled = true;

            yield return new WaitForSeconds(duration);

            _coll.isTrigger = false;
            _mesh.enabled = false;

            //deactivate


        }



    }

}