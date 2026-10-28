using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Player;
using Trapple.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class DeathScreenTests
    {
        private bool hadSave;
        private string savedProgress;
        private DeathScreenUI screen;
        private PlayerDeath player;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave = PlayerPrefs.HasKey(PreviewSaveService.Key);
            savedProgress = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            yield return null;
            PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if (GameManager.Instance == null) new GameObject("Test game systems", typeof(GameManager));
            GameManager.Instance.LoadScene(GameManager.TutorialScene);
            yield return null; yield return null;
            player = Object.FindAnyObjectByType<PlayerDeath>();
            screen = Object.FindAnyObjectByType<DeathScreenUI>(FindObjectsInactive.Include);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            if (hadSave) PlayerPrefs.SetString(PreviewSaveService.Key, savedProgress);
            else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            GameManager.Instance.LoadScene(GameManager.MenuScene);
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator DeathWaitsForExistingDelayThenFreezesSimulationAndBlocksOtherMenus()
        {
            Assert.That(screen.IsOpen, Is.False);
            var ui = Object.FindAnyObjectByType<GameInterface>();
            GameManager.Instance.Pause();
            ui.OpenSettings();
            player.Die(); player.Die();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Dying));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(screen.IsOpen, Is.False, "Preserve the authored 0.65-second delay");
            Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.False);
            Assert.That(ui.SettingsOpen, Is.False);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Dead));
            Assert.That(screen.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            float simulationTime = Time.time;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Time.time, Is.EqualTo(simulationTime));
            GameManager.Instance.Pause(); GameManager.Instance.Resume(); GameManager.Instance.CompleteLevel();
            ui.OpenSettings();
            Assert.That(ui.HandleEscape(), Is.True);
            Assert.That(ui.SettingsOpen, Is.False);
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Dead));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.RespawnButton.gameObject));
            Assert.That(screen.GetComponentsInChildren<TMP_Text>().Select(t => t.text),
                Is.EquivalentTo(new[] { "YOU DIED!", "Score: 0", "Respawn", "Go to the hub" }));
            Assert.That(screen.GetComponentsInChildren<Button>().Length, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<Button>().All(b => b.transform.IsChildOf(screen.transform)), Is.True);
        }

        [UnityTest]
        public IEnumerator RespawnWorksAcrossThreeDeathsAndIgnoresRepeatedClicks()
        {
            for (int i = 0; i < 3; i++)
            {
                var before = player;
                yield return DieQuickly();
                int loaded = 0;
                void Loaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) loaded++; }
                SceneManager.sceneLoaded += Loaded;
                try
                {
                    screen.RespawnButton.onClick.Invoke();
                    screen.RespawnButton.onClick.Invoke(); screen.HubButton.onClick.Invoke();
                    yield return null; yield return null;
                    Assert.That(loaded, Is.EqualTo(1));
                }
                finally { SceneManager.sceneLoaded -= Loaded; }
                Assert.That(before == null, Is.True);
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameManager.TutorialScene));
                player = Object.FindAnyObjectByType<PlayerDeath>();
                Assert.That(player.IsDead, Is.False);
                Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.True);
                Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(screen.IsOpen, Is.False);
                Assert.That(Object.FindObjectsByType<DeathScreenUI>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator RespawnAndHubKeepCurrencyUpgradesAndWorldProgress()
        {
            GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath);
            yield return null; yield return null;
            var flow = HubSceneFlowController.Instance;
            var progress = flow.Progression;
            progress.CompleteTutorial(); progress.CompleteLevel("meadow", 0); progress.AddSeeds(5000);
            // Current prices intentionally disable purchases. Supply a temporary test price only.
            var upgrade = flow.Configuration.Upgrade(UpgradeKind.Speed);
            var originalCosts = upgrade.costs;
            try
            {
                upgrade.costs = new[] { 10, 20, 30 };
                Assert.That(flow.Upgrades.Purchase(upgrade), Is.True);
            }
            finally { upgrade.costs = originalCosts; }
            int seeds = progress.Seeds;
            string saved = PlayerPrefs.GetString(PreviewSaveService.Key);
            Vector3 spawn = Object.FindAnyObjectByType<PlayerController>().transform.position;
            GameManager.Instance.LoadScene(GameManager.TutorialScene);
            yield return null; yield return null;
            player = Object.FindAnyObjectByType<PlayerDeath>();
            yield return DieQuickly();
            screen.RespawnButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key), Is.EqualTo(saved));
            player = Object.FindAnyObjectByType<PlayerDeath>();
            yield return DieQuickly();
            screen.HubButton.onClick.Invoke(); screen.HubButton.onClick.Invoke(); screen.RespawnButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(HubSceneFlowController.Instance, Is.SameAs(flow));
            Assert.That(progress.Seeds, Is.EqualTo(seeds));
            Assert.That(progress.Level(UpgradeKind.Speed), Is.EqualTo(1));
            Assert.That(progress.TutorialComplete, Is.True);
            Assert.That(progress.IsLevelComplete("meadow", 0), Is.True);
            Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key), Is.EqualTo(saved));
            var actor = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(actor.ControlEnabled, Is.True);
            Assert.That(Vector2.Distance(actor.transform.position, spawn), Is.LessThan(.1f));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DirectLevelEntryCanReturnToHubAndExternalTransitionsClearDeathScreen()
        {
            Assert.That(HubSceneFlowController.Instance, Is.Not.Null, "Direct gameplay entry initializes the existing progression service.");
            yield return DieQuickly();
            screen.HubButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled, Is.True);
            GameManager.Instance.LoadScene(GameManager.TutorialScene);
            yield return null; yield return null;
            player = Object.FindAnyObjectByType<PlayerDeath>();
            yield return DieQuickly();
            GameManager.Instance.LoadScene(GameManager.MenuScene);
            yield return null; yield return null;
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(screen.gameObject.activeInHierarchy, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeathButtonsReceiveRaycastPointerInputWhilePaused()
        {
            yield return DieQuickly();
            Canvas.ForceUpdateCanvases();
            foreach (var button in new[] { screen.RespawnButton, screen.HubButton })
            {
                var rect = (RectTransform)button.transform;
                var data = new PointerEventData(EventSystem.current) {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
            }
            var image = screen.GetComponent<Image>();
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(image.color.a, Is.InRange(.3f, .8f));
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(screen.RespawnButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            Assert.That(Object.FindAnyObjectByType<PlayerDeath>().IsDead, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeathInHubClosesItsModalAndBlocksItsInput()
        {
            GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath);
            yield return null; yield return null;
            var hub = Object.FindAnyObjectByType<HubUIController>();
            hub.Open(HubScreen.Settings);
            player = Object.FindAnyObjectByType<PlayerDeath>();
            yield return DieQuickly();
            Assert.That(hub.IsOpen, Is.False);
            Assert.That(Object.FindAnyObjectByType<HubInteractionController>().enabled, Is.False);
            Assert.That(screen.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(150));
        }

        [UnityTest]
        public IEnumerator RespawnReloadsPlayersSceneWhenAnotherSceneIsActive()
        {
            var sourceScene = player.gameObject.scene;
            var other = SceneManager.CreateScene("Death screen additive integration");
            SceneManager.SetActiveScene(other);
            yield return DieQuickly();
            Assert.That(GameManager.Instance.DeathScenePath, Is.EqualTo(sourceScene.path));
            screen.RespawnButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameManager.TutorialScene));
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CaptureAndCheckResponsiveLayout()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("-capture-death-screen"))
                Assert.Ignore("Opt-in rendered verification: -capture-death-screen without -batchmode or -nographics.");
#if UNITY_EDITOR
            yield return DieQuickly();
            var scaler = screen.GetComponentInParent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,
                new[] { System.Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
            var viewType = assembly.GetType("UnityEditor.GameView");
            var gameView = UnityEditor.EditorWindow.GetWindow(viewType);
            var selected = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int previous = (int)selected.GetValue(gameView);
            string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Logs/DeathScreenCaptures"));
            System.IO.Directory.CreateDirectory(output);
            try
            {
                foreach (var size in new[] { new Vector2Int(1366, 768), new Vector2Int(1920, 1080),
                    new Vector2Int(1024, 768), new Vector2Int(2560, 1080), new Vector2Int(720, 1280), new Vector2Int(3840, 2160) })
                {
                    var entry = System.Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),
                        new[] { System.Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"),
                            (object)size.x, size.y, "Death screen QA " + size });
                    group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry });
                    int index = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) +
                        (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null) - 1;
                    selected.SetValue(gameView, index);
                    gameView.Focus(); gameView.Repaint();
                    yield return new WaitForSecondsRealtime(.25f);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(Screen.width, Is.EqualTo(size.x));
                    Assert.That(Screen.height, Is.EqualTo(size.y));
                    foreach (var rect in screen.GetComponentsInChildren<RectTransform>())
                    {
                        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                        Assert.That((corners[0].x + corners[2].x) * .5f, Is.EqualTo(size.x * .5f).Within(2), rect.name);
                        Assert.That(corners.All(p => p.x >= -1 && p.y >= -1 && p.x <= size.x + 1 && p.y <= size.y + 1), Is.True, rect.name);
                    }
                    foreach (var text in screen.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False, text.name);
                        Assert.That(text.font.name, Does.Contain("Thaleah"));
                    }
                    yield return new WaitForEndOfFrame();
                    var capture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    capture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                    capture.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, size.x + "x" + size.y + ".png"), capture.EncodeToPNG());
                    Object.Destroy(capture);
                }
            }
            finally { selected.SetValue(gameView, previous); }
#endif
        }

        private IEnumerator DieQuickly()
        {
            typeof(PlayerDeath).GetField("restartDelay", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, .05f);
            player.Die(); player.Die();
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(screen.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
        }
    }
}
