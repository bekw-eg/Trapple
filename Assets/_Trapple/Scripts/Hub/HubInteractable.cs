using UnityEngine;

namespace Trapple.Hub
{
    public interface IHubInteractable
    {
        string Prompt { get; }
        HubScreen Screen { get; }
        string WorldId { get; }
        bool InRange(Vector2 position);
    }

    [DisallowMultipleComponent]
    public class HubInteractable : MonoBehaviour, IHubInteractable
    {
        [SerializeField] private HubScreen screen;
        [SerializeField] private string prompt;
        [SerializeField] private string worldId;
        [SerializeField, Min(0.1f)] private float radius = 2f;
        public string Prompt => prompt;
        public HubScreen Screen => screen;
        public string WorldId => worldId;
        public void SetWorld(string id) => worldId = id;
        public virtual bool InRange(Vector2 position) => ((Vector2)transform.position - position).sqrMagnitude <= radius * radius;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.88f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
