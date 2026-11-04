using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FactoryConveyor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float beltForce = 22f;
        [SerializeField] private float direction = 1f;
        private PlayerController player;
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            player = FindAnyObjectByType<PlayerController>();
        }
        public void Configure(float force, float travelDirection)
        { beltForce = force; direction = Mathf.Sign(travelDirection); }

        private void FixedUpdate()
        {
            if (player == null || !player.ControlEnabled) return;
            var zone = GetComponent<BoxCollider2D>();
            var playerCollider = player.GetComponent<CapsuleCollider2D>();
            if (playerCollider == null || !zone.bounds.Intersects(playerCollider.bounds)) return;
            var body = player.GetComponent<Rigidbody2D>();
            if (!body.simulated) return;
            Vector2 velocity = body.linearVelocity;
            velocity.x += beltForce * direction * Time.fixedDeltaTime;
            body.linearVelocity = velocity;
        }
    }
}
