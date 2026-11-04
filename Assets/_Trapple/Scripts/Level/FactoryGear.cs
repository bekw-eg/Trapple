using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class FactoryGear : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float throwForce = 11f;
        private Rigidbody2D body;
        private CircleCollider2D hitbox;
        private PlayerController holder;
        private Transform hand;
        private bool consumed;

        public bool IsHeld => holder != null;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<CircleCollider2D>();
            FactoryWorldArt.FitSpriteToNominalSize(GetComponent<SpriteRenderer>(), Vector2.one * .82f);
        }

        public float PickupRange => 1.35f;

        public bool TryPickUp(PlayerController player)
        {
            if (consumed || holder != null || player == null || (player.transform.position - transform.position).sqrMagnitude > PickupRange * PickupRange)
                return false;
            PickUp(player);
            return true;
        }

        public void Throw()
        {
            if (consumed || holder == null) return;
            bool left = holder.GetComponent<SpriteRenderer>() != null && holder.GetComponent<SpriteRenderer>().flipX;
            transform.SetParent(null, true);
            body.simulated = true;
            hitbox.enabled = true;
            body.linearVelocity = Vector2.zero;
            body.AddForce(new Vector2(left ? -throwForce : throwForce, 2.3f), ForceMode2D.Impulse);
            holder = null;
        }

        private void PickUp(PlayerController player)
        {
            holder = player;
            hand = player.transform.Find("FactoryGearHand");
            if (hand == null)
            {
                var handObject = new GameObject("FactoryGearHand");
                hand = handObject.transform;
                hand.SetParent(player.transform, false);
            }
            UpdateHandPosition();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            hitbox.enabled = false;
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (holder == null || hand == null) return;
            UpdateHandPosition();
        }

        private void UpdateHandPosition()
        {
            var renderer = holder.GetComponent<SpriteRenderer>();
            float facing = renderer != null && renderer.flipX ? -1f : 1f;
            hand.localPosition = new Vector3(.62f * facing, .48f, 0f);
            hand.localRotation = Quaternion.identity;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var wall = collision.collider.GetComponent<FactoryGearWall>();
            if (wall != null && !IsHeld && !consumed)
            {
                wall.Open();
                Consume();
            }
        }

        public void Consume() { consumed = true; Destroy(gameObject); }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class FactoryGearInteractor : MonoBehaviour
    {
        private PlayerController player;
        private FactoryGear held;

        private void Awake() => player = GetComponent<PlayerController>();

        private void Update()
        {
            if (!player.ControlEnabled || Trapple.Core.GameManager.Instance != null &&
                Trapple.Core.GameManager.Instance.State != Trapple.Core.GameState.Playing) return;
            if (held != null)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    held.Throw();
                    held = null;
                }
                return;
            }
            if (!Input.GetKeyDown(KeyCode.E)) return;
            FactoryGear nearest = null;
            float nearestSqr = float.PositiveInfinity;
            foreach (var gear in FindObjectsByType<FactoryGear>())
            {
                float distance = (player.transform.position - gear.transform.position).sqrMagnitude;
                if (distance <= gear.PickupRange * gear.PickupRange && distance < nearestSqr)
                {
                    nearest = gear;
                    nearestSqr = distance;
                }
            }
            if (nearest != null && nearest.TryPickUp(player)) held = nearest;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FactoryGearWall : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private Collider2D barrier;
        [SerializeField] private SpriteRenderer openIndicator;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (artwork == null) artwork = GetComponent<SpriteRenderer>();
            if (barrier == null) barrier = GetComponent<Collider2D>();
            if (openIndicator == null)
                openIndicator = transform.Find("Gear socket lamp")?.GetComponent<SpriteRenderer>();
        }
        public void Configure(SpriteRenderer visual, Collider2D collider, SpriteRenderer indicator)
        {
            artwork = visual;
            barrier = collider;
            openIndicator = indicator;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (barrier != null) barrier.enabled = false;
            if (artwork != null) artwork.enabled = false;
            if (openIndicator != null) openIndicator.enabled = true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class FactoryScrapDrop : MonoBehaviour
    {
        private bool landed;
        private bool releasedGear;
        [SerializeField] private GameObject gearPrefab;
        private SpriteRenderer warning;

        private void Awake() => FactoryWorldArt.FitSpriteToNominalSize(GetComponent<SpriteRenderer>(),
            new Vector2(1.55f, 1.15f));

        public void Initialize(GameObject gear, SpriteRenderer warningArt)
        {
            gearPrefab = gear;
            warning = warningArt;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (landed) return;
            landed = true;
            if (warning != null) warning.enabled = false;
            if (gearPrefab != null && !releasedGear)
            {
                releasedGear = true;
                Instantiate(gearPrefab, transform.position + Vector3.up * .65f, Quaternion.identity);
            }
            Destroy(gameObject, 3f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!landed) other.GetComponentInParent<PlayerDeath>()?.Die();
        }
    }

    [DisallowMultipleComponent]
    public sealed class FactoryDropZone : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float warningDuration = 0.8f;
        [SerializeField, Min(0f)] private float horizontalJitter = 10f;
        [SerializeField] private GameObject scrapPrefab;
        [SerializeField] private GameObject gearPrefab;
        [SerializeField] private Sprite warningSprite;
        private bool launched;
        private float dropX;

        private void Awake()
        {
            if (scrapPrefab == null) scrapPrefab = Resources.Load<GameObject>("Factory/FallingScrap");
            if (gearPrefab == null) gearPrefab = Resources.Load<GameObject>("Factory/FactoryGear");
            if (warningSprite == null) warningSprite = Resources.Load<Sprite>("Factory/DropWarning");
        }
        public void Configure(GameObject scrap, GameObject gear, Sprite warning)
        {
            scrapPrefab = scrap;
            gearPrefab = gear;
            warningSprite = warning;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (launched || other.GetComponentInParent<PlayerDeath>() == null) return;
            ShowWarning();
        }

        private void ShowWarning()
        {
            if (launched || scrapPrefab == null) return;
            launched = true;
            dropX = transform.position.x + Random.Range(-horizontalJitter, horizontalJitter);
            var marker = new GameObject("Falling metal warning", typeof(SpriteRenderer));
            marker.transform.position = new Vector3(dropX, transform.position.y - 5.7f, 0f);
            var renderer = marker.GetComponent<SpriteRenderer>();
            renderer.sprite = warningSprite;
            FactoryWorldArt.FitSpriteToNominalSize(renderer, Vector2.one);
            renderer.color = new Color(1f, .24f, .07f, .95f);
            renderer.sortingOrder = 15;
            Invoke(nameof(Drop), warningDuration);
            Destroy(marker, warningDuration);
        }

        private void Drop()
        {
            if (scrapPrefab == null) return;
            var scrap = Instantiate(scrapPrefab, new Vector3(dropX, transform.position.y, 0f), Quaternion.identity);
            scrap.GetComponent<FactoryScrapDrop>()?.Initialize(gearPrefab, null);
        }
    }
}
