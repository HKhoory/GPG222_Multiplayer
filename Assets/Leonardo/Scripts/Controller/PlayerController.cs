using Leonardo.Scripts.Abilities;
using Leonardo.Scripts.Effects;
using UnityEngine;

namespace Leonardo.Scripts.Controller
{
    /// <summary>
    /// This takes care of the player control for the client.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("- Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float jumpForce = 5f;
        
        // For the rotation.
        [SerializeField] private float minimumMovementThreshold = 0.05f;

        [Header("- Ground Check")] [SerializeField]
        private Transform groundCheck;

        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        
        private Rigidbody _rb;
        private AbilityManager _abilityManager;

        private Vector3 _movement;
        private Vector3 _previousPosition;
        private bool _isGrounded;
        private bool _isLocalPlayer = true;
        
        public bool IsLocalPlayer => _isLocalPlayer;

        #region Unity Methods

        private void Awake()
        {
            InitializeComponents();
            _previousPosition = transform.position;
        }

        private void Update()
        {
            // Only handle input for local player.
            if (!_isLocalPlayer) 
            {
                return; 
            }
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            
            Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
            
            _movement = movementDirection * moveSpeed;
            
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            if (!_isLocalPlayer) return;

            // Apply movement.
            Vector3 velocity = new Vector3(_movement.x, _rb.velocity.y, _movement.z);
            _rb.velocity = velocity;
        }
        
        private void LateUpdate()
        {
            if (!_isLocalPlayer) return;
            Vector3 movementVector = transform.position - _previousPosition;
            
            // Only rotate if we've moved a significant amount.
            if (movementVector.magnitude > minimumMovementThreshold)
            {
                if (movementVector.magnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movementVector.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            
            _previousPosition = transform.position;
        }
        
        #endregion
        
        #region Script Specific Methods

        /// <summary>
        /// JUMP!
        /// </summary>
        private void Jump()
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        /// <summary>
        /// Initializes the components of this script.
        /// </summary>
        private void InitializeComponents()
        {
            // Get components if not assigned.
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            _abilityManager = GetComponent<AbilityManager>();
            if (_abilityManager == null && _isLocalPlayer)
            {
                _abilityManager = gameObject.AddComponent<AbilityManager>(); // more dummy proofing lol.
            }
            
            // Create ground check if not assigned (DUMMY PROOFING).
            if (groundCheck == null)
            {
                GameObject check = new GameObject("GroundCheck");
                check.transform.parent = transform;
                check.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = check.transform;
            }

            // Dummy proofing (Im setting the rigidbody rotation freeze).
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.freezeRotation = true;
            }
        }

        /// <summary>
        /// Sets whether this player is controlled locally or by the network.
        /// </summary>
        /// <param name="isLocalPlayer">Is this the local player?</param>
        public void SetLocalplayer(bool isLocalPlayer)
        {
            _isLocalPlayer = isLocalPlayer;

            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material.color = isLocalPlayer ? Color.blue : Color.red;
            }
            
            if (_rb != null)
            {
                //_rb.isKinematic = !_isLocalPlayer;
                //_rb.useGravity = _isLocalPlayer;
            }
        }
        
        public void ApplyPushForce(Vector3 force, string effectName)
        {
            if (!_isLocalPlayer) return;
    
            if (!string.IsNullOrEmpty(effectName) && EffectManager.Instance != null)
            {
                EffectManager.Instance.PlayEffect(effectName, transform.position, transform.rotation);
            }
    
            if (_rb != null)
            {
                _rb.AddForce(force, ForceMode.Impulse);
                //Debug.Log($"PlayerController.cs: Applied push force {force} to local player");
            }
        }
        
        /// <summary>
        /// Get the ability manager script from the player.
        /// </summary>
        /// <returns>The ability manager attached to this player.</returns>
        public AbilityManager GetAbilityManager()
        {
            return _abilityManager;
        }

        #endregion
    }
}