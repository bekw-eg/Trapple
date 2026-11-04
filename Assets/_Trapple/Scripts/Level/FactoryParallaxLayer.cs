using UnityEngine;

namespace Trapple.Level
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class FactoryParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField, Range(0f, .9f)] private float parallax = .25f;
        [SerializeField] private float centerY = 4.5f;
        [SerializeField, Min(1f)] private float panelHeight = 5f;
        [SerializeField] private SpriteRenderer[] panels;

        public void Configure(Camera camera, Sprite sprite, float amount, float y, float height, int sortingOrder, Color tint)
        {
            view = camera;
            parallax = Mathf.Clamp(amount, 0f, .9f);
            centerY = y;
            panelHeight = height;
            panels = new SpriteRenderer[3];
            for (int i = 0; i < panels.Length; i++)
            {
                var panel = new GameObject("Parallax panel " + (i + 1), typeof(SpriteRenderer));
                panel.transform.SetParent(transform, false);
                panels[i] = panel.GetComponent<SpriteRenderer>();
                panels[i].sprite = sprite;
                panels[i].sortingOrder = sortingOrder;
                panels[i].color = tint;
            }
        }

        private void LateUpdate()
        {
            if (view == null || panels == null || panels.Length != 3 || panels[0].sprite == null) return;
            float width = view.orthographicSize * 2f * view.aspect + 3f;
            float shift = view.transform.position.x * parallax;
            int cell = Mathf.FloorToInt((view.transform.position.x - shift) / width);
            for (int i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                panel.transform.position = new Vector3((cell + i - 1) * width + shift, centerY, 0f);
                panel.transform.localScale = new Vector3(width / panel.sprite.bounds.size.x,
                    panelHeight / panel.sprite.bounds.size.y, 1f);
            }
        }
    }
}
