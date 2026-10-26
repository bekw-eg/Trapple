using UnityEngine;

namespace Trapple.Level
{
    /// <summary>Three recycled panels cover any horizontal camera position without a map-length texture.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class RepeatingLevelBackdrop : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField, Range(0, 1)] private float parallax = .9f;
        [SerializeField] private float height = 3.4f;
        [SerializeField] private SpriteRenderer[] panels;
        private void LateUpdate()
        {
            if (view == null || panels == null || panels.Length != 3) return;
            float panelHeight = Mathf.Max(20, view.orthographicSize * 2 + 4);
            float width = Mathf.Max(46, view.orthographicSize * 2 * view.aspect + 4);
            float shift = view.transform.position.x * parallax;
            int cell = Mathf.FloorToInt((view.transform.position.x - shift) / width);
            for (int i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                panel.transform.position = new Vector3((cell + i - 1) * width + shift, height, 10);
                // Tiny overlap removes subpixel cracks; alternating panels mirror the same edge at each seam.
                panel.transform.localScale = new Vector3((width + .04f) / panel.sprite.bounds.size.x,
                    panelHeight / panel.sprite.bounds.size.y, 1);
                panel.flipX = ((cell + i - 1) & 1) != 0;
            }
        }
    }
}
