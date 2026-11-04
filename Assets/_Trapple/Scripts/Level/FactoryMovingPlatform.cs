using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class FactoryMovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField, Min(.1f)] private float travelTime = 2f;
        [SerializeField] private float phaseOffset;
        private Rigidbody2D body;

        public void Configure(Vector2 a, Vector2 b, float duration, float phase = 0f)
        {
            pointA = a; pointB = b; travelTime = duration; phaseOffset = phase;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (pointA == Vector2.zero && pointB == Vector2.zero) pointA = body.position;
        }

        private void FixedUpdate()
        {
            float phase = Mathf.PingPong((Time.time + phaseOffset) / Mathf.Max(.1f, travelTime), 1f);
            body.MovePosition(Vector2.Lerp(pointA, pointB, phase));
        }
    }
}
