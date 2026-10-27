using System.Collections;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ForestDisappearingPlatform : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float warningDuration = .8f;
        [SerializeField] private SpriteRenderer artwork;
        public bool IsTriggered { get; private set; }
        public bool HasVanished { get; private set; }
        private void OnCollisionEnter2D(Collision2D collision) => TryActivate(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryActivate(collision);
        private void TryActivate(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerDeath>();
            if (IsTriggered || player == null || player.IsDead) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y < -.5f)
                {
                    IsTriggered = true;
                    StartCoroutine(Vanish());
                    break;
                }
        }
        private IEnumerator Vanish()
        {
            Color original = artwork != null ? artwork.color : Color.white;
            for (float t = 0; t < warningDuration; t += Time.deltaTime)
            {
                if (artwork != null) artwork.color = Color.Lerp(original, new Color(1f, .66f, .3f),
                    .35f + .25f * Mathf.Sin(t * 24f));
                yield return null;
            }
            // The surface remains opaque throughout its warning; support and art disappear together.
            GetComponent<Collider2D>().enabled = false;
            if (artwork != null) artwork.enabled = false;
            HasVanished = true;
        }
    }
}
