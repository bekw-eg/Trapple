using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [Tooltip("Apparent camera motion: 0 = infinitely distant, 1 = gameplay plane.")]
        [SerializeField, Range(0f, 1f)] private float depth = .12f;
        [SerializeField] private Vector2 drift = new Vector2(.035f, 0);
        [Tooltip("Wrap beyond both camera edges. Zero disables wrapping.")]
        [SerializeField, Min(0f)] private float wrapWidth = 60f;
        private Vector3 origin;
        private Vector3 cameraOrigin;
        private Vector2 driftOffset;
        private bool initialized;

        public void Configure(Transform cameraTransform) => view = cameraTransform;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized || view == null) return;
            origin = transform.position;
            cameraOrigin = view.position;
            initialized = true;
        }

        private void LateUpdate()
        {
            Initialize();
            if (!initialized || view == null) return;
            driftOffset += drift * Time.deltaTime;
            if (wrapWidth > 0) driftOffset.x = Mathf.Repeat(driftOffset.x + wrapWidth * .5f, wrapWidth) - wrapWidth * .5f;
            Vector3 cameraDelta = view.position - cameraOrigin;
            cameraDelta.z = 0;
            transform.position = origin + cameraDelta * (1f - depth) + (Vector3)driftOffset;
        }

        private void OnDisable()
        {
            if (initialized) transform.position = origin;
            driftOffset = Vector2.zero;
        }
    }
}
