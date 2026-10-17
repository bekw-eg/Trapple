using UnityEngine;

namespace Trapple.Hub
{
    [RequireComponent(typeof(Camera))]
    public sealed class HubCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.55f;
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-6.5f, 6.5f);
        [SerializeField] private float height = 3.1f;
        private Vector3 velocity;
        private void LateUpdate()
        {
            if (target == null) return;
            var desired = new Vector3(Mathf.Clamp(target.position.x * 0.55f, horizontalBounds.x, horizontalBounds.y), height, -10f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
}
