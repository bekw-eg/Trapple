using UnityEngine;

namespace Trapple.Bosses
{
    /// <summary>Boss-specific barrages. Every falling object commits to its marked position before release.</summary>
    [DisallowMultipleComponent]
    public sealed class GuardianSpecialAttacks : MonoBehaviour
    {
        [SerializeField] private Sprite giantBranch, apple, root, leaf, markerSprite;
        [SerializeField, Range(40, 60)] private int rainBranchCount = 50;
        [SerializeField, Min(.65f)] private float rainWarning = .75f;
        [SerializeField, Min(.65f)] private float rainVolleyInterval = .72f;
        private readonly Reservation[] reservations = new Reservation[20];
        private ForestGuardianController owner;
        private ForestGuardianAnimation rig;
        private GuardianAttack attack;
        private Vector2 arena;
        private float ground, elapsed, nextEmission;
        private int emitted, planned;
        private SpriteRenderer heldBranch;
        private float previewAge, previewDuration;
        public bool IsRunning { get; private set; }
        public float Duration { get; private set; }
        public int EmittedCount => emitted;
        public int PlannedCount => planned;
        public int PendingCount
        {
            get { int count = 0; foreach (var r in reservations) if (r != null && r.active && !r.released) count++; return count; }
        }

        private sealed class Reservation
        {
            public SpriteRenderer marker;
            public Vector2 position;
            public float remaining, warning;
            public bool active;
            public bool released;
            public ForestGuardianHazard falling;
            public int launchId;
            public GuardianAttack kind;
        }

