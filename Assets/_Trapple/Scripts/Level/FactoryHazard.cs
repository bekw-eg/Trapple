using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FactoryHazard : MonoBehaviour
    {
        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void OnTriggerEnter2D(Collider2D other) => Kill(other);
        private void OnTriggerStay2D(Collider2D other) => Kill(other);
        private static void Kill(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && !player.IsDead) player.Die();
        }
    }
}
