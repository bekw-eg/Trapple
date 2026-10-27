using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ForestBaitTrigger : MonoBehaviour
    {
        [SerializeField] private ForestDisappearingPlatform surface;
        private void OnTriggerEnter2D(Collider2D other) => Activate(other);
        private void OnTriggerStay2D(Collider2D other) => Activate(other);
        private void Activate(Collider2D other)
        {
            if (surface != null) surface.Trigger(other.GetComponentInParent<PlayerDeath>());
        }
    }
}
