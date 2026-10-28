using System.Collections;
using Trapple.Core;
using UnityEngine;

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

        // Shared API for traps and boss attacks. Keep the existing death delay before showing UI.
        public void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            GetComponent<PlayerController>().SetControlEnabled(false);
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            GameManager.Instance?.BeginPlayerDeath(this);
            StartCoroutine(FinishDeath());
        }

        private IEnumerator FinishDeath()
        {
            yield return new WaitForSecondsRealtime(restartDelay);
            GameManager.Instance?.FinishPlayerDeath(this);
        }
    }
}
