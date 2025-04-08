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

        [Header("- Ground Check")] [SerializeField]
        private Transform groundCheck;

        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;

        [Header("- References")] [SerializeField]
        private Rigidbody rb;

        private Vector3 _movement;
        private float _rotation;
        private bool _isGrounded;
        private bool _isLocalPlayer = true;

        #region Unity Methods

        private void Awake()
        {
            InitializeComponents();
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
            
            if (movementDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
        
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            if (!_isLocalPlayer) return;

            // Apply movement.
            Vector3 velocity = new Vector3(_movement.x, rb.velocity.y, _movement.z);
            rb.velocity = velocity;
        }
        
        #endregion
        
        #region Script Specific Methods

        /// <summary>
        /// JUMP!
        /// </summary>
        private void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        /// <summary>
        /// Initializes the components of this script.
        /// </summary>
        private void InitializeComponents()
        {
            // Get components if not assigned.
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            // Create ground check if not assigned (DUMMY PROOFING).
            if (groundCheck == null)
            {
                GameObject check = new GameObject("GroundCheck");
                check.transform.parent = transform;
                check.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = check.transform;
            }

            // Dummy proofing (Im setting the rigidbody rotation freeze).
            if (rb != null)
            {
                // For remote players, use kinematic to let network positioning control them.
                rb.isKinematic = !_isLocalPlayer;
                
                // Only apply gravity to local player.
                rb.useGravity = _isLocalPlayer;
                
                // Prevent rigidbody from rotating the player.
                rb.freezeRotation = true;
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
            
            if (rb != null)
            {
                rb.isKinematic = !_isLocalPlayer;
                rb.useGravity = _isLocalPlayer;
            }
        }

        #endregion
    }
}