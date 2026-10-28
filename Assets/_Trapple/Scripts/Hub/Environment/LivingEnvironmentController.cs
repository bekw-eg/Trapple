using UnityEngine;

namespace Trapple.Hub.LivingEnvironment
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class LivingEnvironmentController : MonoBehaviour
    {
        [SerializeField] private bool wind = true;
        [SerializeField] private bool clouds = true;
        [SerializeField] private bool particles = true;
        [SerializeField] private bool warmLights = true;
        [SerializeField] private bool contactShadows = true;
        [SerializeField] private bool waterShimmer = true;
        [SerializeField] private EnvironmentWindController[] foliage = new EnvironmentWindController[0];
        [SerializeField] private ParallaxLayer[] cloudLayers = new ParallaxLayer[0];
        [SerializeField] private AmbientParticleController[] emitters = new AmbientParticleController[0];
        [SerializeField] private LightFlicker2D[] lights = new LightFlicker2D[0];
        [SerializeField] private AppiGroundShadow playerShadow;
        [SerializeField] private SpriteRenderer[] staticShadows = new SpriteRenderer[0];
        [SerializeField] private EnvironmentWaterShimmer water;

        private void OnEnable() => Apply();
        private void OnDisable() => Apply(false);
        private void OnValidate() { if (Application.isPlaying && isActiveAndEnabled) Apply(); }
        public void SetEffectsEnabled(bool value)
        {
            wind = clouds = particles = warmLights = contactShadows = waterShimmer = value;
            Apply(isActiveAndEnabled);
        }

        private void Apply(bool active = true)
        {
            foreach (var item in foliage) if (item != null) item.SetVisualsEnabled(active && wind);
            foreach (var item in cloudLayers) if (item != null) item.enabled = active && clouds;
            foreach (var item in emitters) if (item != null) item.enabled = active && particles;
            foreach (var item in lights) if (item != null) item.enabled = active && warmLights;
            if (playerShadow != null) playerShadow.enabled = active && contactShadows;
            foreach (var item in staticShadows) if (item != null) item.enabled = active && contactShadows;
            if (water != null) water.SetVisualsEnabled(active && waterShimmer);
        }
    }
}
