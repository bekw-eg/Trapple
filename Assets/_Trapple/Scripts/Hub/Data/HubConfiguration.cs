using System;
using UnityEngine;

namespace Trapple.Hub
{
    public enum HubScreen { None, Workshop, Worlds, Wardrobe, Settings }
    public enum UpgradeKind { Speed, Agility, Jump }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        public UpgradeKind kind;
        public string title;
        public string description;
        public string icon;
        [Min(0f)] public float percentPerLevel = 2f;
        public int[] costs = { 10, 20, 35 };
        public int MaxLevel => costs == null ? 0 : costs.Length;
    }

    [Serializable]
    public sealed class WorldDefinition
    {
        public string id, title, subtitle, description, boss;
        public Sprite illustration;
        [Min(1)] public int plannedLevels = 3;
        public bool initiallyUnlocked;
        // Only actual, enabled build scenes can be launched. Empty entries are planned levels.
        public string[] levelScenes = Array.Empty<string>();
    }

    [Serializable]
    public sealed class CosmeticDefinition
    {
        public string id, title, description, worldId;
        public Sprite sprite;
        public Color tint = Color.white;
        public bool initiallyUnlocked;
    }

    [CreateAssetMenu(menuName = "Trapple/Hub configuration")]
    public sealed class HubConfiguration : ScriptableObject
    {
        [Min(0)] public int previewStartingSeeds = 80;
        [Min(0)] public int tutorialReward = 15;
        public Sprite heroSprite;
        public UpgradeDefinition[] upgrades;
        public WorldDefinition[] worlds;
        public CosmeticDefinition[] cosmetics;
        public UpgradeDefinition Upgrade(UpgradeKind kind) => Array.Find(upgrades, value => value.kind == kind);
        public WorldDefinition World(string id) => Array.Find(worlds, value => value.id == id);
        public CosmeticDefinition Cosmetic(string id) => Array.Find(cosmetics, value => value.id == id);
    }
}
