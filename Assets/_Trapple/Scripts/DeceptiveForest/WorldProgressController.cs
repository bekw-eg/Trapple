using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    /// <summary>Projects the player onto an authored ordered polyline, including vertical/backtracking routes.</summary>
    public sealed class WorldProgressController : MonoBehaviour
    {
        [SerializeField] private PlayerDeath player;
        [SerializeField] private Vector2[] route;
        private float[] distances;
        private float length;
        public float Progress { get; private set; }
        private void Awake()
        {
            distances = new float[route == null ? 0 : route.Length];
            for (int i = 1; i < distances.Length; i++) distances[i] = distances[i - 1] + Vector2.Distance(route[i - 1], route[i]);
            length = distances.Length > 0 ? distances[distances.Length - 1] : 0;
        }
        private void Update()
        {
            if (player == null || player.IsDead || Time.timeScale <= 0) return;
            Progress = Mathf.Max(Progress, Project(player.transform.position));
        }
        public float Project(Vector2 position)
        {
            if (length <= 0 || route.Length < 2) return 0;
            float best = float.PositiveInfinity, travelled = 0;
            for (int i = 1; i < route.Length; i++)
            {
                Vector2 segment = route[i] - route[i - 1];
                float t = Mathf.Clamp01(Vector2.Dot(position - route[i - 1], segment) / Mathf.Max(.001f, segment.sqrMagnitude));
                float distance = (position - route[i - 1] - segment * t).sqrMagnitude;
                // At crossings the earlier segment wins. Explicit route gates should be used for overlapping paths.
                if (distance >= best) continue;
                best = distance; travelled = distances[i - 1] + segment.magnitude * t;
            }
            return Mathf.Clamp01(travelled / length);
        }
        public void ReachArena() => Progress = 1;
    }
}
