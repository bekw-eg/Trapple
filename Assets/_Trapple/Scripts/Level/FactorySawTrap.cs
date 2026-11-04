using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class FactorySawTrap : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float rotationSpeed = 240f;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Update() => transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        private void OnTriggerEnter2D(Collider2D other) => Kill(other);
        private void OnTriggerStay2D(Collider2D other) => Kill(other);
        private static void Kill(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && !player.IsDead) player.Die();
        }
    }
}
