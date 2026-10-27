using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Trapple.UI
{
    [RequireComponent(typeof(Selectable)), DisallowMultipleComponent]
    public sealed class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Range(.08f, .15f)] private float duration = .11f;
        private Selectable control;
        private Coroutine tween;
        private bool hovered, pressed, focused, chosen;
        public bool Chosen => chosen;
        private void Awake() => control = GetComponent<Selectable>();
        public void SetChosen(bool value)
        {
            chosen=value;
            if(control==null) control=GetComponent<Selectable>();
            var colors=control.colors; colors.normalColor=chosen?new Color(1,.93f,.65f):Color.white; control.colors=colors;
            Animate();
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; Animate(); }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Animate(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = true; Animate(); } }
        public void OnPointerUp(PointerEventData e) { pressed = false; Animate(); }
        public void OnSelect(BaseEventData e) { focused = true; Animate(); }
        public void OnDeselect(BaseEventData e) { focused = pressed = false; Animate(); }
        private void OnEnable() { control = GetComponent<Selectable>(); Animate(); }
        private void Animate()
        {
            if (!isActiveAndEnabled) return;
            if (tween != null) StopCoroutine(tween);
            tween = StartCoroutine(Transition());
        }
        private IEnumerator Transition()
        {
            bool available = control != null && control.IsInteractable();
            float scale = !available ? 1f : pressed ? .97f : hovered || focused || chosen ? 1.025f : 1f;
            Vector3 start = transform.localScale;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, Vector3.one * scale, Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }
            transform.localScale = Vector3.one * scale;
            tween = null;
        }
        private void OnDisable()
        {
            if (tween != null) StopCoroutine(tween);
            tween = null; hovered = pressed = focused = false; transform.localScale = Vector3.one;
        }
    }
}
