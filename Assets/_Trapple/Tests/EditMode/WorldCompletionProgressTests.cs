using NUnit.Framework;
using Trapple.Hub;
using UnityEngine;

namespace Trapple.Tests
{
    public sealed class WorldCompletionProgressTests
    {
        private sealed class MemorySave : ISaveService
        {
            public string json;
            public string Read() => json;
            public void Write(string value) => json = value;
        }
        private HubConfiguration configuration;
        private MemorySave save;

        [SetUp]
        public void SetUp()
        {
            configuration = ScriptableObject.CreateInstance<HubConfiguration>();
            configuration.worlds = new[]
            {
                new WorldDefinition { id = "meadow", plannedLevels = 1, initiallyUnlocked = true, completionRequiresBoss = true },
                new WorldDefinition { id = "forest", plannedLevels = 1, requiredCompletedWorldId = "meadow", completionRequiresBoss = true },
                new WorldDefinition { id = "factory", plannedLevels = 3, requiredCompletedWorldId = "forest", completionRequiresBoss = true }
            };
            configuration.cosmetics = System.Array.Empty<CosmeticDefinition>();
            save = new MemorySave();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(configuration);

        [Test]
        public void ForestRequiresRouteAndBossAndPersistsCompletionAndNextWorldUnlock()
        {
            var progression = new PreviewProgressionService(configuration, save);
            progression.CompleteTutorial();
            Assert.That(progression.IsWorldComplete("meadow"), Is.False);
            Assert.That(progression.CompleteBoss("meadow"), Is.True);
            Assert.That(progression.IsWorldUnlocked(configuration.World("forest")), Is.True);
            Assert.That(progression.CompleteBoss("forest"), Is.False, "Boss completion cannot bypass the route.");
            Assert.That(progression.CompleteLevel("forest", 0), Is.True);
            Assert.That(progression.IsWorldComplete("forest"), Is.False);
            Assert.That(progression.CompleteBoss("forest"), Is.True);
            Assert.That(progression.CompleteBoss("forest"), Is.False);
            var restored = new PreviewProgressionService(configuration, save);
            Assert.That(restored.IsWorldComplete("forest"), Is.True);
            Assert.That(restored.IsWorldUnlocked(configuration.World("factory")), Is.True);
            Assert.That(restored.CompletedLevels("forest"), Is.EqualTo(1));
        }
    }
}
