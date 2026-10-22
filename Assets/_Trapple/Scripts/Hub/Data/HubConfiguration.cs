using System;
using UnityEngine;

namespace Trapple.Hub
{
    public enum HubScreen { None, Workshop, Worlds, Wardrobe, Settings }
    public enum UpgradeKind { Speed, Agility, Jump }
    public enum CosmeticUnlockRule { Manual, WorldUnlocked, WorldCompleted }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        public UpgradeKind kind;
        public string title;
        public string description;
        public string icon;
        [Min(0f)] public float percentPerLevel = 2f;
        [Tooltip("Three positive prices. Missing, zero or negative entries disable purchase.")]
        public int[] costs = { -1, -1, -1 };
        public int MaxLevel => 3;
        public int Cost(int level) => costs != null && level >= 0 && level < Math.Min(3, costs.Length) ? costs[level] : -1;
    }

    [Serializable]
    public sealed class WorldDefinition
    {
        public string id, title, subtitle, description, boss;
        public Sprite illustration;
        [Min(1)] public int plannedLevels = 3;
        public bool initiallyUnlocked;
        public string requiredCompletedWorldId;
        public bool completionRequiresBoss;
        [Tooltip("Leave 0 until the game's star rules exist.")]
        [Min(0)] public int maximumStars;
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
        public CosmeticUnlockRule unlockRule;
        [Tooltip("UI preview fallback only; never installed as a gameplay animation.")]
        public Sprite previewSprite;
    }

    [CreateAssetMenu(menuName = "Trapple/Hub configuration")]
    public sealed class HubConfiguration : ScriptableObject
    {
        [Min(0)] public int previewStartingSeeds;
        [Min(0)] public int tutorialReward;
        [HideInInspector] public int illustratedDefaultsVersion;
        public Sprite heroSprite;
        public UpgradeDefinition[] upgrades;
        public WorldDefinition[] worlds;
        public CosmeticDefinition[] cosmetics;
        public UpgradeDefinition Upgrade(UpgradeKind kind) => upgrades == null ? null : Array.Find(upgrades, value => value != null && value.kind == kind);
        public WorldDefinition World(string id) => worlds == null ? null : Array.Find(worlds, value => value != null && value.id == id);
        public CosmeticDefinition Cosmetic(string id) => cosmetics == null ? null : Array.Find(cosmetics, value => value != null && value.id == id);
    }
}
