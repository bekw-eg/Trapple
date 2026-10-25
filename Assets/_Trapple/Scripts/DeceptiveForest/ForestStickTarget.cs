using System.Collections;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ForestStickTarget : MonoBehaviour
    {
        [SerializeField] private Transform artwork;
        public bool IsOpen { get; private set; }
        public bool TryBreak(ForestStick stick)
        {
            if (IsOpen || stick == null || !stick.IsFlying) return false;
            IsOpen = true;
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(Drop());
            return true;
        }
        private IEnumerator Drop()
        {
            if (artwork == null) yield break;
            Vector3 start = artwork.localPosition;
            var renderers = artwork.GetComponentsInChildren<SpriteRenderer>();
            for (float t = 0; t < .45f; t += Time.deltaTime)
            {
                artwork.localPosition = start + Vector3.down * t * t * 10f;
                artwork.localRotation = Quaternion.Euler(0, 0, -t * 65f);
                foreach (var renderer in renderers)
                {
                    Color color = renderer.color; color.a = 1f - t / .45f; renderer.color = color;
                }
                yield return null;
            }
            artwork.gameObject.SetActive(false);
        }
    }
}
