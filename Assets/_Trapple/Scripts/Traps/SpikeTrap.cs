using Trapple.Player;
using UnityEngine;

namespace Trapple.Traps
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SpikeTrap : MonoBehaviour
    {
        [SerializeField] private bool armed = true;

        public bool IsArmed => armed;

        public void SetArmed(bool value) => armed = value;

        private void OnTriggerEnter2D(Collider2D other) => TryKill(other);
        private void OnTriggerStay2D(Collider2D other) => TryKill(other);
        private void OnCollisionEnter2D(Collision2D other) => TryKill(other.collider);
        private void OnCollisionStay2D(Collision2D other) => TryKill(other.collider);

        private void TryKill(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (armed && player != null)
                player.Die();
        }
    }
}
