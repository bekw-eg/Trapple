using Trapple.Traps;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    // Presentation only: FallingPlatform remains responsible for all collision/timing.
    [RequireComponent(typeof(FallingPlatform))]
    public sealed class ForestPlatformWarning : MonoBehaviour
    {
        [SerializeField] private Transform artwork;
        private FallingPlatform platform;
        private Vector3 origin;
        private void Awake() { platform = GetComponent<FallingPlatform>(); if (artwork != null) origin = artwork.localPosition; }
        private void Update()
        {
            if (artwork == null || !platform.IsTriggered) return;
            artwork.localPosition = origin + (platform.HasFallen ? Vector3.zero : Vector3.right * Mathf.Sin(Time.time * 45f) * .025f);
        }
    }
}
