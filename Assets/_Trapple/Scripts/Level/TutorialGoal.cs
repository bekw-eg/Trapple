using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class TutorialGoal : MonoBehaviour
    {
        public bool IsComplete { get; private set; }
        private SpriteRenderer artwork;

        public void SetArtwork(SpriteRenderer value) => artwork = value;

        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (IsComplete || player == null || player.IsDead)
                return;

            IsComplete = true;
            player.GetComponent<PlayerController>().SetControlEnabled(false);
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            if (artwork != null) artwork.enabled = false;
        }

        private void Update()
        {
            if (IsComplete && Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(gameObject.scene.path);
        }

        private void OnGUI()
        {
            if (Trapple.Core.GameManager.Instance != null &&
                Trapple.Core.GameManager.Instance.State == Trapple.Core.GameState.Paused) return;
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 30f, 16f, 28f)),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUI.Box(new Rect(16f, 16f, Screen.width - 32f, 58f),
                "ЯБЛОЧНЫЙ ЛУГ    A / D или стрелки: движение    ПРОБЕЛ: прыжок    ESC: пауза", style);
            if (IsComplete)
                GUI.Box(new Rect(Screen.width * 0.2f, Screen.height * 0.4f, Screen.width * 0.6f, 110f),
                    "LEVEL COMPLETE\nPress R to restart", style);
        }
    }
}
