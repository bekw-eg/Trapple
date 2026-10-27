using TMPro;
using UnityEngine;

namespace Trapple.UI
{
    public sealed class TimedNotice : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text caption;
        [SerializeField, Min(.05f)] private float fadeIn = .25f, fadeOut = .5f;
        private float elapsed, duration;
        public bool IsVisible => gameObject.activeInHierarchy && group.alpha > 0;

        public void Configure(CanvasGroup canvasGroup, TMP_Text label) { group = canvasGroup; caption = label; }
        public void Show(string text, float seconds)
        {
            caption.text = text; elapsed = 0; duration = seconds; group.alpha = 0;
        }
        public void Clear() { duration = 0; group.alpha = 0; gameObject.SetActive(false); }
        public void Tick(bool playing)
        {
            gameObject.SetActive(playing && elapsed < duration);
            if (!playing || elapsed >= duration) return;
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / fadeIn), Mathf.Clamp01((duration - elapsed) / fadeOut));
        }
    }
}
