using UnityEngine;
using Trapple.Player;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class FactoryPressTrap : MonoBehaviour
    {
        [SerializeField] private Vector2 raisedPosition;
        [SerializeField] private Vector2 loweredPosition;
        [SerializeField, Min(.1f)] private float cycleDuration = 5.6f;
        [SerializeField, Min(.1f)] private float travelDuration = .55f;
        [SerializeField, Min(0f)] private float loweredPause = .55f;
        [SerializeField, Min(0f)] private float initialDelay;
        [SerializeField] private SpriteRenderer warningIndicator;
        private Rigidbody2D body;
        private float timeOrigin;
        public bool IsDangerous { get; private set; }
        public void SetWarningIndicator(SpriteRenderer indicator) => warningIndicator = indicator;

        public void Configure(Vector2 raised, Vector2 lowered, float cycle, float travel, float delay)
        {
            raisedPosition = raised;
            loweredPosition = lowered;
            cycleDuration = cycle;
            travelDuration = travel;
            initialDelay = delay;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            timeOrigin = Time.time;
            if (raisedPosition == Vector2.zero) raisedPosition = body.position;
        }

        private void FixedUpdate()
        {
            float t = Mathf.Max(0f, Time.time - timeOrigin - initialDelay);
            float topPause = Mathf.Max(0f, cycleDuration - travelDuration * 2f - loweredPause);
            float phase = t % Mathf.Max(.1f, cycleDuration);
            Vector2 target = raisedPosition;
            IsDangerous = phase >= topPause;
            if (phase >= topPause)
            {
                phase -= topPause;
                if (phase < travelDuration)
                    target = Vector2.Lerp(raisedPosition, loweredPosition, Mathf.SmoothStep(0f, 1f, phase / travelDuration));
                else if ((phase -= travelDuration) < loweredPause)
                    target = loweredPosition;
                else
                {
                    phase -= loweredPause;
                    target = Vector2.Lerp(loweredPosition, raisedPosition, Mathf.SmoothStep(0f, 1f, phase / travelDuration));
                }
            }
            body.MovePosition(target);
        }

        private void Update()
        {
            if (warningIndicator == null) return;
            warningIndicator.color = IsDangerous && Mathf.Sin(Time.time * 13f) > 0f
                ? new Color(1f, .16f, .05f) : new Color(1f, .7f, .18f);
        }

        private void OnTriggerEnter2D(Collider2D other) => CheckCrush(other);
        private void OnTriggerStay2D(Collider2D other) => CheckCrush(other);
        private void CheckCrush(Collider2D other)
        {
            if (!IsDangerous) return;
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && !player.IsDead) player.Die();
        }
    }
}
