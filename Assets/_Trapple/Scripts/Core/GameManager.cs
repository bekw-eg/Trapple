using System.Collections.Generic;
using Trapple.Player;
using Trapple.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Core
{
    public enum GameState { MainMenu, Playing, Paused, Victory }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public const string MenuScene = "Assets/_Trapple/Scenes/Menu/MainMenu.unity";
        public const string TutorialScene = "Assets/_Trapple/Scenes/Levels/Tutorial.unity";
        public const string BossScene = "Assets/_Trapple/Scenes/BossArenas/BossTest.unity";

        [SerializeField] private string firstLevel = TutorialScene;
        private readonly List<PlayerController> pausedPlayers = new List<PlayerController>();
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // Also supplies pause/menu support when Tutorial is opened directly, without editing it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
                new GameObject("Game Systems", typeof(GameManager));
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (GetComponent<GameInterface>() == null)
                gameObject.AddComponent<GameInterface>();
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Trapple.MasterVolume", 1f));
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void Update()
        {
            // The hub owns modal input; its scenery continues running behind UI Toolkit panels.
            if (SceneManager.GetActiveScene().path == Trapple.Hub.HubSceneFlowController.HubScenePath ||
                SceneManager.GetActiveScene().path == Trapple.Hub.Illustrated.TrappleUIManager.ScenePath) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void StartGame() => LoadScene(Application.CanStreamedLevelBeLoaded(Trapple.Hub.HubSceneFlowController.HubScenePath)
            ? Trapple.Hub.HubSceneFlowController.HubScenePath : firstLevel);
        public void StartBossTest() => LoadScene(BossScene);
        public void ReturnToMenu() => LoadScene(MenuScene);
        public void RestartLevel() => LoadScene(SceneManager.GetActiveScene().path);

        public void Pause()
        {
            if (State != GameState.Playing) return;
            FreezePlayers();
            State = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            foreach (var player in pausedPlayers)
            {
                if (player != null && !player.GetComponent<PlayerDeath>().IsDead)
                    player.SetControlEnabled(true);
            }
            pausedPlayers.Clear();
            State = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void CompleteBossFight() => CompleteLevel();

        public void CompleteLevel()
        {
            if (State != GameState.Playing) return;
            FreezePlayers();
            State = GameState.Victory;
        }

        private void FreezePlayers()
        {
            pausedPlayers.Clear();
            foreach (var player in FindObjectsByType<PlayerController>())
            {
                if (!player.ControlEnabled) continue;
                pausedPlayers.Add(player);
                player.SetControlEnabled(false);
            }
        }

        public void LoadScene(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            // Explicit scene lists in PrototypeBuilder.BuildWindows keep shared build settings intact.
            if (Application.CanStreamedLevelBeLoaded(path))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(path);
                return;
            }
#if UNITY_EDITOR
            if (System.IO.File.Exists(path))
            {
                Time.timeScale = 1f;
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,
                    new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogError("Scene is unavailable: " + path + ". Build with Trapple/Build Boss Prototype (Windows).");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            pausedPlayers.Clear();
            Time.timeScale = 1f;
            State = scene.path == MenuScene ? GameState.MainMenu : GameState.Playing;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
