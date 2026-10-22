using Trapple.Player;
using UnityEngine;

namespace Trapple.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private HubUIController ui;
        [SerializeField] private HubInteractable[] interactables;
        public IHubInteractable Nearest { get; private set; }

        private void Update()
        {
            RefreshTarget();
            if (ui == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) ui.HandleEscape();
            else if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        }

        public void RefreshTarget()
        {
            IHubInteractable next = null;
            float distance = float.MaxValue;
            if (player != null && ui != null && interactables != null && !ui.IsOpen && player.ControlEnabled)
            {
                foreach (var candidate in interactables)
                {
                    if (candidate == null || !candidate.isActiveAndEnabled || !candidate.InRange(player.transform.position)) continue;
                    float value = ((Vector2)candidate.transform.position - (Vector2)player.transform.position).sqrMagnitude;
                    if (value < distance) { next = candidate; distance = value; }
                }
            }
            if (Nearest == next) return;
            Nearest = next;
            ui?.SetPrompt(next?.Prompt);
        }

        public bool TryInteract()
        {
            RefreshTarget();
            if (Nearest == null || ui == null || ui.IsOpen) return false;
            ui.Open(Nearest.Screen);
            return true;
        }
    }
}
