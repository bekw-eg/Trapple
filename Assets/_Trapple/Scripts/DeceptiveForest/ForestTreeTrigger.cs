using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ForestTreeTrigger : MonoBehaviour
    {
        [SerializeField] private ForestFallingTree tree;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (tree != null) tree.Trigger(other.GetComponentInParent<PlayerDeath>());
        }
    }
}
