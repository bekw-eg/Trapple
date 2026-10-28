using UnityEngine;

namespace Trapple.Hub
{
    public sealed class HubAmbience : MonoBehaviour
    {
        public enum Motion { Sway = 0, Cloud = 1, Leaf = 3, Parallax = 4 }
        [SerializeField] private Motion motion;
        [SerializeField] private float amplitude = 0.1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float phase;
        [SerializeField] private Transform cameraTransform;
        private Vector3 origin;
        private float started;
        private void Awake()
        {
            origin = transform.localPosition;
            started = Time.unscaledTime;
        }
        private void Update()
        {
            float t = (Time.unscaledTime - started) * speed + phase;
            switch (motion)
            {
                case Motion.Sway:
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * amplitude);
                    break;
                case Motion.Cloud:
                    transform.localPosition = origin + new Vector3(Mathf.Sin(t * 0.15f) * amplitude, Mathf.Sin(t * 0.25f) * 0.06f, 0f);
                    break;
                case Motion.Leaf:
                    transform.localPosition = origin + new Vector3(Mathf.Sin(t * 0.6f) * amplitude, -Mathf.Repeat(t * 0.3f, 5f), 0f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, t * 18f);
                    break;
                case Motion.Parallax:
                    if (cameraTransform != null) transform.localPosition = origin + Vector3.right * cameraTransform.position.x * amplitude;
                    break;
            }
        }
    }
}
