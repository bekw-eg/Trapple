using UnityEngine;

namespace Trapple.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TutorialCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 levelBounds;
        [SerializeField] private float height = 3.4f;
        private Vector3 velocity;
        private Camera view;

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (target != null && useBounds) transform.position = Desired();
        }

        private Vector3 Desired()
        {
            if (!useBounds) return target.position + offset;
            float half = view.orthographicSize * view.aspect;
            float min = levelBounds.x + half, max = levelBounds.y - half;
            return new Vector3(min > max ? (levelBounds.x + levelBounds.y) * .5f :
                Mathf.Clamp(target.position.x + 2, min, max), height, -10);
        }

        private void LateUpdate()
        {
            if (target != null)
                transform.position = Vector3.SmoothDamp(transform.position, Desired(),
                    ref velocity, smoothTime);
            if (useBounds && view != null)
            {
                float half = view.orthographicSize * view.aspect;
                var p = transform.position;
                p.x = levelBounds.y - levelBounds.x < 2 * half ? (levelBounds.x + levelBounds.y) * .5f :
                    Mathf.Clamp(p.x, levelBounds.x + half, levelBounds.y - half);
                p.y = height;
                transform.position = p;
            }
        }
    }
}
