using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Trapple.Hub.HubUIElements;

namespace Trapple.Hub
{
    public sealed class UpgradePanelController : IDisposable
    {
        private sealed class Card
        {
            public UpgradeDefinition definition;
            public VisualElement root;
            public Label level, bonus, price;
            public Button purchase;
            public Action click;
            public readonly List<VisualElement> pips = new List<VisualElement>();
        }
        private readonly List<Card> cards = new List<Card>();
        private readonly IProgressionService progression;
        private readonly IUpgradeService upgrades;
        private readonly Action<string> notify;

        public UpgradePanelController(VisualElement root, HubConfiguration config, IProgressionService progression,
            IUpgradeService upgrades, Action<string> notify)
        {
            this.progression = progression; this.upgrades = upgrades; this.notify = notify;
            var container = root.Q("upgrade-cards");
            container.Clear();
            foreach (var definition in config.upgrades)
            {
                var card = new Card { definition = definition, root = Box(container, "upgrade-card") };
                card.root.name = "upgrade-" + definition.kind.ToString().ToLowerInvariant();
                var heading = Box(card.root, "card-heading");
                Label(heading, definition.icon, "stat-icon " + definition.kind.ToString().ToLowerInvariant());
                card.level = Label(heading, "", "eyebrow");
                Label(card.root, definition.title, "card-title");
                Label(card.root, definition.description, "description");
                var pips = Box(card.root, "pips");
                for (int i = 0; i < definition.MaxLevel; i++) card.pips.Add(Box(pips, "pip"));
                card.bonus = Label(card.root, "", "next-bonus");
                card.price = Label(card.root, "", "price");
                card.purchase = Button(card.root, "УЛУЧШИТЬ", "primary");
                card.purchase.name = "buy-" + definition.kind.ToString().ToLowerInvariant();
                card.click = () => Purchase(card);
                card.purchase.clicked += card.click;
                cards.Add(card);
            }
            progression.Changed += Refresh;
            Refresh();
        }

        private void Purchase(Card card)
        {
            if (!upgrades.Purchase(card.definition)) return;
            card.root.RemoveFromClassList("purchase-success");
            card.root.schedule.Execute(() => card.root.AddToClassList("purchase-success"));
            card.root.schedule.Execute(() => card.root.RemoveFromClassList("purchase-success")).StartingIn(650);
            notify(card.definition.title + " · уровень " + progression.Level(card.definition.kind));
        }

        private void Refresh()
        {
            foreach (var card in cards)
            {
                int level = progression.Level(card.definition.kind);
                bool max = level >= card.definition.MaxLevel;
                card.level.text = "УРОВЕНЬ " + level + " / " + card.definition.MaxLevel;
                for (int i = 0; i < card.pips.Count; i++) card.pips[i].EnableInClassList("filled", i < level);
                float current = (upgrades.PreviewModifier(card.definition) - 1f) * 100f;
                card.bonus.text = max ? "Бонус +" + current.ToString("0") + "% · максимум" :
                    "+" + current.ToString("0") + "%  →  +" + (current + card.definition.percentPerLevel).ToString("0") + "%";
                card.price.text = max ? "Все улучшения открыты" : card.definition.costs[level] + "  семечек";
                card.purchase.text = max ? "MAX LEVEL" : "УЛУЧШИТЬ  ↑";
                card.purchase.SetEnabled(upgrades.CanPurchase(card.definition));
            }
        }

        public void Dispose()
        {
            progression.Changed -= Refresh;
            foreach (var card in cards) card.purchase.clicked -= card.click;
        }
    }
}
