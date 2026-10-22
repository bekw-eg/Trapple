using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Trapple.Hub.HubUIElements;

namespace Trapple.Hub
{
    public sealed class WardrobeController : IDisposable
    {
        private readonly List<(Button button, Action action, CosmeticDefinition cosmetic)> options = new List<(Button, Action, CosmeticDefinition)>();
        private readonly IProgressionService progression;
        private readonly HubConfiguration configuration;
        private readonly VisualElement preview;
        private readonly Label name, description, state;
        private readonly Button equip;
        private readonly Action<string> notify;
        private CosmeticDefinition selected;

        public WardrobeController(VisualElement root, HubConfiguration configuration, IProgressionService progression, Action<string> notify)
        {
            this.configuration = configuration; this.progression = progression; this.notify = notify;
            preview = root.Q("hero-preview");
            if (configuration.heroSprite != null) preview.style.backgroundImage = new StyleBackground(configuration.heroSprite);
            name = root.Q<Label>("cosmetic-name"); description = root.Q<Label>("cosmetic-description"); state = root.Q<Label>("cosmetic-state");
            equip = root.Q<Button>("equip"); equip.clicked += Equip;
            root.Q("cosmetics").Clear();
            foreach (var cosmetic in configuration.cosmetics)
            {
                var button = Button(root.Q("cosmetics"), "", "cosmetic-option");
                button.name = "cosmetic-" + cosmetic.id;
                var swatch = Box(button, "cosmetic-swatch");
                swatch.style.backgroundImage = new StyleBackground(cosmetic.sprite != null ? cosmetic.sprite : configuration.heroSprite);
                swatch.style.unityBackgroundImageTintColor = cosmetic.tint;
                var copy = Box(button, "cosmetic-copy");
                Label(copy, cosmetic.title, "card-title");
                Label(copy, cosmetic.description, "small");
                var tag = Label(button, "", "cosmetic-tag");
                tag.name = "tag";
                Action action = () => Select(cosmetic);
                button.clicked += action;
                options.Add((button, action, cosmetic));
            }
            progression.Changed += Refresh;
            Select(configuration.Cosmetic(progression.EquippedCosmetic) ?? configuration.cosmetics[0]);
        }

        private void Select(CosmeticDefinition cosmetic)
        {
            selected = cosmetic;
            Refresh();
        }

        private void Equip()
        {
            if (progression.TryEquip(selected)) notify(selected.title + " · образ выбран");
        }

        private void Refresh()
        {
            if (selected == null) return;
            preview.style.backgroundImage = new StyleBackground(selected.sprite != null ? selected.sprite : configuration.heroSprite);
            preview.style.unityBackgroundImageTintColor = selected.tint;
            name.text = selected.title; description.text = selected.description;
            bool unlocked = progression.IsCosmeticUnlocked(selected);
            bool equipped = selected.id == progression.EquippedCosmetic;
            state.text = unlocked ? "UNLOCKED" : "LOCKED · связан с миром";
            equip.text = equipped ? "EQUIPPED  ✓" : unlocked ? "НАДЕТЬ  →" : "LOCKED";
            equip.SetEnabled(unlocked && !equipped);
            foreach (var entry in options)
            {
                entry.button.EnableInClassList("selected", entry.cosmetic == selected);
                entry.button.Q<Label>("tag").text = entry.cosmetic.id == progression.EquippedCosmetic ? "НАДЕТО" :
                    progression.IsCosmeticUnlocked(entry.cosmetic) ? "ОТКРЫТО" : "LOCKED";
            }
        }

        public void Dispose()
        {
            progression.Changed -= Refresh;
            equip.clicked -= Equip;
            foreach (var entry in options) entry.button.clicked -= entry.action;
        }
    }
}
