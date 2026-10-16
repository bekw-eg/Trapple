using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float restartDelay = 0.65f;
        [SerializeField] private float fallDeathY = -8f;

        public bool IsDead { get; private set; }

        private void Update()
        {
            if (transform.position.y < fallDeathY)
                Die();
        }

        // Shared API for traps and future boss attacks. One call starts exactly one restart.
        public void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            GetComponent<PlayerController>().SetControlEnabled(false);
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            StartCoroutine(RestartLevel());
        }

        private IEnumerator RestartLevel()
        {
            yield return new WaitForSecondsRealtime(restartDelay);
            // Reload the player's scene, even when another scene is active for integration.
            SceneManager.LoadScene(gameObject.scene.path);
        }
    }
}
