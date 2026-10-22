using Trapple.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Trapple.Hub
{
    // Owns the isolated preview services across Hub -> Tutorial -> Hub, including death reloads.
    public sealed class HubSceneFlowController : MonoBehaviour, ISceneFlowController
    {
        public const string HubScenePath = "Assets/_Trapple/Scenes/Hub/Hub.unity";
        public static HubSceneFlowController Instance { get; private set; }
        public IProgressionService Progression { get; private set; }
        public IUpgradeService Upgrades { get; private set; }
        public HubConfiguration Configuration => configuration;
        private HubConfiguration configuration;
        private PanelSettings panel;
        private VisualTreeAsset returnTree;
        private UIDocument returnDocument;
        private Button returnButton;
        private TutorialGoal goal;
        private bool tutorialTrip;
        private bool completionReported;
        private string launchedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public static HubSceneFlowController Ensure(HubConfiguration configuration, PanelSettings panel, VisualTreeAsset returnTree)
        {
            if (Instance != null) return Instance;
            var flow = new GameObject("Hub preview services").AddComponent<HubSceneFlowController>();
            flow.configuration = configuration;
            flow.panel = panel;
            flow.returnTree = returnTree;
            flow.Progression = new PreviewProgressionService(configuration, new PreviewSaveService());
            flow.Upgrades = new UpgradeService(flow.Progression);
            return flow;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
        }

        public bool CanLaunch(WorldDefinition world, int level) => world != null && Progression != null &&
            configuration.World(world.id) == world && Progression.IsWorldUnlocked(world) &&
            level >= 0 && world.levelScenes != null && level < world.levelScenes.Length && !string.IsNullOrEmpty(world.levelScenes[level]) &&
            Application.CanStreamedLevelBeLoaded(world.levelScenes[level]);

        public bool TryLaunch(WorldDefinition world, int level)
        {
            if (!CanLaunch(world, level)) return false;
            tutorialTrip = world.id == "meadow";
            launchedScene = world.levelScenes[level];
            SceneManager.LoadScene(launchedScene);
            return true;
        }

        public void ReturnToHub()
        {
            if (!Application.CanStreamedLevelBeLoaded(HubScenePath)) return;
            tutorialTrip = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(HubScenePath);
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            ClearReturnUI();
            goal = null;
            completionReported = false;
            ApplyCosmetic();
            if (!tutorialTrip || scene.path != launchedScene) { tutorialTrip = false; return; }
            goal = FindAnyObjectByType<TutorialGoal>();
            if (panel == null || returnTree == null) return;
            var obj = new GameObject("Return to orchard");
            obj.transform.SetParent(transform, false);
            returnDocument = obj.AddComponent<UIDocument>();
            returnDocument.panelSettings = panel;
            returnDocument.visualTreeAsset = returnTree;
            returnButton = returnDocument.rootVisualElement.Q<Button>("return-hub");
            returnButton.focusable = false;
            returnDocument.rootVisualElement.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
            returnDocument.rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Space) evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            returnButton.clicked += ReturnToHub;
        }

        private void Update()
        {
            if (returnDocument != null)
                returnDocument.rootVisualElement.style.display = Trapple.Core.GameManager.Instance != null &&
                    Trapple.Core.GameManager.Instance.State == Trapple.Core.GameState.Paused ? DisplayStyle.None : DisplayStyle.Flex;
            if (!tutorialTrip || completionReported || goal == null || !goal.IsComplete) return;
            completionReported = true;
            if (!Progression.TutorialComplete) Progression.CompleteTutorial();
            if (returnDocument != null)
                returnDocument.rootVisualElement.Q<Label>("trip-status").text = "Поляна пройдена · можно вернуться домой";
        }

        private void ClearReturnUI()
        {
            if (returnButton != null) returnButton.clicked -= ReturnToHub;
            returnButton = null;
            if (returnDocument != null) Destroy(returnDocument.gameObject);
            returnDocument = null;
        }

        private void ApplyCosmetic()
        {
            var cosmetic = configuration?.Cosmetic(Progression?.EquippedCosmetic);
            if (cosmetic == null || cosmetic.sprite == null) return;
            foreach (var player in FindObjectsByType<Trapple.Player.PlayerController>())
            {
                var renderer = player.GetComponent<SpriteRenderer>();
                if (renderer != null) { renderer.sprite = cosmetic.sprite; renderer.color = cosmetic.tint; }
                player.GetComponent<Trapple.Player.PlayerSpriteAnimator>()?.SetAppearance(cosmetic.sprite);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            ClearReturnUI();
            SceneManager.sceneLoaded -= SceneLoaded;
            Instance = null;
        }
    }
}
