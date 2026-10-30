using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private ForestGuardianEncounter encounter;
        [SerializeField] private bool checkpoint;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player == null || player.IsDead || encounter == null) return;
            if (checkpoint) encounter.CaptureCheckpoint(); else encounter.EnterArena();
        }
    }
}
