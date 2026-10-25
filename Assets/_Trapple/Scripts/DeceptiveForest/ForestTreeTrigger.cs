using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ForestTreeTrigger : MonoBehaviour
    {
        [SerializeField] private ForestFallingTree tree;
        [SerializeField] private bool requireLanding;
        private void OnTriggerEnter2D(Collider2D other) => Activate(other);
        private void OnTriggerStay2D(Collider2D other) => Activate(other);
        private void Activate(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || (requireLanding && !player.IsGrounded)) return;
            if (tree != null) tree.Trigger(player.GetComponent<PlayerDeath>());
        }
    }
}
