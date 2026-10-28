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
        private float remaining, age;
        public bool Armed { get; private set; }

        private void Awake()
        {
            view = GetComponent<SpriteRenderer>();
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
            kind = attackKind;
            velocity = movement;
            remaining = lifetime;
            age = 0f;
            gameObject.SetActive(true);
            body.position = position;
            body.rotation = 0f;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            // Sprite pixels do not define damage area; the explicit physical size always does.
            transform.localScale = Vector3.one;
            hitbox.size = size;
            hitbox.offset = Vector2.zero;
            view.sprite = sprite;
            view.drawMode = SpriteDrawMode.Simple;
            view.color = Color.white;
            if (sprite != null)
            {
                // Simple sprites need object scaling. Keep the collider in unscaled units.
                Vector2 dimensions = sprite.bounds.size;
                transform.localScale = new Vector3(size.x / Mathf.Max(.001f, dimensions.x),
                    size.y / Mathf.Max(.001f, dimensions.y), 1f);
                hitbox.size = dimensions * .78f;
            }
            visualScale = transform.localScale;
            Armed = true;
            hitbox.enabled = true;
        }

        private void Update()
        {
            if (owner == null || !owner.IsFighting) { Release(); return; }
            if (!owner.CanDamage) return;
            age += Time.deltaTime;
            remaining -= Time.deltaTime;
            if (kind == GuardianAttack.RootStrike)
                transform.localScale = new Vector3(visualScale.x,
                    visualScale.y * Mathf.SmoothStep(.15f, 1f, Mathf.Clamp01(age / .13f)), 1f);
            if (kind == GuardianAttack.LeafStorm) body.rotation += 130f * Time.deltaTime;
            float opacity = Mathf.Min(1f, remaining / .12f);
            view.color = new Color(1f, 1f, 1f, opacity);
            if (remaining <= 0f) Release();
        }

        private void FixedUpdate()
        {
            if (Armed && owner != null && owner.CanDamage)
                body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        }

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
