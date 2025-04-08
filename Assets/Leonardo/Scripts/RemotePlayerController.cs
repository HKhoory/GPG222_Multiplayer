using UnityEngine;

namespace Leonardo.Scripts
{
    /// <summary>
    /// Handles the movement interpolation for remote players to reduce jitter.
    /// </summary>
    public class RemotePlayerController : MonoBehaviour
    {
        [SerializeField] private float interpolationSpeed = 10f;
        
        private Vector3 _targetPosition;
        private Vector3 _targetForward;
        private bool _hasTarget;
        
        private void Awake()
        {
            _targetPosition = transform.position;
        }
        
        private void Update()
        {
            if (!_hasTarget) return;
            
            // Move the player smoothly towards the position.
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * interpolationSpeed);
            
            // If we have a target direction, smoothly rotate towards it.
            if (_targetForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_targetForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * interpolationSpeed);
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
            _targetPosition = position;
            _hasTarget = true;
            
            // Calculate the movement direction for rotation.
            Vector3 movementDirection = position - transform.position;
            if (movementDirection.magnitude > 0.1f)
            {
                // Remove vertical component for calculating forward direction
                //movementDirection.y = 0;
                //_targetForward = movementDirection.normalized;
            }
        }
    }
}