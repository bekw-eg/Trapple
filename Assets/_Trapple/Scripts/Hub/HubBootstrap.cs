using Trapple.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Trapple.Hub
{
    [DefaultExecutionOrder(20)]
    public sealed class HubBootstrap : MonoBehaviour
    {
        [SerializeField] private HubConfiguration configuration;
        [SerializeField] private HubUIController ui;
        [SerializeField] private PlayerController player;
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private VisualTreeAsset returnTree;
        private IProgressionService progression;
        private SpriteRenderer hero;

        private void Start()
        {
            if (configuration == null || ui == null || player == null || panelSettings == null || returnTree == null)
            {
                Debug.LogError("Hub is missing a dependency. Rebuild with Trapple/Hub/Rebuild Hub.", this);
                enabled = false;
                return;
            }
            var flow = HubSceneFlowController.Ensure(configuration, panelSettings, returnTree);
            progression = flow.Progression;
            hero = player.GetComponent<SpriteRenderer>();
            progression.Changed += ApplyCosmetic;
            ApplyCosmetic();
            ui.Initialize(configuration, progression, flow.Upgrades, flow, player);
        }

        private void ApplyCosmetic()
        {
            var cosmetic = configuration.Cosmetic(progression.EquippedCosmetic);
            if (hero != null && cosmetic != null)
            {
                hero.color = cosmetic.tint;
                if (cosmetic.sprite != null) hero.sprite = cosmetic.sprite;
                var animator = player.GetComponent<PlayerSpriteAnimator>();
                if (animator != null) animator.SetAppearance(cosmetic.sprite);
            }
        }

        private void OnDestroy()
        {
            if (progression != null) progression.Changed -= ApplyCosmetic;
        }
    }
}
