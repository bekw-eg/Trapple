using UnityEngine;

namespace Trapple.DeceptiveForest
{
    // TutorialCamera follows Y without bounds; this level needs a fixed horizon above its pits.
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
        private void LateUpdate() => transform.position = Vector3.SmoothDamp(transform.position,
            Desired(), ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
    }
}
