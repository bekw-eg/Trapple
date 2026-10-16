using UnityEngine;

namespace Trapple.Bosses
{
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossPhaseController : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoThreshold = 0.5f;
        [SerializeField, Range(0.25f, 1f)] private float phaseTwoDurationMultiplier = 0.65f;
        private BossHealth health;
        public int CurrentPhase { get; private set; } = 1;
        public float DurationMultiplier => CurrentPhase == 2 ? phaseTwoDurationMultiplier : 1f;

        private void Awake() => health = GetComponent<BossHealth>();
        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHealth, health.MaximumHealth);
        }
        private void OnDisable() => health.HealthChanged -= OnHealthChanged;
        private void OnHealthChanged(int current, int maximum)
        {
            // Exactly 50% remains phase one; phase two starts strictly below the threshold.
            if (current > 0 && (float)current / maximum < phaseTwoThreshold)
                CurrentPhase = 2;
        }
    }
}
