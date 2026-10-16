using System;
using System.Collections;
using Trapple.Core;
using UnityEngine;

namespace Trapple.Bosses
{
    public enum BossState { Waiting, Telegraphing, Attacking, Vulnerable, Dead }

    [RequireComponent(typeof(BossHealth), typeof(BossAttack), typeof(BossPhaseController))]
    public sealed class BossController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float initialDelay = 2f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.2f;
        [SerializeField, Min(0.5f)] private float vulnerabilityDuration = 3.2f;
        [SerializeField] private SpriteRenderer trunk;
        private BossHealth health;
        private BossAttack attacks;
        private BossPhaseController phases;
        private bool hasStarted;
        public BossState State { get; private set; }
        public event Action FightFinished;

        private void Awake()
        {
            health = GetComponent<BossHealth>();
            attacks = GetComponent<BossAttack>();
            phases = GetComponent<BossPhaseController>();
        }
        private void OnEnable()
        {
            health.Died += OnDied;
            if (hasStarted && !health.IsDead) StartCoroutine(Encounter());
        }
        private void Start()
        {
            // Other components' Awake/OnEnable order is unspecified. Wait for all Awake calls
            // before reading BossHealth, whose runtime HP is initialized there.
            hasStarted = true;
            if (!health.IsDead) StartCoroutine(Encounter());
        }
        private void OnDisable()
        {
            health.Died -= OnDied;
            StopAllCoroutines();
            attacks.Cancel();
            health.SetVulnerable(false);
        }

        private IEnumerator Encounter()
        {
            SetState(BossState.Waiting);
            yield return new WaitForSeconds(initialDelay);
            int attackIndex = 0;
            while (!health.IsDead)
            {
                SetState(BossState.Telegraphing);
                yield return attacks.Execute((BossAttackKind)(attackIndex++ % 3), phases.DurationMultiplier,
                    () => SetState(BossState.Attacking));
                health.SetVulnerable(true);
                SetState(BossState.Vulnerable);
                // Keep the interaction window readable even in phase two.
                yield return new WaitForSeconds(vulnerabilityDuration);
                health.SetVulnerable(false);
                SetState(BossState.Waiting);
                yield return new WaitForSeconds(attackInterval * phases.DurationMultiplier);
            }
        }

        private void OnDied()
        {
            StopAllCoroutines();
            attacks.Cancel();
            SetState(BossState.Dead);
            FightFinished?.Invoke();
            if (GameManager.Instance != null) GameManager.Instance.CompleteBossFight();
        }

        private void SetState(BossState state)
        {
            State = state;
            if (trunk == null) return;
            trunk.color = state == BossState.Dead ? new Color(0.22f, 0.24f, 0.22f) :
                state == BossState.Vulnerable ? new Color(1f, 0.74f, 0.25f) :
                phases.CurrentPhase == 2 ? new Color(0.64f, 0.25f, 0.15f) : new Color(0.4f, 0.26f, 0.16f);
        }
    }
}