        private void Awake()
        {
            owner = GetComponent<ForestGuardianController>(); rig = GetComponent<ForestGuardianAnimation>();
            heldBranch = new GameObject("Raised giant branch", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            heldBranch.transform.SetParent(transform, false); heldBranch.sprite = giantBranch; heldBranch.sortingOrder = 19; heldBranch.enabled = false;
            for (int i = 0; i < reservations.Length; i++)
            {
                var marker = new GameObject("Reserved landing " + i, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                marker.transform.SetParent(transform, false); marker.sprite = markerSprite; marker.sortingOrder = 15; marker.enabled = false;
                reservations[i] = new Reservation { marker = marker };
            }
        }

        public void Preview(GuardianAttack kind, float seconds)
        {
            previewAge = 0f; previewDuration = Mathf.Max(.01f, seconds);
            heldBranch.enabled = kind == GuardianAttack.BranchRain;
        }

        private void LateUpdate()
        {
            if (heldBranch == null || !heldBranch.enabled || !owner.CanDamage) return;
            previewAge += Time.deltaTime;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(previewAge / previewDuration));
            heldBranch.transform.localPosition = new Vector3(0f, 4.3f + rise * 3.1f, 0f);
            heldBranch.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (giantBranch != null)
                heldBranch.transform.localScale = new Vector3(.7f / giantBranch.bounds.size.x, 4.3f / giantBranch.bounds.size.y, 1f);
        }

        public void Begin(GuardianAttack kind, Vector2 bounds, float floor)
        {
            Clear(); attack = kind; arena = bounds; ground = floor; elapsed = nextEmission = 0f; emitted = planned = 0;
            Duration = kind == GuardianAttack.BranchRain ? (Mathf.Ceil(rainBranchCount / 5f) - 1f) * rainVolleyInterval + rainWarning + 1.15f :
                kind == GuardianAttack.AppleBarrage ? 8.5f : kind == GuardianAttack.RootWave ? 3.8f : 3.8f;
            IsRunning = true;
            Tick(0f);
        }

        // The combat controller calls Tick only while gameplay is live, so pause freezes reservations too.
        public void Tick(float dt)
        {
            if (!IsRunning || !owner.CanDamage) return;
            elapsed += dt;
            foreach (var r in reservations)
            {
                if (!r.active) continue;
                if (r.released)
                {
                    if (r.falling == null || !r.falling.gameObject.activeSelf || r.falling.LaunchId != r.launchId)
                    { r.active = false; r.marker.enabled = false; }
                    continue;
                }
                r.remaining -= dt;
                r.marker.color = new Color(1f, .57f, .12f, .5f + .45f * Mathf.Abs(Mathf.Sin(r.remaining * 20f)));
                if (r.remaining <= 0f) ReleaseReservation(r);
            }
            if (elapsed >= nextEmission)
            {
                if (attack == GuardianAttack.BranchRain && planned < rainBranchCount) RainVolley();
                else if (attack == GuardianAttack.AppleBarrage && emitted < 6) ThrowApple();
                else if (attack == GuardianAttack.RootWave && planned < 11) AdvanceRoots();
                else if (attack == GuardianAttack.LeafFan && emitted < 9) ThrowFan();
            }
            if (elapsed >= Duration && PendingCount == 0 && owner.ActiveHazardCount == 0) IsRunning = false;
        }

        private void RainVolley()
        {
            // Five distinct lanes around Appi's *current* position; later movement never retargets a reservation.
            float width = arena.y - arena.x - 1.4f;
            float center = Mathf.Clamp(owner.Player.transform.position.x, arena.x + .7f, arena.y - .7f);
            for (int n = 0; n < 5 && planned < rainBranchCount; n++)
            {
                float x = arena.x + .7f + Mathf.Repeat(center - arena.x - .7f + (n - 2) * 3f, width);
                Reserve(GuardianAttack.BranchRain, new Vector2(x, ground + .06f), rainWarning);
            }
            nextEmission += rainVolleyInterval;
            rig.PulseLeaves();
        }

        private void AdvanceRoots()
        {
            float span = Mathf.Max(1f, owner.transform.position.x - 2f - (arena.x + 1f));
            float x = owner.transform.position.x - 2f - span * planned / 10f;
            Reserve(GuardianAttack.RootWave, new Vector2(x, ground + .06f), .65f);
            nextEmission += .24f;
            rig.PulseLeaves();
        }

        private void Reserve(GuardianAttack kind, Vector2 position, float warning)
        {
            foreach (var r in reservations)
            {
                if (r.active) continue;
                var surface = Physics2D.Raycast(new Vector2(position.x, ground + 3f), Vector2.down, 4f, 1 << 6);
                if (surface.collider != null && surface.normal.y > .8f) position.y = surface.point.y + .06f;
                r.position = position; r.kind = kind; r.remaining = r.warning = warning; r.active = true; r.released = false; r.falling = null;
                r.marker.transform.position = position;
                Vector2 bounds = markerSprite != null ? (Vector2)markerSprite.bounds.size : Vector2.one;
                r.marker.transform.localScale = new Vector3(1.05f / bounds.x, .22f / bounds.y, 1f);
                r.marker.color = new Color(1f, .65f, .14f, .8f); r.marker.enabled = true;
                planned++; return;
            }
            Debug.LogError("Guardian landing reservation pool exhausted.", this);
        }

        private void ReleaseReservation(Reservation r)
        {
            r.active = false; r.marker.enabled = false;
            if (r.kind == GuardianAttack.BranchRain)
            {
                var hazard = owner.Launch(giantBranch, new Vector2(r.position.x, ground + 10.5f),
                    new Vector2(.65f, 2.7f), Vector2.down * 2f, 3f, attack);
                if (hazard != null)
                {
                    hazard.SetBallistics(24f, r.position.y - .06f, false); emitted++;
                    r.falling = hazard; r.released = true; r.active = true; r.marker.enabled = true;
                    r.launchId = hazard.LaunchId;
                }
            }
            else
            {
                owner.Launch(root, new Vector2(r.position.x, r.position.y - .06f + .8f), new Vector2(1.25f, 1.6f),
                    Vector2.zero, .43f, GuardianAttack.RootWave);
                owner.SpecialContact(GuardianAttack.RootWave, r.position); emitted++;
            }
        }

        private void ThrowApple()
        {
            var hazard = owner.Launch(apple, new Vector2(owner.transform.position.x - 1.2f, ground + 5.8f),
                new Vector2(.85f, .85f), new Vector2(-3.8f - (emitted % 2) * .6f, 1.3f), 5.5f, attack);
            if (hazard != null) { hazard.SetBallistics(15f, ground, true); emitted++; }
            nextEmission += .56f; rig.PulseLeaves();
        }

        private void ThrowFan()
        {
            for (int lane = 0; lane < 3; lane++)
            {
                owner.Launch(leaf, new Vector2(owner.transform.position.x - 2.1f, ground + 3.9f),
                    new Vector2(.85f, .45f), new Vector2(-7.5f, -2.6f + lane * 2.6f), 2.5f, attack);
                emitted++;
            }
            owner.SpecialContact(GuardianAttack.LeafFan, new Vector2(owner.transform.position.x - 2.1f, ground + 3.9f));
            rig.PulseLeaves(); nextEmission += .65f;
        }

        public void Clear()
        {
            IsRunning = false;
            if (heldBranch != null) heldBranch.enabled = false;
            foreach (var r in reservations)
                if (r != null) { r.active = false; if (r.marker != null) r.marker.enabled = false; }
        }
        private void OnDisable() => Clear();
        private void OnValidate() => rainBranchCount = Mathf.Clamp(rainBranchCount, 40, 60);
    }
}
