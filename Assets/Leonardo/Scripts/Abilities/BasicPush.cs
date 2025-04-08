using Leonardo.Scripts.ClientRelated;
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
        [Header("- Cone Settings")] [Tooltip("Angle of the cone in degrees")] [SerializeField]
        private float coneAngle = 60f;

        [Tooltip("- Distance the cone extends")] [SerializeField]
        private float coneDistance = 5f;

        [Header("- Force Settings")] [Tooltip("Force applied to enemies hit by the ability")] [SerializeField]
        private float pushForce = 10f;

        // TODO: Check w team and delete later.
        [SerializeField] private bool useWorldSpace;

        [Tooltip("Upward force applied")] [SerializeField]
        private float upwardForce = 2f;

        protected override bool ExecuteAbilityEffect(Transform playerTransform)
        {
            Collider[] colliders = Physics.OverlapSphere(playerTransform.position, coneDistance);


            foreach (Collider collider in colliders)
            {
                if (collider.transform == playerTransform)
                {
                    continue;
                }

                // Check if this is a remote player.
                RemotePlayerController remotePlayer = collider.GetComponent<RemotePlayerController>();

                if (remotePlayer == null)
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
                }
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
            if (useWorldSpace)
            {
                pushDirection = playerTransform.forward;
            }
            else
            {
                pushDirection = (targetCollider.transform.position - playerTransform.position).normalized;
            }

            // Add upward force to make it chaotic and fun.
            pushDirection += Vector3.up * upwardForce;
            pushDirection.Normalize();

            Vector3 finalForce = pushDirection * pushForce;
            targetRigidbody.AddForce(finalForce, ForceMode.Impulse);

            // SEND TO NETWORK.
            RemotePlayerController remotePlayerController = targetCollider.GetComponent<RemotePlayerController>();

            if (remotePlayerController != null)
            {
                int targetPlayerTag = remotePlayerController.PlayerTag;

                NetworkClient networkClient = FindObjectOfType<NetworkClient>();
                if (networkClient != null && targetPlayerTag != -1)
                {
                    networkClient.SendPushEvent(targetPlayerTag, finalForce, effectName);
                    Debug.Log($"BasicPush.cs: Sent network push event for player with tag {targetPlayerTag}.");
                }
                else
                {
                    Debug.LogWarning(
                        $"BasicPush.cs: Could not send network push event - NetworkClient or player tag not found.");
                }
            }

            Debug.Log($"BasicPush.cs: Pushed {targetCollider.name} with force {pushForce}.");
        }
    }
}