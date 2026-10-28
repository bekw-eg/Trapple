using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class StickPickup : MonoBehaviour
    {
        [SerializeField] private string pickupId;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private AudioClip pickupSound;
        private Collider2D hitbox;
        private AudioSource sound;
        private Vector3 artPosition;
        private Color artColor;
        private float animationTime;
        public string PickupId => string.IsNullOrEmpty(pickupId) ? name : pickupId;
        public bool IsCollected { get; private set; }

        private void Awake()
        {
            hitbox = GetComponent<Collider2D>();
            if (artwork == null) artwork = GetComponentInChildren<SpriteRenderer>();
            if (artwork != null) { artPosition = artwork.transform.localPosition; artColor = artwork.color; }
            if (pickupSound != null)
            {
                sound = gameObject.AddComponent<AudioSource>();
                sound.playOnAwake = false; sound.spatialBlend = 0; sound.volume = .3f;
            }
        }

        private void Update()
        {
            if (artwork == null) return;
            animationTime += Time.deltaTime;
            if (!IsCollected)
            {
                artwork.transform.localPosition = artPosition + Vector3.up * (Mathf.Sin(animationTime * 2.4f) * .055f);
                artwork.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(animationTime * 1.8f) * 5f);
                return;
            }
            float t = Mathf.Clamp01(animationTime / .3f);
            artwork.transform.localPosition = artPosition + Vector3.up * t * .65f;
            artwork.color = new Color(artColor.r, artColor.g, artColor.b, artColor.a * (1f - t));
            if (t >= 1f) gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var inventory = other.GetComponentInParent<StickInventory>();
            if (inventory != null) TryCollect(inventory);
        }

        public bool TryCollect(StickInventory inventory)
        {
            if (IsCollected || inventory == null || Time.timeScale <= 0f) return false;
            var player = inventory.GetComponent<PlayerDeath>();
            var control = inventory.GetComponent<PlayerController>();
            if (player == null || player.IsDead || control == null || !control.ControlEnabled ||
                (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)) return false;
            if (!inventory.TryAdd(amount)) return false;
            IsCollected = true;
            animationTime = 0;
            hitbox.enabled = false;
            if (sound != null && pickupSound != null) sound.PlayOneShot(pickupSound);
            return true;
        }

        /// <summary>Hides a pickup already owned by the restored checkpoint; never grants ammunition.</summary>
        public void MarkCollected()
        {
            IsCollected = true;
            if (hitbox != null) hitbox.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
