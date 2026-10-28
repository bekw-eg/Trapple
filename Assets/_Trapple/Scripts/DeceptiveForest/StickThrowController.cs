using System.Collections.Generic;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    // Run before the original puzzle stick's Update so one F press cannot throw both items.
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StickInventory), typeof(PlayerController), typeof(PlayerDeath))]
    public sealed class StickThrowController : MonoBehaviour
    {
        [SerializeField] private StickInventory inventory;
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private StickProjectile projectilePrefab;
        [SerializeField] private Transform handVisual;
        [SerializeField] private KeyCode throwKey = KeyCode.F;
        [SerializeField, Min(.1f)] private float throwForce = 12f;
        [SerializeField, Range(0, 65)] private float throwAngle = 10f;
        [SerializeField, Min(0)] private float gravityScale = .65f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(.05f)] private float cooldown = .35f;
        [SerializeField, Range(1, 20)] private int poolCapacity = 6;
        private readonly Queue<StickProjectile> available = new Queue<StickProjectile>();
        private readonly List<StickProjectile> pool = new List<StickProjectile>();
        private PlayerController controller;
        private PlayerDeath death;
        private PlayerSpriteAnimator animator;
        private ForestStick[] puzzleSticks;
        private float readyAt, gestureRemaining;
        private int gestureFacing;
        public StickProjectile LastProjectile { get; private set; }
        public KeyCode ThrowKey => throwKey;
        public int FacingDirection
        {
            get
            {
                // Running/walking frames have authored directions, so flipX is deliberately false for both.
                if (animator != null)
                {
                    if (animator.State == PlayerVisualState.RunLeft || animator.State == PlayerVisualState.WalkLeft) return -1;
                    if (animator.State == PlayerVisualState.RunRight || animator.State == PlayerVisualState.WalkRight) return 1;
                }
                return characterRenderer != null && characterRenderer.flipX ? -1 : 1;
            }
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<StickInventory>();
            if (characterRenderer == null) characterRenderer = GetComponentInChildren<SpriteRenderer>();
            controller = GetComponent<PlayerController>(); death = GetComponent<PlayerDeath>();
            animator = GetComponent<PlayerSpriteAnimator>();
            if (handVisual != null) handVisual.gameObject.SetActive(false);
        }

        private void Start()
        {
            puzzleSticks = FindObjectsByType<ForestStick>();
            if (projectilePrefab == null) return;
            for (int i = 0; i < poolCapacity; i++)
            {
                var projectile = Instantiate(projectilePrefab);
                projectile.name = "Pooled combat stick";
                projectile.gameObject.SetActive(false);
                pool.Add(projectile); available.Enqueue(projectile);
            }
        }

        private void Update()
        {
            if (death.IsDead || (GameManager.Instance != null && GameManager.Instance.State == GameState.Victory))
            { ClearProjectiles(); return; }
            if (Input.GetKeyDown(throwKey)) TryThrow();
        }

        private void LateUpdate()
        {
            if (handVisual == null) return;
            gestureRemaining = Mathf.Max(0, gestureRemaining - Time.deltaTime);
            bool showing = gestureRemaining > 0 && !death.IsDead;
            handVisual.gameObject.SetActive(showing);
            if (!showing) return;
            float t = 1f - gestureRemaining / .22f;
            handVisual.localPosition = new Vector3(gestureFacing * Mathf.Lerp(.13f, .55f, t), .05f + Mathf.Sin(t * Mathf.PI) * .18f, 0);
            handVisual.localRotation = Quaternion.Euler(0, 0, gestureFacing * Mathf.Lerp(80, -35, t));
        }

        public bool TryThrow()
        {
            if (inventory == null || inventory.Count == 0 || projectilePrefab == null || available.Count == 0 ||
                death == null || death.IsDead || controller == null || !controller.ControlEnabled ||
                Time.timeScale <= 0f || Time.time < readyAt ||
                (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)) return false;
            if (puzzleSticks != null)
                foreach (var stick in puzzleSticks)
                    if (stick != null && stick.gameObject.scene == gameObject.scene && stick.IsHeld) return false;
            int facing = FacingDirection;
            float angle = throwAngle * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angle) * facing, Mathf.Sin(angle)) * throwForce;
            var shot = available.Dequeue();
            // Start inside the owner's ignored collider: a close wall cannot be skipped by a spawn offset.
            shot.Launch(transform, (Vector2)transform.position + Vector2.up * .12f, velocity, gravityScale, damage, Release);
            inventory.TrySpend();
            readyAt = Time.time + cooldown;
            gestureRemaining = .22f; gestureFacing = facing; LastProjectile = shot;
            return true;
        }

        private void Release(StickProjectile projectile)
        {
            if (projectile != null && !available.Contains(projectile)) available.Enqueue(projectile);
        }

        public void ClearProjectiles()
        {
            foreach (var projectile in pool) if (projectile != null) projectile.ReturnToPool();
            gestureRemaining = 0;
            if (handVisual != null) handVisual.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var projectile in pool) if (projectile != null) Destroy(projectile.gameObject);
        }
    }
}
