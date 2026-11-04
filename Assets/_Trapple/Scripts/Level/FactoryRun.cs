using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    public sealed class FactoryRun : MonoBehaviour
    {
        private PlayerDeath player;
        public float ElapsedSeconds { get; private set; }
        public bool IsComplete { get; private set; }

        private void Awake()
        {
            player = FindAnyObjectByType<PlayerDeath>();
            FactoryWorldArt.Apply();
        }

        private void Update()
        {
            if (!IsComplete && player != null && !player.IsDead && GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Playing)
                ElapsedSeconds += Time.deltaTime;
        }

        public void Complete()
        {
            if (IsComplete) return;
            IsComplete = true;
            GameManager.Instance?.CompleteLevel();
        }

        public string TimeLabel
        {
            get
            {
                int seconds = Mathf.FloorToInt(ElapsedSeconds);
                return $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class FactoryGoal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player == null || player.IsDead) return;
            player.GetComponent<PlayerController>().SetControlEnabled(false);
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            FindAnyObjectByType<FactoryRun>()?.Complete();
        }
    }
}
