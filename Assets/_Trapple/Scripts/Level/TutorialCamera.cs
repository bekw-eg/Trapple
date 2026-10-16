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
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target != null)
                transform.position = Vector3.SmoothDamp(transform.position, target.position + offset,
                    ref velocity, smoothTime);
        }
    }
}
