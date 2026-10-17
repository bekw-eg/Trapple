using System;
using UnityEngine;

namespace Trapple.Hub
{
    public interface ISaveService
    {
        string Read();
        void Write(string json);
    }

    public interface IProgressionService
    {
        event Action Changed;
        int Seeds { get; }
        string EquippedCosmetic { get; }
        int Level(UpgradeKind kind);
        bool TutorialComplete { get; }
        bool IsWorldUnlocked(WorldDefinition world);
        bool IsCosmeticUnlocked(CosmeticDefinition cosmetic);
        bool TryUpgrade(UpgradeDefinition definition);
        bool TryEquip(CosmeticDefinition cosmetic);
        void CompleteTutorial();
    }

    public interface IUpgradeService
    {
        bool CanPurchase(UpgradeDefinition definition);
        bool Purchase(UpgradeDefinition definition);
        float PreviewModifier(UpgradeDefinition definition);
    }

    public interface ISceneFlowController
    {
        bool CanLaunch(WorldDefinition world, int level);
        bool TryLaunch(WorldDefinition world, int level);
        void ReturnToHub();
    }

    // A deliberately isolated local UI-preview save, never the production economy save.
    public sealed class PreviewSaveService : ISaveService
    {
        public const string Key = "Trapple.HubPreview.v1";
        public string Read() => PlayerPrefs.GetString(Key, "");
        public void Write(string json)
        {
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    internal sealed class PreviewProgress
    {
        public int version = 1;
        public int seeds;
        public int speed, agility, jump;
        public bool tutorialComplete;
        public string equipped = "classic";
    }

    public sealed class PreviewProgressionService : IProgressionService
    {
        private readonly HubConfiguration configuration;
        private readonly ISaveService save;
        private readonly PreviewProgress data;
        public event Action Changed;
        public int Seeds => data.seeds;
        public string EquippedCosmetic => data.equipped;
        public bool TutorialComplete => data.tutorialComplete;

        public PreviewProgressionService(HubConfiguration configuration, ISaveService save)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            PreviewProgress loaded = null;
            try
            {
                string json = save.Read();
                if (!string.IsNullOrWhiteSpace(json)) loaded = JsonUtility.FromJson<PreviewProgress>(json);
            }
            catch (ArgumentException) { /* Invalid preview saves restart with safe defaults. */ }
            data = loaded != null && loaded.version == 1 ? loaded :
                new PreviewProgress { seeds = configuration.previewStartingSeeds };
            data.seeds = Mathf.Clamp(data.seeds, 0, 1000000);
            data.speed = ClampLevel(UpgradeKind.Speed, data.speed);
            data.agility = ClampLevel(UpgradeKind.Agility, data.agility);
            data.jump = ClampLevel(UpgradeKind.Jump, data.jump);
            if (!IsCosmeticUnlocked(configuration.Cosmetic(data.equipped))) data.equipped = "classic";
        }

        private int ClampLevel(UpgradeKind kind, int value) => Mathf.Clamp(value, 0, configuration.Upgrade(kind)?.MaxLevel ?? 0);
        public int Level(UpgradeKind kind) => kind == UpgradeKind.Speed ? data.speed : kind == UpgradeKind.Agility ? data.agility : data.jump;
        public bool IsWorldUnlocked(WorldDefinition world) => world != null && world.initiallyUnlocked;
        public bool IsCosmeticUnlocked(CosmeticDefinition cosmetic) => cosmetic != null &&
            (cosmetic.initiallyUnlocked || configuration.World(cosmetic.worldId)?.initiallyUnlocked == true);

        public bool TryUpgrade(UpgradeDefinition definition)
        {
            // Accept only the configured definition; UI callers cannot supply their own prices.
            if (definition == null || configuration.Upgrade(definition.kind) != definition) return false;
            int level = Level(definition.kind);
            if (level >= definition.MaxLevel || definition.costs[level] < 0 || Seeds < definition.costs[level]) return false;
            data.seeds -= definition.costs[level];
            switch (definition.kind)
            {
                case UpgradeKind.Speed: data.speed++; break;
                case UpgradeKind.Agility: data.agility++; break;
                case UpgradeKind.Jump: data.jump++; break;
            }
            Commit();
            return true;
        }

        public bool TryEquip(CosmeticDefinition cosmetic)
        {
            if (cosmetic == null || configuration.Cosmetic(cosmetic.id) != cosmetic || !IsCosmeticUnlocked(cosmetic)) return false;
            data.equipped = cosmetic.id;
            Commit();
            return true;
        }

        public void CompleteTutorial()
        {
            if (data.tutorialComplete) return;
            data.tutorialComplete = true;
            data.seeds = (int)Math.Min(1000000L, (long)data.seeds + configuration.tutorialReward);
            Commit();
        }

        private void Commit()
        {
            save.Write(JsonUtility.ToJson(data));
            Changed?.Invoke();
        }
    }

    // Upgrade data is real and saved, but platforming modifiers are preview-only until balanced.
    public sealed class UpgradeService : IUpgradeService
    {
        private readonly IProgressionService progression;
        public UpgradeService(IProgressionService progression) => this.progression = progression;
        public bool CanPurchase(UpgradeDefinition definition)
        {
            if (definition == null) return false;
            int level = progression.Level(definition.kind);
            return level < definition.MaxLevel && definition.costs[level] >= 0 && progression.Seeds >= definition.costs[level];
        }
        public bool Purchase(UpgradeDefinition definition) => progression.TryUpgrade(definition);
        public float PreviewModifier(UpgradeDefinition definition) => 1f + progression.Level(definition.kind) * definition.percentPerLevel / 100f;
    }
}
