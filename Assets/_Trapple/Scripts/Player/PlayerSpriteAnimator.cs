using UnityEngine;

namespace Trapple.Player
{
    public enum PlayerVisualState { Idle, WalkRight, WalkLeft, Rising, Falling, Landing }

    // Presentation only. The existing controller remains responsible for input and physics.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public sealed class PlayerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationSet animations;
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [SerializeField, Min(0.01f)] private float movingThreshold = 0.15f;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private PlayerController controller;
        private SpriteRenderer character;
        private Sprite appearanceOverride;
        private ContactFilter2D groundFilter;
        private float elapsed;
        private bool facingLeft, wasGrounded;
        private bool cinematicMotion;
        private Vector2 cinematicVelocity;
        public PlayerVisualState State { get; private set; }
        public int FrameIndex { get; private set; }
        public PlayerAnimationSet Animations => animations;

        // The workshop owns movement while input/physics are suspended; reuse the actual walk frames.
        public void SetCinematicMotion(bool active, Vector2 velocity)
        { cinematicMotion=active; cinematicVelocity=velocity; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
            character = GetComponent<SpriteRenderer>();
            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(groundLayers);
        }

        public void SetAppearance(Sprite sprite)
        {
            appearanceOverride = animations != null && animations.idle != null && animations.idle.Length > 0 &&
                sprite == animations.idle[0] ? null : sprite;
            if (sprite != null && character != null) character.sprite = sprite;
        }

        private void LateUpdate()
        {
            if (animations == null || !animations.IsValid || character == null || body == null) return;
            if (appearanceOverride != null)
            {
                character.sprite = appearanceOverride;
                return;
            }
            if (!body.simulated && !cinematicMotion) return;
            Vector2 velocity = cinematicMotion ? cinematicVelocity : body.linearVelocity;
            if (Mathf.Abs(velocity.x) > movingThreshold) facingLeft = velocity.x < 0f;
            bool grounded = cinematicMotion || IsGrounded();
            PlayerVisualState next;
            if (!grounded) next = velocity.y > 0.05f ? PlayerVisualState.Rising : PlayerVisualState.Falling;
            else if (!wasGrounded || (State == PlayerVisualState.Landing && elapsed < animations.landingDuration)) next = PlayerVisualState.Landing;
            else if ((controller.ControlEnabled || cinematicMotion) && Mathf.Abs(velocity.x) > movingThreshold) next = facingLeft ? PlayerVisualState.WalkLeft : PlayerVisualState.WalkRight;
            else next = PlayerVisualState.Idle;
            if (next != State) { State = next; elapsed = 0f; }
            else elapsed += cinematicMotion ? Time.unscaledDeltaTime : Time.deltaTime;
            wasGrounded = grounded;
            bool authoredLeft = State == PlayerVisualState.WalkLeft;
            character.flipX = !authoredLeft && State != PlayerVisualState.WalkRight && facingLeft;
            switch (State)
            {
                case PlayerVisualState.WalkLeft: Cycle(animations.walkLeft, animations.walkFramesPerSecond); break;
                case PlayerVisualState.WalkRight: Cycle(animations.walkRight, animations.walkFramesPerSecond); break;
                case PlayerVisualState.Rising:
                    FrameIndex = elapsed < animations.takeoffDuration ? 0 : 1;
                    character.sprite = animations.jump[FrameIndex];
                    break;
                case PlayerVisualState.Falling: FrameIndex = 2; character.sprite = animations.jump[2]; break;
                case PlayerVisualState.Landing: FrameIndex = 3; character.sprite = animations.jump[3]; break;
                default: Cycle(animations.idle, animations.idleFramesPerSecond); break;
            }
        }

        private void Cycle(Sprite[] frames, float framesPerSecond)
        {
            FrameIndex = Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length;
            character.sprite = frames[FrameIndex];
        }

        private bool IsGrounded()
        {
            if (body.linearVelocity.y > 0.1f) return false;
            int count = body.Cast(Vector2.down, groundFilter, hits, 0.08f);
            for (int i = 0; i < count; i++) if (hits[i].normal.y >= 0.65f) return true;
            return false;
        }
    }
}
