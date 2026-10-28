using UnityEngine;

namespace Trapple.Player
{
    public enum PlayerVisualState { Idle, RunRight, RunLeft, Rising, Falling, Landing, WalkRight, WalkLeft }

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

        // The workshop owns movement while input/physics are suspended; reuse the actual run frames.
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
            else if (Mathf.Abs(velocity.x) > movingThreshold)
            {
                // Cinematic movement still uses the workshop's existing run presentation.
                bool running = cinematicMotion || controller.IsSprinting;
                next = running ? (facingLeft ? PlayerVisualState.RunLeft : PlayerVisualState.RunRight) :
                    (facingLeft ? PlayerVisualState.WalkLeft : PlayerVisualState.WalkRight);
            }
            else next = PlayerVisualState.Idle;
            bool changingDirection = (IsRunning(State) && IsRunning(next)) || (IsWalking(State) && IsWalking(next));
            if (next != State) { State = next; if (!changingDirection) elapsed = 0f; }
            else elapsed += cinematicMotion ? Time.unscaledDeltaTime : Time.deltaTime;
            wasGrounded = grounded;
            bool authoredDirection = IsRunning(State) || IsWalking(State);
            character.flipX = !authoredDirection && facingLeft;
            switch (State)
            {
                case PlayerVisualState.RunLeft: Cycle(animations.runLeftClip, animations.walkLeft, animations.walkFramesPerSecond); break;
                case PlayerVisualState.RunRight: Cycle(animations.runRightClip, animations.walkRight, animations.walkFramesPerSecond); break;
                case PlayerVisualState.WalkLeft:
                    Walk(animations.walkingLeft, animations.walkLeft); break;
                case PlayerVisualState.WalkRight:
                    Walk(animations.walkingRight, animations.walkRight); break;
                case PlayerVisualState.Rising:
                    FrameIndex = elapsed < animations.takeoffDuration ? 0 : 1;
                    character.sprite = animations.jump[FrameIndex];
                    break;
                case PlayerVisualState.Falling: FrameIndex = 2; character.sprite = animations.jump[2]; break;
                case PlayerVisualState.Landing: FrameIndex = 3; character.sprite = animations.jump[3]; break;
                default: Cycle(animations.idleClip, animations.idle, animations.idleFramesPerSecond); break;
            }
        }

        private static bool IsRunning(PlayerVisualState state) =>
            state == PlayerVisualState.RunRight || state == PlayerVisualState.RunLeft;

        private static bool IsWalking(PlayerVisualState state) =>
            state == PlayerVisualState.WalkRight || state == PlayerVisualState.WalkLeft;

        private void Walk(Sprite[] frames, Sprite[] fallback)
        {
            // Older/custom animation sets remain usable until their walking frames are assigned.
            Cycle(null, frames != null && frames.Length > 0 ? frames : fallback, animations.walkingFramesPerSecond);
        }

        private void Cycle(AnimationClip clip, Sprite[] frames, float framesPerSecond)
        {
            float rate = clip != null ? clip.frameRate : framesPerSecond;
            FrameIndex = Mathf.FloorToInt(elapsed * rate) % frames.Length;
            // Use the existing frame player: SampleAnimation does not bind sprite
            // curves on this character without adding a second animation component.
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
