using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    /// <summary>UV wind for independent foliage. Never moves the transform or its collider.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnvironmentWindController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 1.2f;
        [SerializeField, Range(0f, .02f)] private float strength = .004f;
        [SerializeField, Min(.01f)] private float frequency = 5f;
        [SerializeField] private float phase;
        [Tooltip("Normalized sprite height below which pixels stay fixed. Use .6 for a tree canopy.")]
        [SerializeField, Range(0f, .95f)] private float fixedBase = .15f;
        private SpriteRenderer target;
        private MaterialPropertyBlock properties;
        private bool visualsEnabled = true;
        private static readonly int Wind = Shader.PropertyToID("_Wind");
        private static readonly int Region = Shader.PropertyToID("_SpriteUV");
        private static readonly int Base = Shader.PropertyToID("_FixedBase");

        public void Configure(float amount, float baseHeight, float offset)
        {
            strength = amount;
            fixedBase = baseHeight;
            phase = offset;
            Apply();
        }

        public void SetVisualsEnabled(bool value) { visualsEnabled = value; Apply(); }
        private void OnEnable() => Apply();
        private void OnDisable() => Apply(false);
        private void OnValidate() { if (isActiveAndEnabled) Apply(); }

        private void Apply(bool active = true)
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target == null || target.sprite == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Rect rect = target.sprite.textureRect;
            Texture texture = target.sprite.texture;
            target.GetPropertyBlock(properties);
            properties.SetVector(Region, new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height));
            properties.SetVector(Wind, new Vector4(speed, active && visualsEnabled ? strength : 0, frequency, phase));
            properties.SetFloat(Base, fixedBase);
            target.SetPropertyBlock(properties);
        }
    }
}
