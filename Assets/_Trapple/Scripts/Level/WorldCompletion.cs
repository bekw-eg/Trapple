using System;
using Trapple.Core;
using Trapple.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    /// <summary>Marks a scene that supplies its own victory presentation while retaining shared pause/death UI.</summary>
    public interface IWorldVictoryPresentation { }

    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class WorldCompletion : MonoBehaviour
    {
        [SerializeField] private HubConfiguration configuration;
        private static HubConfiguration sceneConfiguration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => sceneConfiguration = null;

        public void Configure(HubConfiguration value) => configuration = value;

        private void Awake()
        {
            if (configuration != null) sceneConfiguration = configuration;
            EnsureProgression(configuration);
        }

        public static IProgressionService EnsureProgression(HubConfiguration value = null)
        {
            if (HubSceneFlowController.Instance != null) return HubSceneFlowController.Instance.Progression;
            if (value == null) value = sceneConfiguration;
            if (value == null)
            {
                var source = FindAnyObjectByType<WorldCompletion>();
                if (source != null) value = source.configuration;
            }
            if (value == null) return null;
            sceneConfiguration = value;
            return HubSceneFlowController.Ensure(value, null, null).Progression;
        }

        public static bool Record(string worldId, string scenePath)
        {
            var progress = EnsureProgression();
            var world = HubSceneFlowController.Instance?.Configuration?.World(worldId);
            if (progress == null || world == null) return false;
            int level = Array.IndexOf(world.levelScenes ?? Array.Empty<string>(), scenePath);
            if (level < 0) return false;
            // The established service owns persistence and idempotence, including the tutorial reward.
            if (worldId == "meadow") progress.CompleteTutorial();
            progress.CompleteLevel(worldId, level);
            progress.CompleteBoss(worldId);
            return progress.IsWorldComplete(worldId);
        }

        public static bool TryGetNext(string scenePath, out WorldDefinition world, out int level)
        {
            world = null; level = -1;
            var flow = HubSceneFlowController.Instance;
            if (flow == null || flow.Configuration == null || flow.Configuration.worlds == null) return false;
            var worlds = flow.Configuration.worlds;
            for (int i = 0; i < worlds.Length; i++)
            {
                if (worlds[i] == null) continue;
                int current = Array.IndexOf(worlds[i].levelScenes ?? Array.Empty<string>(), scenePath);
                if (current < 0) continue;
                if (flow.CanLaunch(worlds[i], current + 1)) { world = worlds[i]; level = current + 1; return true; }
                if (!flow.Progression.IsWorldComplete(worlds[i].id)) return false;
                for (int next = i + 1; next < worlds.Length; next++)
                    if (flow.CanLaunch(worlds[next], 0)) { world = worlds[next]; level = 0; return true; }
                return false;
            }
            return false;
        }

        public static void Continue()
        {
            if (TryGetNext(SceneManager.GetActiveScene().path, out var world, out int level))
                HubSceneFlowController.Instance.TryLaunch(world, level);
            else if (HubSceneFlowController.Instance != null) HubSceneFlowController.Instance.ReturnToHub();
            else GameManager.Instance?.LoadScene(HubSceneFlowController.HubScenePath);
        }
    }
}
