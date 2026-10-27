using UnityEngine;

namespace Trapple.DeceptiveForest
{
    // Keep the forest horizon fixed above pits, including during death and changes of aspect ratio.
    [RequireComponent(typeof(Camera))]
    public sealed class ForestCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 levelBounds = new Vector2(-4f, 80f);
        [SerializeField] private float height = 3.4f;
        [SerializeField, Min(.01f)] private float smoothTime = .2f;
        private Camera view;
        private Vector3 velocity;
        private void Awake() { view = GetComponent<Camera>(); transform.position = Desired(); }
        private Vector3 Desired()
        {
            float half = view.orthographicSize * view.aspect;
            float x = target == null ? levelBounds.x + half : target.position.x + 2f;
            float min = levelBounds.x + half, max = levelBounds.y - half;
            return new Vector3(min > max ? (levelBounds.x + levelBounds.y) * .5f : Mathf.Clamp(x, min, max), height, -10f);
        }
        private void LateUpdate()
        {
            var p = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            float half = view.orthographicSize * view.aspect;
            p.x = levelBounds.y - levelBounds.x < 2 * half ? (levelBounds.x + levelBounds.y) * .5f :
                Mathf.Clamp(p.x, levelBounds.x + half, levelBounds.y - half);
            transform.position = p;
        }
    }
}
