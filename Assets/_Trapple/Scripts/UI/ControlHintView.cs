using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.UI
{
    public enum ControlHint { None, Move, Jump, Sprint, Interact, Throw }

    // One screen-space slot for both tutorial and existing interaction prompts.
    public sealed class ControlHintView : MonoBehaviour
    {
        [SerializeField] private Image firstKey, secondKey;
        [SerializeField] private TMP_Text separator, caption;
        [SerializeField] private Sprite a, d, space, shift, e, f;
        public ControlHint Current { get; private set; }
        public string Caption => caption.text;
        public void SetTopOffset(float pixels)
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(0, -pixels);
        }

        public void Configure(Image first, Image second, TMP_Text slash, TMP_Text label, Sprite[] keys)
        {
            firstKey = first; secondKey = second; separator = slash; caption = label;
            a = keys[0]; d = keys[1]; space = keys[2]; shift = keys[3]; e = keys[4]; f = keys[5];
        }

        public void Show(ControlHint hint, string text = null)
        {
            string label = text ?? (hint == ControlHint.Move ? "MOVE" : hint == ControlHint.Jump ? "JUMP" :
                hint == ControlHint.Sprint ? "SPRINT" : hint == ControlHint.Throw ? "THROW" : "INTERACT");
            if (Current == hint && gameObject.activeSelf == (hint != ControlHint.None) &&
                (hint == ControlHint.None || caption.text == label)) return;
            Current = hint;
            gameObject.SetActive(hint != ControlHint.None);
            if (hint == ControlHint.None) return;
            bool movement = hint == ControlHint.Move;
            secondKey.gameObject.SetActive(movement);
            separator.gameObject.SetActive(movement);
            SetKey(firstKey, hint == ControlHint.Move ? a : hint == ControlHint.Jump ? space :
                hint == ControlHint.Sprint ? shift : hint == ControlHint.Throw ? f : e);
            if (movement) SetKey(secondKey, d);
            caption.text = label;
            // Thaleah's visible glyphs are smaller than its em box. Use measured width,
            // never shrink-to-fit; wide key art keeps its native aspect ratio.
            float labelWidth = caption.GetPreferredValues(caption.text).x + 8;
            caption.GetComponent<LayoutElement>().preferredWidth = labelWidth;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private static void SetKey(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.GetComponent<LayoutElement>().preferredWidth = 48 * sprite.rect.width / sprite.rect.height;
        }
    }
}
