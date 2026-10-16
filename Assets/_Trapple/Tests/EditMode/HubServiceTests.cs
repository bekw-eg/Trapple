using NUnit.Framework;
using Trapple.Hub;
using UnityEditor;
using UnityEngine;

namespace Trapple.Tests
{
    public sealed class HubServiceTests
    {
        private sealed class MemorySave : ISaveService
        {
            public string json;
            public string Read() => json;
            public void Write(string value) => json=value;
        }
        private HubConfiguration config;
        private MemorySave save;
        private PreviewProgressionService progression;

        [SetUp]
        public void Setup()
        {
            config=Object.Instantiate(AssetDatabase.LoadAssetAtPath<HubConfiguration>("Assets/_Trapple/Settings/HubConfiguration.asset"));
            config.previewStartingSeeds=200;
            save=new MemorySave();
            progression=new PreviewProgressionService(config,save);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(config);

        [Test]
        public void PurchaseChargesConfiguredPricesAndStopsAtMaximum()
        {
            int notifications=0; progression.Changed+=()=>notifications++;
            var definition=config.Upgrade(UpgradeKind.Speed);
            Assert.That(progression.TryUpgrade(definition),Is.True);
            Assert.That(progression.Seeds,Is.EqualTo(190));
            Assert.That(progression.TryUpgrade(definition),Is.True);
            Assert.That(progression.Seeds,Is.EqualTo(170));
            Assert.That(progression.TryUpgrade(definition),Is.True);
            Assert.That(progression.Seeds,Is.EqualTo(135));
            Assert.That(progression.TryUpgrade(definition),Is.False);
            Assert.That(progression.Level(UpgradeKind.Speed),Is.EqualTo(3));
            Assert.That(notifications,Is.EqualTo(3));
        }

        [Test]
        public void InsufficientFundsNeverChangesSaveOrLevel()
        {
            save.json="{\"version\":1,\"seeds\":9,\"equipped\":\"classic\"}";
            progression=new PreviewProgressionService(config,save);
            var upgrades=new UpgradeService(progression);
            string before=save.json;
            Assert.That(upgrades.CanPurchase(config.upgrades[0]),Is.False);
            Assert.That(upgrades.Purchase(config.upgrades[0]),Is.False);
            Assert.That(progression.Seeds,Is.EqualTo(9));
            Assert.That(progression.Level(UpgradeKind.Speed),Is.Zero);
            Assert.That(save.json,Is.EqualTo(before));
        }

        [Test]
        public void SavedUpgradesAndEquipmentSurviveServiceRecreation()
        {
            progression.TryUpgrade(config.Upgrade(UpgradeKind.Jump));
            var restored=new PreviewProgressionService(config,save);
            Assert.That(restored.Level(UpgradeKind.Jump),Is.EqualTo(1));
            Assert.That(restored.Seeds,Is.EqualTo(190));
            Assert.That(restored.EquippedCosmetic,Is.EqualTo("classic"));
            Assert.That(new UpgradeService(restored).PreviewModifier(config.Upgrade(UpgradeKind.Jump)),Is.EqualTo(1.02f).Within(.0001f));
        }

        [Test]
        public void LockedCosmeticsCannotBeEquipped()
        {
            Assert.That(progression.TryEquip(config.Cosmetic("forest")),Is.False);
            Assert.That(progression.TryEquip(config.Cosmetic("factory")),Is.False);
            Assert.That(progression.EquippedCosmetic,Is.EqualTo("classic"));
        }

        [Test]
        public void WorldUnlockContractMakesMatchingCosmeticAvailable()
        {
            config.World("forest").initiallyUnlocked=true;
            Assert.That(progression.TryEquip(config.Cosmetic("forest")),Is.True);
            Assert.That(new PreviewProgressionService(config,save).EquippedCosmetic,Is.EqualTo("forest"));
        }

        [Test]
        public void TutorialRewardIsGrantedExactlyOnceAndDoesNotLaunchPlannedWorlds()
        {
            progression.CompleteTutorial(); progression.CompleteTutorial();
            Assert.That(progression.TutorialComplete,Is.True);
            Assert.That(progression.Seeds,Is.EqualTo(200+config.tutorialReward));
            Assert.That(progression.IsWorldUnlocked(config.World("forest")),Is.False);
        }

        [TestCase("{\"version\":1,\"seeds\":-90,\"speed\":900,\"jump\":-2,\"equipped\":\"lava\"}")]
        public void InvalidSavedValuesAreClamped(string json)
        {
            save.json=json;
            var restored=new PreviewProgressionService(config,save);
            Assert.That(restored.Seeds,Is.Zero);
            Assert.That(restored.Level(UpgradeKind.Speed),Is.EqualTo(3));
            Assert.That(restored.Level(UpgradeKind.Jump),Is.Zero);
            Assert.That(restored.EquippedCosmetic,Is.EqualTo("classic"));
        }

        [TestCase("not json")]
        [TestCase("{\"version\":999,\"seeds\":500}")]
        public void UnsupportedSaveRestartsPreview(string json)
        {
            save.json=json;
            Assert.That(new PreviewProgressionService(config,save).Seeds,Is.EqualTo(200));
        }

        [Test]
        public void CallerCannotForgeCheaperUpgradeDefinition()
        {
            var forged=new UpgradeDefinition { kind=UpgradeKind.Speed,costs=new[]{0,0,0} };
            Assert.That(progression.TryUpgrade(forged),Is.False);
            Assert.That(progression.Seeds,Is.EqualTo(200));
        }
    }
}
