using System;
using Trapple.Bosses;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class StickProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private Vector2 visualSize = new Vector2(1.25f, .65f);
        [SerializeField, Min(.1f)] private float lifetime = 4f;
        private Rigidbody2D body;
        private Collider2D hitbox;
        private Transform owner;
        private Action<StickProjectile> release;
        private Vector3 initialVisualScale;
        private Color initialColor;
        private int damage;
        private float elapsed;
        private bool impacted;
        private bool initialized;
        public bool HasImpacted => impacted;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            if (artwork == null) artwork = GetComponentInChildren<SpriteRenderer>();
            // Physics uses world units; source texture pixels must never become the projectile's scale.
            transform.localScale = Vector3.one;
            if (artwork != null && artwork.sprite != null)
            {
                // Also support older prefabs with a renderer on the Rigidbody root.
                if (artwork.transform == transform)
                {
                    var visual = new GameObject("Projectile artwork", typeof(SpriteRenderer));
                    visual.transform.SetParent(transform, false);
                    var view = visual.GetComponent<SpriteRenderer>();
                    view.sprite = artwork.sprite; view.color = artwork.color;
                    view.sharedMaterial = artwork.sharedMaterial;
                    view.sortingLayerID = artwork.sortingLayerID; view.sortingOrder = artwork.sortingOrder;
                    artwork.enabled = false; artwork = view;
                }
                Vector2 bounds = artwork.sprite.bounds.size;
                artwork.transform.localScale = new Vector3(visualSize.x / Mathf.Max(.001f, bounds.x),
                    visualSize.y / Mathf.Max(.001f, bounds.y), 1);
            }
            initialVisualScale = artwork != null ? artwork.transform.localScale : Vector3.one;
            initialColor = artwork != null ? artwork.color : Color.white;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Launch(Transform thrower, Vector2 position, Vector2 velocity, float gravity, int hitDamage,
            Action<StickProjectile> returnToPool = null)
        {
            Initialize();
            owner = thrower; release = returnToPool; damage = Mathf.Max(1, hitDamage);
            elapsed = 0; impacted = false;
            transform.localScale = Vector3.one;
            if (artwork != null) { artwork.transform.localScale = initialVisualScale; artwork.color = initialColor; }
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.position = position; body.rotation = 0;
            body.gravityScale = Mathf.Max(0, gravity);
            body.simulated = true; hitbox.enabled = true; hitbox.isTrigger = false;
            gameObject.SetActive(true);
            if (thrower != null)
                foreach (var collider in thrower.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(hitbox, collider);
            body.linearVelocity = velocity;
            body.angularVelocity = velocity.x < 0 ? 480 : -480;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (!impacted)
            {
                if (elapsed >= lifetime) ReturnToPool();
                return;
            }
            float t = Mathf.Clamp01(elapsed / .14f);
            if (artwork != null)
            {
                artwork.transform.localScale = initialVisualScale * Mathf.Lerp(1f, 1.12f, t);
                artwork.color = new Color(1f, .85f, .45f, 1f - t);
            }
            if (t >= 1f) ReturnToPool();
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        public bool TryHit(Collider2D other)
        {
            if (impacted || other == null || !gameObject.activeInHierarchy ||
                (owner != null && other.transform.IsChildOf(owner))) return false;
            // Pooled hazards live under the guardian for cleanup, but are not its damageable body.
            if (other.GetComponentInParent<ForestGuardianHazard>() != null || other.GetComponentInParent<BossHazard>() != null) return false;
            var health = other.GetComponentInParent<BossHealth>();
            if (health == null && other.isTrigger) return false;
            // Latch BEFORE notifying health: death events may clear this pool synchronously.
            impacted = true; elapsed = 0;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            body.simulated = false; hitbox.enabled = false;
            if (health != null) health.TryTakeDamage(damage);
            return true;
        }

        public void ReturnToPool()
        {
            if (!gameObject.activeSelf) return;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            body.simulated = false; hitbox.enabled = false;
            gameObject.SetActive(false);
            release?.Invoke(this);
        }
    }
}
