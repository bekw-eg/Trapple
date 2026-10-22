using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Workshop
{
    [RequireComponent(typeof(Image))]
    public sealed class IrisTransitionController : MonoBehaviour
    {
        [SerializeField] private Material template;
        [SerializeField, Min(.01f)] private float duration = .65f;
        [SerializeField, Range(.001f,.05f)] private float softness = .012f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0,0,1,1);
        private Material material;
        private Image overlay;
        public float Radius { get; private set; }
        public Vector2 Center { get; private set; } = new Vector2(.5f,.5f);
        public bool IsCovered => overlay != null && overlay.enabled && Radius < 0;
        public bool IsVisible => overlay != null && overlay.enabled;

        public void Configure(Material value) => template = value;
        private void EnsureMaterial()
        {
            if (material != null) return;
            overlay = GetComponent<Image>();
            material = new Material(template);
            overlay.material = material;
        }
        public void Clear()
        {
            if (overlay == null) overlay = GetComponent<Image>();
            overlay.enabled = false;
        }
        public IEnumerator Animate(bool close, Vector2 viewportCenter)
        {
            EnsureMaterial();
            Center = new Vector2(Mathf.Clamp01(viewportCenter.x),Mathf.Clamp01(viewportCenter.y));
            overlay.enabled = true;
            float elapsed = 0;
            do
            {
                float aspect = (float)Screen.width / Mathf.Max(1,Screen.height);
                // Recompute on resize. Distance uses screen height, so the aperture stays circular.
                float full = new Vector2(Mathf.Max(Center.x,1-Center.x)*aspect,Mathf.Max(Center.y,1-Center.y)).magnitude + softness;
                float t = easing.Evaluate(Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration)));
                Radius = Mathf.Lerp(close ? full : -softness,close ? -softness : full,t);
                material.SetVector("_Center",Center); material.SetFloat("_Aspect",aspect);
                material.SetFloat("_Softness",softness); material.SetFloat("_Radius",Radius);
                if (elapsed >= duration) break;
                elapsed = Mathf.Min(duration,elapsed+Time.unscaledDeltaTime);
                yield return null;
            } while (true);
            if (!close) Clear();
        }
        private void Awake() => Clear();
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
