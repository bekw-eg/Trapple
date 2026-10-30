using Trapple.Player;
using UnityEngine;

namespace Trapple.Bosses
{
    /// <summary>A pooled, single-hit root, branch or travelling leaf. Never damages during its warning.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public sealed class ForestGuardianHazard : MonoBehaviour
    {
        private SpriteRenderer view;
        private BoxCollider2D hitbox;
        private Rigidbody2D body;
        private ForestGuardianController owner;
        private GuardianAttack kind;
        private Vector2 velocity;
        private Vector3 visualScale;
        private Vector2 physicalSize;
        private float remaining, age;
        private float gravity, ground;
        private bool bouncing, rolling;
        private int bounceCount;
        public bool Armed { get; private set; }
        public GuardianAttack Kind => kind;
        public int BounceCount => bounceCount;
        public Vector2 Velocity => velocity;
        public int LaunchId { get; private set; }

        private void Awake()
        {
            // Keep the physics root unscaled: flutter, stretch and rotation belong to the artwork.
            var template = GetComponent<SpriteRenderer>();
            template.enabled = false;
            view = new GameObject("Animated hazard artwork", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            view.transform.SetParent(transform, false);
            view.sharedMaterial = template.sharedMaterial;
            hitbox = GetComponent<BoxCollider2D>();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            hitbox.isTrigger = true;
            hitbox.enabled = false;
            view.sortingOrder = 16;
        }

        public void Launch(ForestGuardianController source, Sprite sprite, Vector2 position, Vector2 size,
            Vector2 movement, float lifetime, GuardianAttack attackKind)
        {
            owner = source;
            LaunchId++;
            kind = attackKind;
            velocity = movement;
            remaining = lifetime;
            age = 0f;
            gravity = 0f; ground = float.NegativeInfinity; bouncing = rolling = false; bounceCount = 0;
            gameObject.SetActive(true);
            body.position = position;
            body.rotation = 0f;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            // Sprite pixels do not define damage area; the explicit physical size always does.
            transform.localScale = Vector3.one;
            physicalSize = size;
            hitbox.size = size * .78f;
            hitbox.offset = Vector2.zero;
            view.sprite = sprite;
            view.drawMode = SpriteDrawMode.Simple;
            view.color = Color.white;
            if (sprite != null)
            {
                Vector2 dimensions = sprite.bounds.size;
                // The branch sheet is vertical; turn its artwork into a grounded horizontal sweep.
                bool sideways = kind == GuardianAttack.BranchSlam;
                visualScale = new Vector3((sideways ? size.y : size.x) / Mathf.Max(.001f, dimensions.x),
                    (sideways ? size.x : size.y) / Mathf.Max(.001f, dimensions.y), 1f);
            }
            else visualScale = new Vector3(size.x, size.y, 1f);
            AnimateArtwork();
            Armed = true;
            hitbox.enabled = true;
        }

        private void Update()
        {
            if (owner == null || !owner.IsFighting) { Release(); return; }
            if (!owner.CanDamage) return;
            age += Time.deltaTime;
            remaining -= Time.deltaTime;
            AnimateArtwork();
            float opacity = Mathf.Min(1f, remaining / .12f);
            view.color = new Color(1f, 1f, 1f, opacity);
            if (remaining <= 0f) Release();
        }

        private void AnimateArtwork()
        {
            Vector3 scale = visualScale;
            Vector3 offset = Vector3.zero;
            float angle = 0f;
            if (kind == GuardianAttack.RootStrike || kind == GuardianAttack.RootWave)
            {
                float growth = Mathf.SmoothStep(.08f, 1f, Mathf.Clamp01(age / .14f));
                growth *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(remaining / .16f));
                scale.y *= growth;
                // Anchor the base in the soil while the damaging part rises with the visible root.
                offset.y = physicalSize.y * (growth - 1f) * .5f;
                hitbox.size = new Vector2(physicalSize.x, physicalSize.y * growth) * .78f;
                hitbox.offset = offset;
                angle = Mathf.Sin(age * 32f) * 2f * Mathf.Exp(-age * 8f);
            }
            else if (kind == GuardianAttack.BranchSlam)
            {
                angle = 90f + Mathf.Sin(age * 40f) * 3f * Mathf.Exp(-age * 12f);
                offset.y = Mathf.Abs(Mathf.Sin(age * 28f)) * .08f * Mathf.Exp(-age * 9f);
            }
            else if (kind == GuardianAttack.BranchRain)
                angle = Mathf.Sin(age * 11f) * 4f;
            else if (kind == GuardianAttack.AppleBarrage)
                angle = age * 280f;
            else
            {
                angle = -20f + age * 250f;
                scale.x *= .85f + Mathf.Sin(age * 24f) * .15f;
                offset.y = Mathf.Sin(age * 20f) * .045f;
            }
            view.transform.localPosition = offset;
            view.transform.localScale = scale;
            view.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FixedUpdate()
        {
            if (!Armed || owner == null || !owner.CanDamage) return;
            float dt = Time.fixedDeltaTime;
            if (!rolling) velocity.y -= gravity * dt;
            Vector2 next = body.position + velocity * dt;
            if (next.y - physicalSize.y * .5f <= ground && velocity.y <= 0f && !rolling)
            {
                next.y = ground + physicalSize.y * .5f;
                owner.SpecialContact(kind, new Vector2(next.x, ground + .05f));
                if (!bouncing) { Release(); return; }
                bounceCount++;
                velocity.y = bounceCount == 1 ? 6f : bounceCount == 2 ? 3.8f : 0f;
                rolling = bounceCount >= 3;
            }
            body.MovePosition(next);
        }

        public void SetBallistics(float acceleration, float floor, bool bounce)
        { gravity = acceleration; ground = floor; bouncing = bounce; }

        private void OnTriggerEnter2D(Collider2D other) => Hit(other);
        private void OnTriggerStay2D(Collider2D other) => Hit(other);
        private void Hit(Collider2D other)
        {
            if (!Armed || owner == null || !owner.CanDamage) return;
            var target = other.GetComponentInParent<PlayerDeath>();
            if (target == null || target != owner.Player || target.IsDead) return;
            Armed = false;
            hitbox.enabled = false;
            target.Die();
        }

        public void Release()
        {
            Armed = false;
            if (hitbox != null) hitbox.enabled = false;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Armed = false;
            if (hitbox != null) hitbox.enabled = false;
        }
    }
}
