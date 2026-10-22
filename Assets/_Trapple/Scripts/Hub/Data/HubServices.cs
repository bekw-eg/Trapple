using System;
using System.Collections.Generic;
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
        string SelectedCosmetic { get; }
        int Level(UpgradeKind kind);
        bool TutorialComplete { get; }
        bool IsWorldUnlocked(WorldDefinition world);
        bool IsCosmeticUnlocked(CosmeticDefinition cosmetic);
        bool TryUpgrade(UpgradeDefinition definition);
        bool TryEquip(CosmeticDefinition cosmetic);
        void CompleteTutorial();
        bool IsWorldComplete(string id);
        int CompletedLevels(string id);
        int Stars(string id);
        bool CompleteLevel(string worldId, int level);
        bool CompleteBoss(string worldId);
        bool SetStars(string worldId, int stars);
        bool GrantCosmetic(string id);
        bool SelectCosmetic(string id);
        bool AddSeeds(int amount);
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

    // The existing hub key is retained so there is one source of progression across both UIs.
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
        public int version = 2;
        public int seeds;
        public int speed, agility, jump;
        public bool tutorialComplete;
        public string equipped = "classic";
        public string selected = "classic";
        public List<string> completedLevels = new List<string>();
        public List<string> completedBosses = new List<string>();
        public List<string> grantedCosmetics = new List<string>();
        public List<SavedStars> stars = new List<SavedStars>();
    }

    [Serializable] internal sealed class SavedStars { public string worldId; public int count; }

    // Future gameplay systems explicitly opt in; UI purchases never mutate player physics.
    public interface IUpgradeApplier { void ApplyUpgrades(IProgressionService progression, HubConfiguration configuration); }

    public sealed class PreviewProgressionService : IProgressionService
    {
        private readonly HubConfiguration configuration;
        private readonly ISaveService save;
        private readonly PreviewProgress data;
        public event Action Changed;
        public int Seeds => data.seeds;
        public string EquippedCosmetic => data.equipped;
        public string SelectedCosmetic => data.selected;
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
            data = loaded != null && (loaded.version == 1 || loaded.version == 2) ? loaded :
                new PreviewProgress { seeds = configuration.previewStartingSeeds };
            if (data.version == 1) data.selected = data.equipped;
            data.version = 2;
            data.completedLevels ??= new List<string>();
            data.completedBosses ??= new List<string>();
            data.grantedCosmetics ??= new List<string>();
            data.stars ??= new List<SavedStars>();
            if (data.tutorialComplete && !data.completedLevels.Contains("meadow:0")) data.completedLevels.Add("meadow:0");
            data.seeds = Mathf.Clamp(data.seeds, 0, 1000000);
            data.speed = ClampLevel(UpgradeKind.Speed, data.speed);
            data.agility = ClampLevel(UpgradeKind.Agility, data.agility);
            data.jump = ClampLevel(UpgradeKind.Jump, data.jump);
            if (!IsCosmeticUnlocked(configuration.Cosmetic(data.equipped)))
                data.equipped = Array.Find(configuration.cosmetics ?? Array.Empty<CosmeticDefinition>(), IsCosmeticUnlocked)?.id ?? "";
            if (configuration.Cosmetic(data.selected) == null) data.selected = data.equipped;
        }

        private int ClampLevel(UpgradeKind kind, int value) => Mathf.Clamp(value, 0, configuration.Upgrade(kind)?.MaxLevel ?? 0);
        public int Level(UpgradeKind kind) => kind == UpgradeKind.Speed ? data.speed : kind == UpgradeKind.Agility ? data.agility : data.jump;
        public bool IsWorldUnlocked(WorldDefinition world) => world != null && configuration.World(world.id) == world &&
            (world.initiallyUnlocked || (!string.IsNullOrEmpty(world.requiredCompletedWorldId) && IsWorldComplete(world.requiredCompletedWorldId)));
        public bool IsCosmeticUnlocked(CosmeticDefinition cosmetic) => cosmetic != null &&
            configuration.Cosmetic(cosmetic.id) == cosmetic && (cosmetic.initiallyUnlocked || data.grantedCosmetics.Contains(cosmetic.id) ||
            (cosmetic.unlockRule == CosmeticUnlockRule.WorldUnlocked && IsWorldUnlocked(configuration.World(cosmetic.worldId))) ||
            (cosmetic.unlockRule == CosmeticUnlockRule.WorldCompleted && IsWorldComplete(cosmetic.worldId)));

        public int CompletedLevels(string id)
        {
            var world = configuration.World(id);
            if (world == null) return 0;
            int count = 0;
            for (int i = 0; i < world.plannedLevels; i++) if (data.completedLevels.Contains(id + ":" + i)) count++;
            return count;
        }
        public bool IsWorldComplete(string id)
        {
            var world = configuration.World(id);
            return world != null && world.plannedLevels > 0 && CompletedLevels(id) >= world.plannedLevels &&
                (!world.completionRequiresBoss || data.completedBosses.Contains(id));
        }
        public int Stars(string id) => Mathf.Clamp(data.stars.Find(value => value != null && value.worldId == id)?.count ?? 0,
            0, configuration.World(id)?.maximumStars ?? 0);
        public bool CompleteLevel(string worldId, int level)
        {
            var world = configuration.World(worldId);
            string key = worldId + ":" + level;
            if (!IsWorldUnlocked(world) || level < 0 || level >= world.plannedLevels || data.completedLevels.Contains(key)) return false;
            data.completedLevels.Add(key);
            Commit(); return true;
        }
        public bool CompleteBoss(string worldId)
        {
            var world = configuration.World(worldId);
            if (!IsWorldUnlocked(world) || !world.completionRequiresBoss || CompletedLevels(worldId) < world.plannedLevels || data.completedBosses.Contains(worldId)) return false;
            data.completedBosses.Add(worldId); Commit(); return true;
        }
        public bool SetStars(string worldId, int stars)
        {
            var world = configuration.World(worldId);
            if (!IsWorldUnlocked(world) || world.maximumStars <= 0 || stars < 0 || stars > world.maximumStars) return false;
            var entry = data.stars.Find(value => value != null && value.worldId == worldId);
            if (entry == null) data.stars.Add(new SavedStars { worldId = worldId, count = stars });
            else entry.count = Math.Max(entry.count, stars);
            Commit(); return true;
        }
        public bool GrantCosmetic(string id)
        {
            if (configuration.Cosmetic(id) == null || data.grantedCosmetics.Contains(id)) return false;
            data.grantedCosmetics.Add(id); Commit(); return true;
        }
        public bool SelectCosmetic(string id)
        {
            if (configuration.Cosmetic(id) == null) return false;
            data.selected = id; Commit(); return true;
        }
        public bool AddSeeds(int amount)
        {
            if (amount <= 0) return false;
            data.seeds = (int)Math.Min(1000000L, (long)data.seeds + amount); Commit(); return true;
        }

        public bool TryUpgrade(UpgradeDefinition definition)
        {
            // Accept only the configured definition; UI callers cannot supply their own prices.
            if (definition == null || configuration.Upgrade(definition.kind) != definition) return false;
            int level = Level(definition.kind);
            int cost = definition.Cost(level);
            if (level >= definition.MaxLevel || cost <= 0 || Seeds < cost) return false;
            data.seeds -= cost;
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
            data.selected = cosmetic.id;
            Commit();
            return true;
        }

        public void CompleteTutorial()
        {
            if (data.tutorialComplete) return;
            data.tutorialComplete = true;
            if (!data.completedLevels.Contains("meadow:0")) data.completedLevels.Add("meadow:0");
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
            return level < definition.MaxLevel && definition.Cost(level) > 0 && progression.Seeds >= definition.Cost(level);
        }
        public bool Purchase(UpgradeDefinition definition) => progression.TryUpgrade(definition);
        public float PreviewModifier(UpgradeDefinition definition) => 1f + progression.Level(definition.kind) * definition.percentPerLevel / 100f;
    }
}
