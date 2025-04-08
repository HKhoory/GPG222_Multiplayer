using Leonardo.Scripts.Controller;
using UnityEngine;

namespace Leonardo.Scripts.Abilities
{
    /// <summary>
    /// An ability that pushes enemies in a cone shape in front of the player.
    /// FORCE: low
    /// COOLDOWN: low
    /// </summary>
    public class BasicPush : AbilityBase
    {
        [Header("- Cone Settings")]
        [Tooltip("Angle of the cone in degrees")]
        [SerializeField] private float coneAngle = 60f;
        
        [Tooltip("- Distance the cone extends")]
        [SerializeField] private float coneDistance = 5f;
        
        [Header("- Force Settings")]
        [Tooltip("Force applied to enemies hit by the ability")]
        [SerializeField] private float pushForce = 10f;
        
        // TODO: Check w team and delete later.
        [SerializeField] private bool useWorldSpace;
        
        [Tooltip("Upward force applied")]
        [SerializeField] private float upwardForce = 2f;
        
        protected override bool ExecuteAbilityEffect(Transform playerTransform)
        {
            Collider[] colliders = Physics.OverlapSphere(playerTransform.position, coneDistance);
            
            bool hitAnything = false;
            
            foreach (Collider collider in colliders)
            {
                if (collider.transform == playerTransform)
                {
                    continue;
                }
                
                // Check if this is a remote player.
                RemotePlayerController remotePlayer = collider.GetComponent<RemotePlayerController>();
                PlayerController playerController = collider.GetComponent<PlayerController>();
                
                if (remotePlayer == null && (playerController == null || playerController.IsLocalPlayer))
                {
                    continue;
                }
                
                Vector3 directionToTarget = collider.transform.position - playerTransform.position;
                
                // Ignore height difference for angle calculation?
                directionToTarget.y = 0; 
                
                float angleToTarget = Vector3.Angle(playerTransform.forward, directionToTarget);
                if (angleToTarget <= coneAngle / 2 && directionToTarget.magnitude <= coneDistance)
                {
                    ApplyPushForce(playerTransform, collider);
                    hitAnything = true;
                }
            }
            

            // Visual effect.
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, playerTransform.position, playerTransform.rotation);
                Destroy(effect, 2f);
            }

            return true;
        }
        
        private void ApplyPushForce(Transform playerTransform, Collider targetCollider)
        {
            Rigidbody targetRigidbody = targetCollider.GetComponent<Rigidbody>();
            
            if (targetRigidbody == null)
            {
                return;
            }
            
            Vector3 pushDirection;
            
            // TODO: CHECK THIS W TEAM LATER DONT FORGET!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! -----------------------------------------------------------------------------------------------------
            if (useWorldSpace)
            {
                // Use player's forward direction.
                pushDirection = playerTransform.forward;
            }
            else
            {
                // Push away from player position.
                pushDirection = (targetCollider.transform.position - playerTransform.position).normalized;
            }
            
            // Some upward force to make the thing more chaotic.
            pushDirection += Vector3.up * upwardForce;
            pushDirection.Normalize();
            
            targetRigidbody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            
            Debug.Log($"ConePushAbility.cs: Pushed {targetCollider.name} with force {pushForce}.");
        }
    }
}
