using Trapple.Player;
using Trapple.Traps;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpikeTrap))]
    public sealed class ForestFallingTree : MonoBehaviour
    {
        [SerializeField] private Transform artwork;
        [SerializeField, Min(.1f)] private float warningDuration = 1.1f;
        [SerializeField, Min(.1f)] private float fallDuration = .85f;
        [SerializeField] private float fallAngle = 90f;
        private Rigidbody2D body;
        private SpikeTrap danger;
        private float elapsed;
        public bool IsTriggered { get; private set; }
        public bool HasFallen { get; private set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); danger = GetComponent<SpikeTrap>(); danger.SetArmed(false);
        }
        public void Trigger(PlayerDeath player)
        {
            if (IsTriggered || player == null || player.IsDead) return;
            IsTriggered = true;
        }
        private void FixedUpdate()
        {
            if (!IsTriggered || HasFallen) return;
            elapsed += Time.fixedDeltaTime;
            if (elapsed < warningDuration)
            {
                if (artwork != null) artwork.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 24f) * 2.4f);
                return;
            }
            if (artwork != null) artwork.localRotation = Quaternion.identity;
            danger.SetArmed(true);
            float t = Mathf.Clamp01((elapsed - warningDuration) / fallDuration);
            body.MoveRotation(fallAngle * t * t);
            if (t < 1f) return;
            HasFallen = true; danger.SetArmed(false);
            GetComponent<BoxCollider2D>().isTrigger = false;
            gameObject.layer = 6; // The settled trunk is a short, safe step.
        }
    }
}
