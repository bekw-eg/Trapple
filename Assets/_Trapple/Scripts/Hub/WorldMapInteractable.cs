using System;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Hub
{
    // Input remains in HubInteractionController. These colliders never render or block Appi.
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class WorldMapInteractable : HubInteractable
    {
        [Serializable]
        public struct WorldZone
        {
            public string worldId;
            public PolygonCollider2D area;
        }

        [SerializeField] private Collider2D proximity;
        [SerializeField] private WorldZone[] zones;
        private PlayerController actor;
        public bool IsInteracting => actor != null;

        public void Configure(Collider2D range, WorldZone[] worlds)
        {
            proximity = range;
            zones = worlds;
        }

        public override bool InRange(Vector2 position) => proximity != null && proximity.enabled &&
            proximity.gameObject.activeInHierarchy && proximity.OverlapPoint(position);

        public bool BeginInteraction(PlayerController player)
        {
            if (!isActiveAndEnabled || IsInteracting || player == null || !player.ControlEnabled ||
                !InRange(player.transform.position)) return false;
            actor = player;
            actor.SetControlEnabled(false);
            return true;
        }

        public void EndInteraction()
        {
            if (actor == null) return;
            var previous = actor;
            actor = null;
            var death = previous.GetComponent<PlayerDeath>();
            if (death == null || !death.IsDead) previous.SetControlEnabled(true);
        }

        public string WorldAtScreenPoint(Camera camera, Vector2 screenPoint)
        {
            if (!IsInteracting || camera == null || zones == null) return null;
            var plane = new Plane(transform.forward, transform.position);
            var ray = camera.ScreenPointToRay(screenPoint);
            if (!plane.Raycast(ray, out float distance)) return null;
            Vector2 point = ray.GetPoint(distance);
            foreach (var zone in zones)
                if (zone.area != null && zone.area.enabled && zone.area.gameObject.activeInHierarchy &&
                    zone.area.OverlapPoint(point)) return zone.worldId;
            return null;
        }

        private void OnDisable() => EndInteraction();
    }
}
