using UnityEngine;

namespace Leonardo.Scripts.Controller
{
    /// <summary>
    /// Handles the movement interpolation for remote players to reduce jitter.
    /// Also calculates rotation based on movement direction.
    /// </summary>
    public class RemotePlayerController : MonoBehaviour
    {
        [SerializeField] private float interpolationSpeed = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        
        // Minimum movement to consider for ROTATION.
        [SerializeField] private float minimumMovementThreshold = 0.05f;
        
        // "If you see something assigned to -1 you know something is wrong" - Mustafa.
        [SerializeField] private int playerTag = -1;
        public int PlayerTag { get { return playerTag; } }
        
        private Vector3 _targetPosition;
        private Vector3 _previousPosition;
        private bool _hasTarget;
        private bool _hasMovedAtLeastOnce;
        
        private void Awake()
        {
            _targetPosition = transform.position;
            _previousPosition = transform.position;
        }
        
        private void Update()
        {
            if (!_hasTarget) return;
            Vector3 positionBeforeMovement = transform.position;
            
            // Smoothly move towards the target position.
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * interpolationSpeed);
            Vector3 actualMovement = transform.position - positionBeforeMovement;
            if (actualMovement.magnitude > minimumMovementThreshold)
            {
                Vector3 movementDirection = actualMovement;
                // THIS IS TO IGNORE VERTICAL MOVEMENT (to not affect vertical rotation). I prefer with it cause it looks silly.
                //movementDirection.y = 0;
                
                if (movementDirection.magnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movementDirection.normalized);
                    // Apply rotation with smooth interpolation.
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            
            // If we're very close to the target, consider it reached.
            if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
            {
                _hasTarget = false;
            }
        }
        
        /// <summary>
        /// Sets a new position target for this remote player.
        /// </summary>
        /// <param name="position">The target position.</param>
        public void SetPositionTarget(Vector3 position)
        {
            if (_hasMovedAtLeastOnce)
            {
                _previousPosition = _targetPosition;
            }
            else
            {
                _previousPosition = transform.position;
                _hasMovedAtLeastOnce = true;
            }
            
            _targetPosition = position;
            _hasTarget = true;
            
            Vector3 movementVector = _targetPosition - _previousPosition;
            if (movementVector.magnitude > minimumMovementThreshold)
            {
                // THIS IS TO IGNORE VERTICAL MOVEMENT (to not affect vertical rotation). I prefer with it cause it looks silly.
                //movementVector.y = 0;
                
                if (movementVector.magnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(movementVector.normalized);
                }
            }
        }
        
        /// <summary>
        /// Sets the player tag for this remote player.
        /// </summary>
        /// <param name="tag">The player's network tag.</param>
        public void SetPlayerTag(int tag)
        {
            playerTag = tag;
        }
    }
}