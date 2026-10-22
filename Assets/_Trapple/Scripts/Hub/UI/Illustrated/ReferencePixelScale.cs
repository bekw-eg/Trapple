using UnityEngine;

namespace Trapple.Hub.Illustrated
{
    [ExecuteAlways]
    public sealed class ReferencePixelScale : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceSize;
        public void SetReferenceSize(Vector2 size) { referenceSize=size; Resize(); }
        private void OnRectTransformDimensionsChange() => Resize();
        private void LateUpdate() => Resize();
        private void Resize()
        {
            if (referenceSize.x <= 0 || transform.parent is not RectTransform parent) return;
            float scale = parent.rect.width / referenceSize.x;
            transform.localScale = new Vector3(scale,scale,1);
        }
    }
}
