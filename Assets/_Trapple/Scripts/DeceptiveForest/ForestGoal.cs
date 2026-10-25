using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ForestGoal : MonoBehaviour
    {
        public const string ScenePath = "Assets/_Trapple/Scenes/DeceptiveForest/DeceptiveForest_Level02.unity";
        public bool IsComplete { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (IsComplete || player == null || player.IsDead || GameManager.Instance == null ||
                GameManager.Instance.State != GameState.Playing) return;
            IsComplete = true;
            player.GetComponent<PlayerController>().SetControlEnabled(false);
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            GetComponent<Collider2D>().enabled = false;
            GameManager.Instance.CompleteLevel();
        }
    }
}
