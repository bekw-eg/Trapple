using System.Collections;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ForestDisappearingPlatform : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float warningDuration = .24f;
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private Transform surface;
        [SerializeField] private GameObject concealedHazard;
        [SerializeField] private bool activateOnLanding = true;
        public enum Phase { Dormant, Triggered, Activating, Resolved }
        public Phase State { get; private set; }
        public bool IsTriggered => State != Phase.Dormant;
        public bool HasVanished => State == Phase.Resolved;
        private void Awake()
        {
            if (concealedHazard != null) concealedHazard.SetActive(false);
        }
        private void OnCollisionEnter2D(Collision2D collision) => TryActivate(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryActivate(collision);
        private void TryActivate(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerDeath>();
            if (!activateOnLanding || IsTriggered || player == null || player.IsDead) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y < -.5f)
                {
                    Trigger(player);
                    break;
                }
        }
        public void Trigger(PlayerDeath player)
        {
            if (IsTriggered || player == null || player.IsDead) return;
            State = Phase.Triggered;
            StartCoroutine(Vanish());
        }
        private IEnumerator Vanish()
        {
            Transform visuals = surface != null ? surface : artwork != null ? artwork.transform : null;
            Vector3 original = visuals != null ? visuals.localPosition : Vector3.zero;
            for (float t = 0; t < warningDuration; t += Time.deltaTime)
            {
                State = t < warningDuration * .5f ? Phase.Triggered : Phase.Activating;
                // Only move after contact. No colour code distinguishes a dormant trap.
                if (visuals != null && State == Phase.Activating)
                    visuals.localPosition = original + Vector3.right * (Mathf.Sin(t * 80f) * .025f);
                yield return null;
            }
            // The complete soil/grass/decor cover and physical support disappear in the same frame.
            GetComponent<Collider2D>().enabled = false;
            if (visuals != null) visuals.gameObject.SetActive(false);
            if (concealedHazard != null) concealedHazard.SetActive(true);
            State = Phase.Resolved;
        }
    }
}
