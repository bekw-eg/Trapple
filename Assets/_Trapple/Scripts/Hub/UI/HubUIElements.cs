using UnityEngine.UIElements;

namespace Trapple.Hub
{
    internal static class HubUIElements
    {
        public static Label Label(VisualElement parent, string text, string style)
        {
            var label = new Label(text);
            foreach (string name in style.Split(' ')) label.AddToClassList(name);
            parent.Add(label);
            return label;
        }
        public static VisualElement Box(VisualElement parent, string style)
        {
            var element = new VisualElement();
            foreach (string name in style.Split(' ')) element.AddToClassList(name);
            parent.Add(element);
            return element;
        }
        public static Button Button(VisualElement parent, string text, string style)
        {
            var button = new Button { text = text };
            foreach (string name in style.Split(' ')) button.AddToClassList(name);
            parent.Add(button);
            return button;
        }
    }
}
