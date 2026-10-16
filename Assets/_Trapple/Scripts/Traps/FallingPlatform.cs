using System.Collections;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Traps
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class FallingPlatform : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float fallDelay = 0.4f;
        [SerializeField, Min(0.1f)] private float fallGravityScale = 2f;
        [SerializeField, Min(0.1f)] private float lifetimeAfterFall = 4f;

        public bool IsTriggered { get; private set; }
        public bool HasFallen { get; private set; }

        private void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryActivate(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryActivate(collision);

        private void TryActivate(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerDeath>();
            if (IsTriggered || player == null || player.IsDead)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < -0.5f)
                {
                    IsTriggered = true;
                    StartCoroutine(Fall());
                    return;
                }
            }
        }

        private IEnumerator Fall()
        {
            yield return new WaitForSeconds(fallDelay);
            var body = GetComponent<Rigidbody2D>();
            body.gravityScale = fallGravityScale;
            body.bodyType = RigidbodyType2D.Dynamic;
            HasFallen = true;
            Destroy(gameObject, lifetimeAfterFall);
        }
    }
}
