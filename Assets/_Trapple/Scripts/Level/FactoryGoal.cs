using Trapple.Core;
using Trapple.Player;
using Trapple.Hub;
using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FactoryGoal : MonoBehaviour
    {
        private bool completed;
        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (completed || player == null || player.IsDead) return;
            completed = true;
            player.GetComponent<PlayerController>().SetControlEnabled(false);
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            FactoryCheckpoint.Clear();
            WorldCompletion.Record("factory", gameObject.scene.path);
            if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        }
    }
}
