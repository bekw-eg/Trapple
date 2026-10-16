using System.Collections;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Traps
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HiddenTrap : MonoBehaviour
    {
        [SerializeField] private SpikeTrap spikes;
        [SerializeField, Min(0f)] private float activationDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.15f;
        [SerializeField, Min(0f)] private float riseDistance = 0.8f;

        public bool IsTriggered { get; private set; }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            if (spikes == null)
            {
                Debug.LogError("HiddenTrap requires a child SpikeTrap reference.", this);
                enabled = false;
                return;
            }
            spikes.SetArmed(false);
            spikes.gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other) => TryActivate(other);
        private void OnTriggerStay2D(Collider2D other) => TryActivate(other);

        private void TryActivate(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (!enabled || IsTriggered || player == null || player.IsDead)
                return;

            IsTriggered = true;
            StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            yield return new WaitForSeconds(activationDelay);
            Vector3 start = spikes.transform.localPosition;
            Vector3 end = start + Vector3.up * riseDistance;
            spikes.gameObject.SetActive(true);
            spikes.SetArmed(true);
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                spikes.transform.localPosition = Vector3.Lerp(start, end, elapsed / riseDuration);
                yield return null;
            }
            spikes.transform.localPosition = end;
        }
    }
}
