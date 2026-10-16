using System;
using UnityEngine;

namespace Trapple.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        public int MaximumHealth => maximumHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public bool IsVulnerable { get; private set; }
        public int VulnerabilityVersion { get; private set; }
        public event Action<int, int> HealthChanged;
        public event Action Died;

        private void Awake() => CurrentHealth = maximumHealth;

        public void SetVulnerable(bool value)
        {
            value &= !IsDead;
            if (value && !IsVulnerable) VulnerabilityVersion++;
            IsVulnerable = value;
        }

        public bool TryTakeDamage(int damage)
        {
            if (damage <= 0 || IsDead || !IsVulnerable) return false;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (IsDead) IsVulnerable = false;
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
            if (IsDead) Died?.Invoke();
            return true;
        }

        private void OnValidate() => maximumHealth = Mathf.Max(1, maximumHealth);
    }
}
