using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    [DefaultExecutionOrder(210), RequireComponent(typeof(SpriteRenderer)), DisallowMultipleComponent]
    public sealed class AppiGroundShadow : MonoBehaviour
    {
        [SerializeField] private Collider2D character;
        [SerializeField] private SpriteRenderer characterArt;
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [SerializeField, Min(.1f)] private float maxDistance = 5f;
        [SerializeField] private Vector2 groundedSize = new Vector2(.85f, .14f);
        [SerializeField] private Vector2 airborneSize = new Vector2(.65f, .23f);
        [SerializeField, Range(0f, 1f)] private float groundedOpacity = .3f;
        [SerializeField, Range(0f, 1f)] private float airborneOpacity = .06f;
        [SerializeField, Min(.1f)] private float fadeHeight = 2.8f;
        [SerializeField, Min(.001f)] private float surfaceOffset = .025f;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        private SpriteRenderer shadow;
        public SpriteRenderer Presentation => shadow != null ? shadow : GetComponent<SpriteRenderer>();
        public float Height { get; private set; }
        public Collider2D Surface { get; private set; }

        public void Configure(Collider2D target, SpriteRenderer art) { character = target; characterArt = art; }
        private void Awake() => shadow = GetComponent<SpriteRenderer>();
        private void LateUpdate() => Refresh();
        private void OnDisable() { if (shadow != null) shadow.enabled = false; }

        public void Refresh()
        {
            if (shadow == null) shadow = GetComponent<SpriteRenderer>();
            shadow.enabled = false;
            Surface = null;
            if (character == null || !character.enabled || !character.gameObject.activeInHierarchy ||
                characterArt == null || !characterArt.enabled || !characterArt.gameObject.activeInHierarchy || shadow.sprite == null ||
                character.attachedRigidbody != null && !character.attachedRigidbody.simulated) return;
            Bounds body = character.bounds;
            const float lift = .12f;
            if (!FindSurface(new Vector2(body.center.x, body.min.y + lift), maxDistance + lift, out var hit) ||
                hit.point.y > body.min.y + lift - .001f) return;

            Height = Mathf.Max(0, body.min.y - hit.point.y);
            float t = Mathf.Clamp01(Height / Mathf.Max(.1f, fadeHeight));
            Vector2 size = Vector2.Lerp(groundedSize, airborneSize, t);
            Vector2 tangent = new Vector2(hit.normal.y, -hit.normal.x);
            // Sample the footprint along the actual surface. Shrink at ledges/curved edges;
            // never leave a complete ellipse suspended over the empty side of a platform.
            float half = size.x * .5f;
            for (int i = 0; i < 6; i++)
            {
                if (Supported(hit, tangent * half) && Supported(hit, -tangent * half)) break;
                half *= .5f;
            }
            if (half < .025f) return;
            size.x = half * 2;
            Vector2 position = hit.point + hit.normal * surfaceOffset;
            transform.position = new Vector3(position.x, position.y, character.transform.position.z);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(size.x / shadow.sprite.bounds.size.x / parentScale.x,
                size.y / shadow.sprite.bounds.size.y / parentScale.y, 1);
            Color color = shadow.color;
            color.a = Mathf.Lerp(groundedOpacity, airborneOpacity, t) * characterArt.color.a;
            shadow.color = color;
            shadow.sortingLayerID = characterArt.sortingLayerID;
            shadow.sortingOrder = characterArt.sortingOrder - 1;
            shadow.enabled = true;
            Surface = hit.collider;
        }

        private bool Supported(RaycastHit2D center, Vector2 offset)
        {
            Vector2 point = center.point + offset;
            return FindSurface(point + Vector2.up * .1f, .2f, out var support) &&
                support.collider == center.collider && Mathf.Abs(support.point.y - point.y) < .06f;
        }

        private bool FindSurface(Vector2 origin, float distance, out RaycastHit2D result)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayers);
            int count = Physics2D.Raycast(origin, Vector2.down, filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == character || hits[i].collider.attachedRigidbody == character.attachedRigidbody &&
                    character.attachedRigidbody != null || hits[i].normal.y < .65f || hits[i].distance <= .001f) continue;
                result = hits[i];
                return true;
            }
            result = default;
            return false;
        }
    }
}
