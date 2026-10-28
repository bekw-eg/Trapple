using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    /// <summary>Built-in compatible painted-light halo; no Light2D dependency or global recoloring.</summary>
    [RequireComponent(typeof(SpriteRenderer)), DisallowMultipleComponent]
    public sealed class LightFlicker2D : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float intensity = .13f;
        [SerializeField, Range(0f, .3f)] private float variation = .08f;
        [SerializeField, Min(0f)] private float speed = .75f;
        [SerializeField] private float phase = 1.7f;
        private SpriteRenderer halo;
        private Color tint;
        private float elapsed;
        private void Awake() { halo = GetComponent<SpriteRenderer>(); tint = halo.color; }
        private void OnEnable() { if (halo != null) halo.enabled = true; }
        private void Update()
        {
            elapsed += Time.deltaTime * speed;
            float wave = Mathf.Sin(elapsed + phase) * .65f + Mathf.Sin(elapsed * .63f + phase * 2) * .35f;
            Color color = tint;
            color.a = Mathf.Clamp01(intensity * (1f + wave * variation));
            halo.color = color;
        }
        private void OnDisable() { if (halo != null) halo.enabled = false; }
    }
}
