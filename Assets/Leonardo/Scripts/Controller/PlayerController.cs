using UnityEngine;

namespace Leonardo.Scripts.Controller
{
    /// <summary>
    /// This takes care of the player control for the client.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("- Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float jumpForce = 5f;
        
        [Header("- Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        
        [Header("- References")]
        [SerializeField] private Rigidbody rb;
        
        // Flags for tracking.
        private bool _isGrounded;
        private Vector3 _movement;
        private float _rotation;
        private bool _isLocalPlayer = true;
        
        private void Awake()
        {
            // Get components if not assigned.
            if (rb == null)
                rb = GetComponent<Rigidbody>();
                
            // Create ground check if not assigned.
            if (groundCheck == null)
            {
                GameObject check = new GameObject("GroundCheck");
                check.transform.parent = transform;
                check.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = check.transform;
            }
            
            // Dummy proofing (Im setting the rigidbody rotation freeze)
            if (rb != null)
            {
                rb.freezeRotation = true; // Prevent rigidbody from rotating the player.
            }
        }
        
        private void Update()
        {
            if (!_isLocalPlayer) return;
            
            // Check if player is grounded.
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            // Get input
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            
            // Calculate movement.
            _movement = transform.forward * verticalInput + transform.right * horizontalInput;
            _movement = Vector3.ClampMagnitude(_movement, 1f) * moveSpeed;
            
            // Handle rotation input.
            _rotation = 0;
            if (Input.GetKey(KeyCode.Q))
                _rotation = -1;
            else if (Input.GetKey(KeyCode.E))
                _rotation = 1;
                
            // Handle jump.
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                Jump();
            }
            
            // Apply rotation.
            transform.Rotate(Vector3.up, _rotation * rotationSpeed * Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            if (!_isLocalPlayer) return;
            
            // Apply movement.
            Vector3 velocity = new Vector3(_movement.x, rb.velocity.y, _movement.z);
            rb.velocity = velocity;
        }
        
        private void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        public void SetLocalplayer(bool isLocalPlayer)
        {
            _isLocalPlayer = isLocalPlayer;

            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material.color = isLocalPlayer ? Color.blue : Color.red;
            }
        }
    }
}