using System;
using System.Collections.Generic;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Bosses
{
    public enum GuardianState { Inactive, Intro, Idle, PreparingAttack, Attacking, Recovering, Stunned, Defeated, Retreating }
    public enum GuardianAttack { RootStrike, BranchSlam, LeafStorm }

    /// <summary>The forest encounter owns the intro, survival clock and completion. This component owns combat only.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossHealth), typeof(ForestGuardianAnimation))]
    public sealed class ForestGuardianController : MonoBehaviour
    {
        [SerializeField] private ForestGuardianAnimation animationRig;
        [SerializeField] private ForestGuardianHazard hazardPrefab;
        [SerializeField] private SpriteRenderer warning;
        [SerializeField] private Sprite rootSprite, branchSprite, leafSprite;
        [SerializeField, Min(1f)] private float minimumAttackInterval = 1.5f;
        [SerializeField, Min(1f)] private float maximumAttackInterval = 3f;
        [SerializeField, Min(.65f)] private float warningDuration = 1.05f;
        [SerializeField, Min(.1f)] private float rootDuration = .65f;
        [SerializeField, Min(.1f)] private float slamDuration = .45f;
        [SerializeField, Min(1f)] private float leafSpeed = 6f;
        [SerializeField, Range(1, 5)] private int leafWaves = 3;
        [SerializeField, Min(.2f)] private float leafWaveInterval = .4f;
        [SerializeField, Min(.1f)] private float hurtDuration = .22f;
        [SerializeField] private bool survivalOnly;
        [SerializeField, Range(0f, .35f)] private float impactDelay;
        [SerializeField] private GuardianAttackEffects attackEffects;

        private readonly List<ForestGuardianHazard> pool = new List<ForestGuardianHazard>();
        private PlayerDeath player;
        private BossHealth health;
        private Vector2 arena;
        private float floorY, remaining, attackX, leafDelay, impactRemaining;
        private bool impactPending;
        private int attackIndex, wavesSent;
        private bool initialized, began, resolved;
        public GuardianState State { get; private set; } = GuardianState.Inactive;
        public GuardianAttack CurrentAttack { get; private set; }
        public BossHealth Health => health;
        public bool SurvivalOnly => survivalOnly;
        public bool IsTelegraphing => State == GuardianState.PreparingAttack || impactPending;
        public PlayerDeath Player => player;
        public bool IsFighting => began && !resolved && player != null && !player.IsDead &&
            State != GuardianState.Inactive && State != GuardianState.Intro;
        public bool CanDamage => IsFighting && Time.timeScale > 0f &&
            (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
        public int ActiveHazardCount
        {
            get
            {
                int count = 0;
                foreach (var hazard in pool) if (hazard != null && hazard.gameObject.activeSelf) count++;
                return count;
            }
        }
        public event Action<bool> Resolved;
        public event Action<float> Impact;

        private void Awake()
        {
            health = GetComponent<BossHealth>();
            if (animationRig == null) animationRig = GetComponent<ForestGuardianAnimation>();
            if (attackEffects == null) attackEffects = GetComponent<GuardianAttackEffects>();
            if (warning != null) warning.enabled = false;
        }

        private void OnEnable()
        {
            health.HealthChanged += HealthChanged;
            health.Died += OnDied;
        }

        public void Initialize(PlayerDeath target, Vector2 arenaBounds, float groundY)
        {
            if (began || resolved) return;
            player = target;
            arena = new Vector2(Mathf.Min(arenaBounds.x, arenaBounds.y), Mathf.Max(arenaBounds.x, arenaBounds.y));
            floorY = groundY;
            initialized = player != null;
            // Allocation happens once before combat. At most three leaves are live concurrently.
            if (hazardPrefab != null && pool.Count == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    var hazard = Instantiate(hazardPrefab, transform);
                    hazard.name = "Guardian hazard " + i;
                    hazard.gameObject.SetActive(false);
                    pool.Add(hazard);
                }
            }
        }

        public void BeginIntro()
        {
            if (!initialized || began || resolved || player.IsDead) return;
            health.SetVulnerable(false);
            SetState(GuardianState.Intro);
        }

        public void BeginFight()
        {
            if (!initialized || began || resolved || player.IsDead || health.IsDead) return;
            began = true;
            health.SetVulnerable(!survivalOnly);
            SetState(GuardianState.Idle, .9f);
        }

        private void Update()
        {
            if (began && !resolved && (player == null || player.IsDead))
            {
                CancelFight();
                return;
            }
            if (!CanDamage) return;
            remaining -= Time.deltaTime;
            switch (State)
            {
                case GuardianState.Idle:
                    if (remaining <= 0f) PrepareAttack();
                    break;
                case GuardianState.PreparingAttack:
                    if (warning != null) warning.color = new Color(1f, .65f + .15f * Mathf.Sin(remaining * 20f), .17f,
                        .65f + .3f * Mathf.Abs(Mathf.Sin(remaining * 12f)));
                    if (remaining <= 0f) ExecuteAttack();
                    break;
                case GuardianState.Attacking:
                    if (impactPending)
                    {
                        impactRemaining -= Time.deltaTime;
                        if (impactRemaining <= 0f) ContactGround();
                    }
                    if (CurrentAttack == GuardianAttack.LeafStorm)
                    {
                        leafDelay -= Time.deltaTime;
                        if (wavesSent < leafWaves && leafDelay <= 0f) LaunchLeaf();
                    }
                    if (remaining <= 0f) SetState(GuardianState.Recovering, .35f);
                    break;
                case GuardianState.Recovering:
                case GuardianState.Stunned:
                    if (remaining <= 0f)
                    {
                        // Deterministic alternating intervals make the pattern learnable.
                        float delay = attackIndex % 2 == 0 ? minimumAttackInterval : maximumAttackInterval;
                        SetState(GuardianState.Idle, delay);
                    }
                    break;
            }
        }

        private void PrepareAttack()
        {
            ClearHazards();
            CurrentAttack = (GuardianAttack)(attackIndex++ % 3);
            attackX = Mathf.Clamp(player.transform.position.x, arena.x + .8f, arena.y - .8f);
            if (CurrentAttack == GuardianAttack.BranchSlam)
            {
                // The right-side guardian hits only its near side. The left half remains safe.
                attackX = Mathf.Clamp(transform.position.x - 2.8f, arena.x + 2f, arena.y - 2f);
            }
            if (warning != null)
            {
                warning.transform.position = new Vector3(CurrentAttack == GuardianAttack.LeafStorm
                    ? transform.position.x - 1.8f : attackX, floorY + .07f, 0f);
                warning.transform.localScale = new Vector3(CurrentAttack == GuardianAttack.RootStrike ? 1.7f :
                    CurrentAttack == GuardianAttack.BranchSlam ? 3.8f : 1.4f, .16f, 1f);
                warning.color = new Color(1f, .76f, .24f, .85f);
                warning.enabled = true;
            }
            SetState(GuardianState.PreparingAttack, warningDuration);
            if (attackEffects != null) attackEffects.Telegraph(CurrentAttack,
                new Vector2(attackX, floorY + .08f), warningDuration);
        }

        private void ExecuteAttack()
        {
            switch (CurrentAttack)
            {
                case GuardianAttack.RootStrike:
                case GuardianAttack.BranchSlam:
                    impactPending = true;
                    impactRemaining = impactDelay;
                    SetState(GuardianState.Attacking, impactDelay +
                        (CurrentAttack == GuardianAttack.RootStrike ? rootDuration : slamDuration));
                    if (impactDelay <= 0f) ContactGround();
                    break;
                case GuardianAttack.LeafStorm:
                    if (warning != null) warning.enabled = false;
                    wavesSent = 0;
                    leafDelay = 0f;
                    SetState(GuardianState.Attacking, leafWaves * leafWaveInterval + .2f);
                    LaunchLeaf();
                    break;
            }
        }

        private void ContactGround()
        {
            impactPending = false;
            if (warning != null) warning.enabled = false;
            bool rootAttack = CurrentAttack == GuardianAttack.RootStrike;
            Launch(rootAttack ? rootSprite : branchSprite,
                new Vector2(attackX, floorY + (rootAttack ? .86f : .34f)),
                rootAttack ? new Vector2(1.55f, 1.72f) : new Vector2(3.8f, .68f), Vector2.zero,
                rootAttack ? rootDuration : slamDuration, CurrentAttack);
            Impact?.Invoke(rootAttack ? .08f : .15f);
            if (attackEffects != null) attackEffects.Contact(CurrentAttack, new Vector2(attackX, floorY + .06f));
        }

        private void LaunchLeaf()
        {
            // Low, evenly spaced leaves travel horizontally; jumping clears the whole stream.
            wavesSent++;
            leafDelay = leafWaveInterval;
            if (attackEffects != null) attackEffects.Contact(GuardianAttack.LeafStorm,
                new Vector2(transform.position.x - 1.65f, floorY + .72f));
            if (animationRig != null) animationRig.PulseLeaves();
            Launch(leafSprite, new Vector2(transform.position.x - 1.65f, floorY + .72f),
                new Vector2(.7f, .4f), Vector2.left * leafSpeed,
                (arena.y - arena.x + 4f) / leafSpeed, GuardianAttack.LeafStorm);
        }

        private void Launch(Sprite sprite, Vector2 position, Vector2 size, Vector2 velocity, float lifetime, GuardianAttack kind)
        {
            foreach (var hazard in pool)
            {
                if (hazard == null || hazard.gameObject.activeSelf) continue;
                hazard.Launch(this, sprite, position, size, velocity, lifetime, kind);
                return;
            }
            Debug.LogWarning("Forest Guardian hazard pool is full; attack omitted to preserve readable combat.", this);
        }

        private void HealthChanged(int current, int maximum)
        {
            if (!IsFighting || current <= 0) return;
            // A hit interrupts the entire attack, so a telegraph can never turn into an unseen hazard.
            ClearHazards();
            SetState(GuardianState.Stunned, hurtDuration);
            Impact?.Invoke(.05f);
        }

        private void OnDied() => Resolve(false);

        public void Resolve(bool retreat)
        {
            if (survivalOnly && !retreat) return;
            if (!began || resolved || player == null || player.IsDead) return;
            resolved = true;
            began = false;
            health.SetVulnerable(false);
            ClearHazards();
            SetState(retreat ? GuardianState.Retreating : GuardianState.Defeated);
            Resolved?.Invoke(retreat);
        }

        private void SetState(GuardianState value, float seconds = 0f)
        {
            State = value;
            remaining = seconds;
            if (animationRig == null) return;
            GuardianAnimationState visual = value == GuardianState.Intro ? GuardianAnimationState.Intro :
                value == GuardianState.Inactive ? GuardianAnimationState.Idle :
                value == GuardianState.PreparingAttack ? GuardianAnimationState.Windup :
                value == GuardianState.Attacking ? GuardianAnimationState.Attack :
                value == GuardianState.Recovering ? GuardianAnimationState.Recovery :
                value == GuardianState.Stunned ? GuardianAnimationState.Hurt :
                value == GuardianState.Defeated ? GuardianAnimationState.Death :
                value == GuardianState.Retreating ? GuardianAnimationState.Retreat :
                health.CurrentHealth <= health.MaximumHealth * .4f ? GuardianAnimationState.Angry : GuardianAnimationState.CombatIdle;
            animationRig.Play(visual, CurrentAttack, seconds, impactDelay);
        }

        public void ClearHazards()
        {
            impactPending = false;
            if (attackEffects != null) attackEffects.Clear();
            if (warning != null) warning.enabled = false;
            foreach (var hazard in pool) if (hazard != null) hazard.Release();
        }

        public void CancelFight()
        {
            began = false;
            ClearHazards();
            health.SetVulnerable(false);
            if (!resolved) SetState(GuardianState.Inactive);
        }

        private void OnDisable()
        {
            health.HealthChanged -= HealthChanged;
            health.Died -= OnDied;
            CancelFight();
        }

        private void OnValidate()
        {
            minimumAttackInterval = Mathf.Max(1f, minimumAttackInterval);
            maximumAttackInterval = Mathf.Max(minimumAttackInterval, maximumAttackInterval);
            warningDuration = Mathf.Max(.65f, warningDuration);
            leafSpeed = Mathf.Max(1f, leafSpeed);
        }
    }
}
