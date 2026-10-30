using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MeadowArenaCheckpoint : MonoBehaviour
    {
        [SerializeField] private MeadowBossEncounter encounter;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && !player.IsDead) encounter.CaptureCheckpoint();
        }
    }
}
