using System;
using System.Collections;
using UnityEngine;

namespace Trapple.Bosses
{
    public enum BossAttackKind { RootAttack, FallingBranch, ArenaTrap }

    public sealed class BossAttack : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private SpriteRenderer warning;
        [SerializeField] private BossHazard roots;
        [SerializeField] private BossHazard branch;
        [SerializeField] private BossHazard platformTrap;
        [SerializeField] private Vector2 arenaX = new Vector2(-8.5f, 3.8f);
        [SerializeField] private float floorY;
        [SerializeField, Min(0.1f)] private float warningDuration = 1.2f;
        [SerializeField, Min(0.1f)] private float minimumWarningDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float rootDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float branchFallDuration = 0.7f;
        [SerializeField] private float branchStartY = 7f;
        [SerializeField, Min(0.1f)] private float trapDuration = 1.4f;

        public BossAttackKind CurrentAttack { get; private set; }
        public bool IsTelegraphing { get; private set; }
        public bool IsExecuting { get; private set; }
        public float WarningDuration => warningDuration;
        public Transform PlayerTarget { get => playerTarget; set => playerTarget = value; }

        public IEnumerator Execute(BossAttackKind kind, float durationMultiplier, Action onAttack)
        {
            Cancel();
            CurrentAttack = kind;
            float x = Mathf.Clamp(playerTarget != null ? playerTarget.position.x : 0f, arenaX.x, arenaX.y);
            Vector3 markerPosition = new Vector3(x, floorY + 0.08f, 0f);
            if (kind == BossAttackKind.ArenaTrap)
                markerPosition = platformTrap.transform.position;
            warning.transform.position = markerPosition;
            warning.transform.localScale = new Vector3(kind == BossAttackKind.ArenaTrap ? 3f : 2f, 0.18f, 1f);
            warning.enabled = true;
            IsTelegraphing = true;
            yield return new WaitForSeconds(Mathf.Max(minimumWarningDuration, warningDuration * durationMultiplier));
            warning.enabled = false;
            IsTelegraphing = false;
            IsExecuting = true;
            onAttack?.Invoke();

            switch (kind)
            {
                case BossAttackKind.RootAttack:
                    roots.transform.position = new Vector3(x, floorY + 1.25f, 0f);
                    roots.SetArmed(true);
                    yield return new WaitForSeconds(rootDuration * durationMultiplier);
                    roots.SetArmed(false);
                    break;
                case BossAttackKind.FallingBranch:
                    branch.transform.position = new Vector3(x, branchStartY, 0f);
                    branch.SetArmed(true);
                    float duration = branchFallDuration * durationMultiplier;
                    for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                    {
                        branch.transform.position = new Vector3(x, Mathf.Lerp(branchStartY, floorY + 0.4f,
                            Mathf.Clamp01(elapsed / duration)), 0f);
                        yield return null;
                    }
                    branch.SetArmed(false);
                    break;
                case BossAttackKind.ArenaTrap:
                    platformTrap.SetArmed(true);
                    yield return new WaitForSeconds(trapDuration * durationMultiplier);
                    platformTrap.SetArmed(false);
                    break;
            }
            IsExecuting = false;
        }

        public void Cancel()
        {
            if (warning != null) warning.enabled = false;
            if (roots != null) roots.SetArmed(false);
            if (branch != null) branch.SetArmed(false);
            if (platformTrap != null) platformTrap.SetArmed(false);
            IsTelegraphing = false;
            IsExecuting = false;
        }

        private void OnDisable() => Cancel();
        private void OnValidate()
        {
            warningDuration = Mathf.Max(0.1f, warningDuration);
            minimumWarningDuration = Mathf.Clamp(minimumWarningDuration, 0.1f, warningDuration);
            rootDuration = Mathf.Max(0.1f, rootDuration);
            branchFallDuration = Mathf.Max(0.1f, branchFallDuration);
            trapDuration = Mathf.Max(0.1f, trapDuration);
        }
    }
}
