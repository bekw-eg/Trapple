using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    [RequireComponent(typeof(SpriteRenderer)), DisallowMultipleComponent]
    public sealed class EnvironmentWaterShimmer : MonoBehaviour
    {
        [SerializeField, Range(0f, .06f)] private float strength = .018f;
        [SerializeField, Min(0f)] private float speed = .55f;
        private MaterialPropertyBlock properties;
        private SpriteRenderer target;
        private bool visualsEnabled = true;
        private static readonly int Shimmer = Shader.PropertyToID("_Shimmer");
        public void SetVisualsEnabled(bool value) { visualsEnabled = value; Apply(); }
        private void OnEnable() => Apply();
        private void OnDisable() => Apply(false);
        private void OnValidate() { if (isActiveAndEnabled) Apply(); }
        private void Apply(bool active = true)
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            target.GetPropertyBlock(properties);
            properties.SetVector(Shimmer, new Vector4(active && visualsEnabled ? strength : 0, speed, 0, 0));
            target.SetPropertyBlock(properties);
        }
    }
}
