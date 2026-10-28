using System;
using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    /// <summary>Small fixed sprite pool. Only startup instantiates; emission never touches gameplay RNG.</summary>
    [DisallowMultipleComponent]
    public sealed class AmbientParticleController : MonoBehaviour
    {
        public enum Kind { Leaf, Pollen, Smoke }
        [SerializeField] private Kind kind;
        [SerializeField] private SpriteRenderer particlePrefab;
        [SerializeField, Range(1, 16)] private int capacity = 4;
        [SerializeField] private Vector2 region = new Vector2(4, 1);
        [SerializeField] private Vector2 interval = new Vector2(3, 6);
        [SerializeField] private Vector2 lifetime = new Vector2(5, 8);
        [SerializeField] private Vector2 velocity = new Vector2(.09f, -.35f);
        [SerializeField, Min(0f)] private float flutter = .18f;
        [SerializeField, Range(0f, 1f)] private float opacity = .45f;
        [SerializeField] private int seed = 731;
        private struct Particle
        {
            public SpriteRenderer renderer;
            public Vector3 origin;
            public Vector3 scale;
            public float age, duration, phase;
            public bool active;
        }
        private Particle[] pool;
        private System.Random random;
        private float remaining;
        public int PoolSize => pool == null ? 0 : pool.Length;
        public int ActiveCount { get; private set; }

        public void Configure(Kind type, SpriteRenderer prefab, int limit, Vector2 area, Vector2 delays,
            Vector2 duration, Vector2 travel, float alpha, int randomSeed)
        {
            kind = type; particlePrefab = prefab; capacity = Mathf.Clamp(limit, 1, 16); region = area;
            interval = delays; lifetime = duration; velocity = travel; opacity = alpha; seed = randomSeed;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, Mathf.Max(min, max), (float)random.NextDouble());
        private float NextInterval() => Range(Mathf.Max(.1f, interval.x), Mathf.Max(.1f, interval.y));
        private void Awake()
        {
            if (particlePrefab == null) { enabled = false; return; }
            random = new System.Random(seed);
            pool = new Particle[Mathf.Clamp(capacity, 1, 16)];
            for (int i = 0; i < pool.Length; i++)
            {
                var sprite = Instantiate(particlePrefab, transform, false);
                sprite.gameObject.SetActive(false);
                pool[i].renderer = sprite;
                pool[i].scale = sprite.transform.localScale;
            }
            remaining = NextInterval();
        }

        private void Update()
        {
            if (pool == null) return;
            float dt = Time.deltaTime;
            remaining -= dt;
            if (remaining <= 0) { Emit(); remaining = NextInterval(); }
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].active) continue;
                ref Particle particle = ref pool[i];
                particle.age += dt;
                if (particle.age >= particle.duration)
                {
                    particle.active = false;
                    particle.renderer.gameObject.SetActive(false);
                    ActiveCount--;
                    continue;
                }
                float t = particle.age / particle.duration;
                float sway = (Mathf.Sin(particle.age * 1.5f + particle.phase) - Mathf.Sin(particle.phase)) * flutter;
                particle.renderer.transform.localPosition = particle.origin + (Vector3)(velocity * particle.age) + Vector3.right * sway;
                particle.renderer.transform.localRotation = Quaternion.Euler(0, 0, kind == Kind.Leaf ?
                    Mathf.Sin(particle.age * 2 + particle.phase) * 35 : 0);
                particle.renderer.transform.localScale = particle.scale * (kind == Kind.Smoke ? Mathf.Lerp(.7f, 1.8f, t) : 1);
                Color color = particle.renderer.color;
                color.a = opacity * Mathf.SmoothStep(0, 1, t / .18f) * Mathf.SmoothStep(0, 1, (1 - t) / .3f);
                particle.renderer.color = color;
            }
        }

        private void Emit()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].active) continue;
                ref Particle particle = ref pool[i];
                particle.age = 0;
                particle.duration = Range(Mathf.Max(.1f, lifetime.x), Mathf.Max(.1f, lifetime.y));
                particle.phase = Range(0, Mathf.PI * 2);
                particle.origin = new Vector2(Range(-region.x * .5f, region.x * .5f), Range(-region.y * .5f, region.y * .5f));
                particle.active = true;
                particle.renderer.gameObject.SetActive(true);
                ActiveCount++;
                return;
            }
        }

        private void OnDisable()
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i].active = false;
                if (pool[i].renderer != null) pool[i].renderer.gameObject.SetActive(false);
            }
            ActiveCount = 0;
            remaining = NextInterval();
        }
    }
}
