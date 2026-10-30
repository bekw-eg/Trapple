using UnityEngine;

namespace Trapple.Bosses
{
    /// <summary>Prewarmed cosmetic particles. Combat and hitboxes remain owned by the controller.</summary>
    [DisallowMultipleComponent]
    public sealed class GuardianAttackEffects : MonoBehaviour
    {
        [SerializeField] private Sprite dustSprite, debrisSprite, leafSprite;
        private ForestGuardianController owner;
        private Particle[] particles;
        private SpriteRenderer groundPulse;
        private Vector2 target;
        private float warningAge, warningDuration;
        private GuardianAttack warningAttack;
        private bool telegraphing;
        public int ActiveParticleCount { get; private set; }

        private sealed class Particle
        {
            public SpriteRenderer renderer;
            public Vector2 position, velocity, size;
            public Color color;
            public float age, lifetime, angle, spin, gravity;
            public bool dust;
        }

        private void Awake()
        {
            owner = GetComponent<ForestGuardianController>();
            particles = new Particle[32];
            for (int i = 0; i < particles.Length; i++)
                particles[i] = new Particle { renderer = CreateRenderer("Attack mote " + i, 18) };
            groundPulse = CreateRenderer("Breathing ground warning", 5);
            groundPulse.sprite = dustSprite;
        }

        private SpriteRenderer CreateRenderer(string label, int order)
        {
            var sprite = new GameObject(label, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sprite.transform.SetParent(transform, false);
            sprite.sortingOrder = order; sprite.enabled = false;
            return sprite;
        }

        public void Telegraph(GuardianAttack kind, Vector2 position, float seconds)
        {
            warningAttack = kind; target = position; warningAge = 0f; warningDuration = seconds;
            telegraphing = kind != GuardianAttack.LeafStorm;
        }

        public void Contact(GuardianAttack kind, Vector2 position)
        {
            telegraphing = false; groundPulse.enabled = false;
            bool leaves = kind == GuardianAttack.LeafStorm;
            int count = leaves ? 7 : kind == GuardianAttack.BranchSlam ? 15 : 11;
            for (int n = 0; n < count; n++)
            {
                bool dust = !leaves && n < 5;
                float side = n % 2 == 0 ? -1f : 1f;
                float fraction = (n + 1f) / count;
                Spawn(dust ? dustSprite : leaves ? leafSprite : debrisSprite,
                    position + new Vector2(leaves ? .1f : side * fraction * .6f, dust ? .04f : .12f),
                    leaves ? new Vector2(-2f - fraction * 3f, 1.1f + fraction * 2.2f) :
                        new Vector2(side * (1.1f + fraction * 3f), dust ? .35f : 2f + fraction * 3f),
                    dust ? new Vector2(1.2f, .35f) : leaves ? new Vector2(.32f, .2f) : new Vector2(.18f, .14f),
                    leaves ? new Color(.9f, .82f, .34f, .85f) : dust ? new Color(.64f, .47f, .22f, .55f) : new Color(.8f, .7f, .48f),
                    .38f + fraction * .35f, dust ? 0f : leaves ? 1.3f : 12f, side * 220f, dust);
            }
        }

        private void Spawn(Sprite sprite, Vector2 position, Vector2 velocity, Vector2 size, Color color,
            float lifetime, float gravity, float spin, bool dust)
        {
            if (sprite == null) return;
            foreach (var p in particles)
            {
                if (p.renderer.enabled) continue;
                p.position = position; p.velocity = velocity; p.size = size; p.color = color;
                p.age = 0f; p.lifetime = lifetime; p.gravity = gravity; p.spin = spin; p.angle = 0f; p.dust = dust;
                p.renderer.sprite = sprite; p.renderer.enabled = true; ActiveParticleCount++;
                Draw(p); return;
            }
        }

        private void Update()
        {
            if (owner == null || !owner.IsFighting) { Clear(); return; }
            if (!owner.CanDamage) return;
            float dt = Time.deltaTime;
            if (telegraphing)
            {
                warningAge += dt;
                float progress = Mathf.Clamp01(warningAge / Mathf.Max(.01f, warningDuration));
                groundPulse.enabled = true;
                groundPulse.transform.position = target;
                float width = warningAttack == GuardianAttack.BranchSlam ? 4.2f : 2.1f;
                SetSize(groundPulse, new Vector2(width * (.9f + .1f * Mathf.Sin(warningAge * 18f)), .24f));
                groundPulse.color = new Color(1f, .65f, .18f, .3f + .35f * progress);
            }
            foreach (var p in particles)
            {
                if (!p.renderer.enabled) continue;
                p.age += dt;
                if (p.age >= p.lifetime) { p.renderer.enabled = false; ActiveParticleCount--; continue; }
                p.velocity.y -= p.gravity * dt; p.position += p.velocity * dt; p.angle += p.spin * dt;
                Draw(p);
            }
        }

        private static void Draw(Particle p)
        {
            float life = p.age / p.lifetime;
            p.renderer.transform.position = p.position;
            p.renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.dust ? 0f : p.angle);
            SetSize(p.renderer, p.size * (p.dust ? 1f + life * 1.8f : 1f - life * .3f));
            p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, p.color.a * (1f - life) * (1f - life));
        }

        private static void SetSize(SpriteRenderer sprite, Vector2 size)
        {
            if (sprite.sprite == null) return;
            Vector2 bounds = sprite.sprite.bounds.size;
            sprite.transform.localScale = new Vector3(size.x / Mathf.Max(.001f, bounds.x), size.y / Mathf.Max(.001f, bounds.y), 1f);
        }

        public void Clear()
        {
            telegraphing = false; ActiveParticleCount = 0;
            if (groundPulse != null) groundPulse.enabled = false;
            if (particles == null) return;
            foreach (var p in particles) if (p.renderer != null) p.renderer.enabled = false;
        }

        private void OnDisable() => Clear();
    }
}
