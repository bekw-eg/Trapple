using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(Camera))]
    public sealed class BossArenaCameraController : MonoBehaviour
    {
        [SerializeField] private ForestCamera follow;
        [SerializeField, Min(.1f)] private float smoothTime = .55f;
        private Vector3 target, velocity;
        private float originalSize, targetSize, sizeVelocity, shake, phase;
        private bool owned;
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); originalSize = view.orthographicSize; }
        public void Focus(Vector3 point, float size)
        {
            owned = true;
            if (follow != null) follow.enabled = false;
            target = new Vector3(point.x, point.y, -10); targetSize = size;
        }
        public void Shake(float amount) => shake = Mathf.Max(shake, Mathf.Min(.16f, amount));
        public void Release()
        {
            owned = false; shake = 0;
            if (view != null) view.orthographicSize = originalSize;
            if (follow != null) follow.enabled = true;
        }
        private void LateUpdate()
        {
            if (!owned || Time.deltaTime <= 0) return;
            Vector3 p = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, targetSize, ref sizeVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            phase += Time.deltaTime * 48;
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * .4f);
            transform.position = p + new Vector3(Mathf.Sin(phase), Mathf.Cos(phase * 1.3f), 0) * shake;
        }
        private void OnDisable() => Release();
    }
}
