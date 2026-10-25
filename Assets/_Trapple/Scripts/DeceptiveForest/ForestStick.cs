using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class ForestStick : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        [SerializeField] private KeyCode throwKey = KeyCode.F;
        [SerializeField, Min(.5f)] private float pickupRadius = 1.65f;
        [SerializeField] private Vector2 throwVelocity = new Vector2(11f, 3f);
        [SerializeField, Min(1f)] private float recoveryDelay = 4f;
        private Rigidbody2D body;
        private Rigidbody2D playerBody;
        private Collider2D hitbox;
        private PlayerDeath death;
        private Vector3 spawn;
        private float flightTime;
        private float facing = 1f;
        public bool IsHeld { get; private set; }
        public bool IsFlying { get; private set; }
        public Sprite Icon => artwork != null ? artwork.sprite : null;
        public KeyCode PickupKey => pickupKey;
        public KeyCode ThrowKey => throwKey;
        public bool CanPickup => !IsHeld && (!IsFlying || flightTime > .35f) && CanInteract &&
            Vector2.Distance(player.transform.position, transform.position) <= pickupRadius;
        private bool CanInteract => player != null && death != null && !death.IsDead &&
            player.ControlEnabled && Time.timeScale > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<Collider2D>();
            spawn = transform.position;
            if (player != null)
            {
                death = player.GetComponent<PlayerDeath>();
                playerBody = player.GetComponent<Rigidbody2D>();
                Physics2D.IgnoreCollision(hitbox, player.GetComponent<Collider2D>());
            }
            Recover();
        }
        private void Update()
        {
            if (player != null)
            {
                float speed = playerBody.linearVelocity.x;
                if (Mathf.Abs(speed) > .1f) facing = Mathf.Sign(speed);
            }
            if (CanPickup && Input.GetKeyDown(pickupKey)) TryPickup();
            else if (IsHeld && CanInteract && Input.GetKeyDown(throwKey)) TryThrow();
            if (IsFlying)
            {
                flightTime += Time.deltaTime;
                if (flightTime >= recoveryDelay || body.position.y < -5f ||
                    Vector2.Distance(body.position, spawn) > 18f) Recover();
            }
        }
        private void LateUpdate()
        {
            if (!IsHeld || player == null) return;
            transform.position = player.transform.position + new Vector3(facing * .62f, .12f, 0);
            transform.rotation = Quaternion.Euler(0, 0, facing * 22f);
        }
        public bool TryPickup()
        {
            if (!CanPickup) return false;
            IsHeld = true; IsFlying = false;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            body.simulated = false;
            hitbox.enabled = false;
            return true;
        }
        public bool TryThrow()
        {
            if (!IsHeld || !CanInteract) return false;
            IsHeld = false; IsFlying = true; flightTime = 0;
            // Start inside the player's ignored collider so a nearby wall cannot be skipped.
            transform.position = player.transform.position + Vector3.up * .12f;
            body.position = transform.position; body.rotation = 0;
            body.bodyType = RigidbodyType2D.Dynamic; body.simulated = true;
            hitbox.isTrigger = false; hitbox.enabled = true;
            // Unity drops ignored pairs when a collider is disabled while held.
            Physics2D.IgnoreCollision(hitbox, player.GetComponent<Collider2D>());
            body.linearVelocity = new Vector2(throwVelocity.x * facing, throwVelocity.y);
            body.angularVelocity = -facing * 260f;
            return true;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsFlying) return;
            var target = collision.collider.GetComponentInParent<ForestStickTarget>();
            if (target != null && target.TryBreak(this)) Recover();
        }
        private void Recover()
        {
            IsHeld = false; IsFlying = false;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            body.bodyType = RigidbodyType2D.Kinematic; body.simulated = true;
            transform.SetPositionAndRotation(spawn, Quaternion.identity);
            body.position = spawn; body.rotation = 0;
            hitbox.isTrigger = true; hitbox.enabled = true;
        }
    }
}
