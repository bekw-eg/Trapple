using Trapple.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Trapple.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private HubUIController ui;
        [SerializeField] private HubInteractable[] interactables;
        public IHubInteractable Nearest { get; private set; }
        public WorldMapInteractable ActiveMap { get; private set; }

        private void OnEnable()
        {
            if (ui != null) ui.BeforeOpen += EndMapInteraction;
        }

        private void OnDisable()
        {
            if (ui != null) ui.BeforeOpen -= EndMapInteraction;
            EndMapInteraction();
        }

        private void Update()
        {
            RefreshTarget();
            if (ui == null) return;
            if (ActiveMap != null && ActiveMap.IsInteracting)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) EndMapInteraction();
                else if (Input.GetMouseButtonDown(0) &&
                    (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                    TrySelectWorld(Input.mousePosition);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ui.HandleEscape();
            else if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        }

        public void RefreshTarget()
        {
            IHubInteractable next = null;
            float distance = float.MaxValue;
            if (player != null && ui != null && interactables != null && !ui.IsOpen && player.ControlEnabled &&
                (ActiveMap == null || !ActiveMap.IsInteracting))
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
            if (Nearest is WorldMapInteractable map)
            {
                if (!map.BeginInteraction(player)) return false;
                ActiveMap = map;
                RefreshTarget();
                return true;
            }
            if (Nearest.Screen == HubScreen.Worlds && !string.IsNullOrEmpty(Nearest.WorldId))
                ui.OpenWorld(Nearest.WorldId);
            else ui.Open(Nearest.Screen);
            return true;
        }

        public bool TrySelectWorld(Vector2 screenPoint)
        {
            if (ActiveMap == null || ui == null || ui.IsOpen) return false;
            string world = ActiveMap.WorldAtScreenPoint(Camera.main, screenPoint);
            if (string.IsNullOrEmpty(world)) return false;
            // OpenWorld invokes BeforeOpen, handing control back before the existing modal captures it.
            ui.OpenWorld(world);
            return true;
        }

        public void EndMapInteraction()
        {
            if (ActiveMap != null) ActiveMap.EndInteraction();
            ActiveMap = null;
        }
    }
}
