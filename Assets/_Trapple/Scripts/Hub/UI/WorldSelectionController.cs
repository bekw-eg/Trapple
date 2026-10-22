using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Trapple.Hub.HubUIElements;

namespace Trapple.Hub
{
    public sealed class WorldSelectionController : IDisposable
    {
        private sealed class Card
        {
            public WorldDefinition definition;
            public VisualElement root;
            public Label state, progress;
            public Button select;
            public Action click;
        }
        private readonly List<Card> cards = new List<Card>();
        private readonly List<(Button button, Action action)> levelButtons = new List<(Button, Action)>();
        private readonly IProgressionService progression;
        private readonly ISceneFlowController flow;
        private readonly VisualElement levelList;
        private readonly Label detailTitle, detailDescription;
        private readonly Action<string> notify;
        private WorldDefinition selected;

        public WorldSelectionController(VisualElement root, HubConfiguration config, IProgressionService progression,
            ISceneFlowController flow, Action<string> notify)
        {
            this.progression = progression; this.flow = flow; this.notify = notify;
            levelList = root.Q("level-list");
            detailTitle = root.Q<Label>("world-detail-title"); detailDescription = root.Q<Label>("world-detail-description");
            root.Q("world-cards").Clear();
            foreach (var definition in config.worlds)
            {
                var card = new Card { definition = definition, root = Box(root.Q("world-cards"), "world-card") };
                card.root.name = "world-" + definition.id;
                var art = Box(card.root, "world-art");
                if (definition.illustration != null) art.style.backgroundImage = new StyleBackground(definition.illustration);
                Label(art, "0" + (cards.Count + 1), "world-number");
                var padlock=Box(art,"world-lock");
                Box(padlock,"lock-shackle");
                Box(padlock,"lock-body");
                var cosmetic=Array.Find(config.cosmetics,value=>value.worldId==definition.id);
                if(cosmetic?.sprite!=null)
                {
                    var hero=Box(art,"world-hero");
                    hero.style.backgroundImage=new StyleBackground(cosmetic.sprite);
                }
                card.state = Label(art, "", "world-state");
                var body = Box(card.root, "world-body");
                Label(body, definition.subtitle, "eyebrow");
                Label(body, definition.title, "world-title");
                Label(body, definition.plannedLevels + (definition.plannedLevels == 1 ? " уровень · без босса" : " уровня + босс"), "small");
                card.progress = Label(body, "", "world-progress");
                card.select = Button(body, "ВЫБРАТЬ  →", "secondary");
                card.select.name = "select-" + definition.id;
                card.click = () => Select(definition);
                card.select.clicked += card.click;
                cards.Add(card);
            }
            progression.Changed += Refresh;
            Refresh();
            Select(config.worlds[0]);
        }

        private void Refresh()
        {
            foreach (var card in cards)
            {
                bool unlocked = progression.IsWorldUnlocked(card.definition);
                bool complete = card.definition.id == "meadow" && progression.TutorialComplete;
                card.state.text = complete ? "COMPLETED" : unlocked ? "UNLOCKED" : "LOCKED";
                card.root.EnableInClassList("locked", !unlocked);
                card.progress.text = "ПРОГРЕСС  " + (complete ? "1" : "0") + " / " + card.definition.plannedLevels;
                // Locked cards still reveal their description; launch buttons remain disabled.
                card.select.text = unlocked ? "ВЫБРАТЬ  →" : "О МИРЕ  →";
            }
            if (selected != null) Select(selected);
        }

        private void ClearLevels()
        {
            foreach (var entry in levelButtons) entry.button.clicked -= entry.action;
            levelButtons.Clear(); levelList.Clear();
        }

        public void Select(WorldDefinition world)
        {
            if (world == null) return;
            selected = world;
            foreach (var card in cards) card.root.EnableInClassList("active-world", card.definition == world);
            detailTitle.text = world.title;
            detailDescription.text = world.description + (string.IsNullOrEmpty(world.boss) ? "" : " · Босс: " + world.boss);
            ClearLevels();
            for (int i = 0; i < world.plannedLevels; i++)
            {
                int level = i;
                bool available = flow.CanLaunch(world, level);
                var button = Button(levelList, "0" + (level + 1) + (available ? "   ИГРАТЬ →" : "   НЕДОСТУПЕН"), available ? "primary level-button" : "secondary level-button");
                button.name = "launch-" + world.id + "-" + level;
                button.SetEnabled(available);
                Action action = () => { if (!flow.TryLaunch(world, level)) notify("Этот уровень пока недоступен"); };
                button.clicked += action; levelButtons.Add((button, action));
            }
            if (!progression.IsWorldUnlocked(world)) Label(levelList, "Мир запланирован · сцены ещё не добавлены", "small planned-note");
        }

        public void Dispose()
        {
            progression.Changed -= Refresh;
            foreach (var card in cards) card.select.clicked -= card.click;
            ClearLevels();
        }
    }
}
