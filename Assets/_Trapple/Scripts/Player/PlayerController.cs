using UnityEngine;

namespace Trapple.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] private float acceleration = 60f;
        [SerializeField, Min(0.1f)] private float deceleration = 70f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 2.4f;
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.08f;
        [SerializeField] private SpriteRenderer characterRenderer;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private float moveInput;
        private bool jumpRequested;

        public bool IsGrounded { get; private set; }
        public bool ControlEnabled { get; private set; } = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (characterRenderer == null)
                characterRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (!ControlEnabled)
                return;

            // This project uses the built-in Input Manager (Active Input Handling: Input Manager).
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            moveInput = (right ? 1f : 0f) - (left ? 1f : 0f);
            jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            if (!ControlEnabled)
                return;

            IsGrounded = CheckGrounded();
            Vector2 velocity = body.linearVelocity;
            float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : deceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, rate * Time.fixedDeltaTime);

            if (jumpRequested && IsGrounded)
            {
                velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * jumpHeight);
                IsGrounded = false;
            }

            // Consume every press, including airborne presses; holding Space cannot repeat a jump.
            jumpRequested = false;
            body.linearVelocity = velocity;

            if (characterRenderer != null && Mathf.Abs(moveInput) > 0.01f)
                characterRenderer.flipX = moveInput < 0f;
        }

        private bool CheckGrounded()
        {
            if (body.linearVelocity.y > 0.1f)
                return false;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayers);
            int count = body.Cast(Vector2.down, filter, groundHits, groundCheckDistance);
            for (int i = 0; i < count; i++)
            {
                // Walls and steep edges cannot grant another jump.
                if (groundHits[i].normal.y >= 0.65f)
                    return true;
            }
            return false;
        }

        public void SetControlEnabled(bool value)
        {
            ControlEnabled = value;
            moveInput = 0f;
            jumpRequested = false;
            if (!value && body != null)
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void OnDisable()
        {
            moveInput = 0f;
            jumpRequested = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                moveInput = 0f;
                jumpRequested = false;
            }
        }
    }
}
