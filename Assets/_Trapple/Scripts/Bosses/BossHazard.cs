using Trapple.Player;
using UnityEngine;

namespace Trapple.Bosses
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class BossHazard : MonoBehaviour
    {
        public bool Armed { get; private set; }
        public void SetArmed(bool armed)
        {
            Armed = armed;
            GetComponent<BoxCollider2D>().enabled = armed;
            GetComponent<SpriteRenderer>().enabled = armed;
        }
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            SetArmed(false);
        }
        private void OnDisable() => SetArmed(false);
        private void OnTriggerEnter2D(Collider2D other) => Hit(other);
        private void OnTriggerStay2D(Collider2D other) => Hit(other);
        private void Hit(Collider2D other)
        {
            if (!Armed) return;
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null) player.Die();
        }
    }
}
