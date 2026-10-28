using System.Collections;
using System.Linq;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class WorldMapTests
    {
        private PlayerController player;
        private HubUIController ui;
        private HubInteractionController interaction;
        private WorldMapInteractable map;
        private string save;
        private bool hadSave;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave = PlayerPrefs.HasKey(PreviewSaveService.Key);
            save = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return new WaitForSeconds(.3f);
            player = Object.FindAnyObjectByType<PlayerController>();
            ui = Object.FindAnyObjectByType<HubUIController>();
            interaction = Object.FindAnyObjectByType<HubInteractionController>();
            map = Object.FindAnyObjectByType<WorldMapInteractable>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            interaction?.EndMapInteraction();
            if (ui != null) ui.Close();
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            if (hadSave) PlayerPrefs.SetString(PreviewSaveService.Key, save);
            else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
            Time.timeScale = 1;
            yield return null;
        }

        private IEnumerator Approach()
        {
            player.GetComponent<Rigidbody2D>().position = new Vector2(map.transform.position.x, .6f);
            yield return new WaitForFixedUpdate();
            yield return null;
            interaction.RefreshTarget();
            Assert.That(interaction.Nearest, Is.SameAs(map));
            Assert.That(interaction.TryInteract(), Is.True);
        }

        private Vector2 ScreenPoint(float x, float y) => Camera.main.WorldToScreenPoint(
            map.transform.TransformPoint(new Vector3((x - 389) / 100, (553 - y) / 100, 0)));

        [UnityTest]
        public IEnumerator HubContainsOnlyOneBoardAndFourInvisibleTriggerZones()
        {
            Assert.That(Object.FindObjectsByType<WorldMapInteractable>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<HubInteractable>().Length, Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Any(value => value.name.StartsWith("Portal")), Is.False);
            var zones = map.GetComponentsInChildren<PolygonCollider2D>();
            Assert.That(zones.Length, Is.EqualTo(4));
            foreach (var zone in zones)
            {
                Assert.That(zone.isTrigger, Is.True);
                Assert.That(zone.GetComponent<Renderer>(), Is.Null);
            }
            Assert.That(map.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(map.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("WorldMapBoard"));
            Assert.That(map.GetComponent<SpriteRenderer>().bounds.min.y, Is.LessThan(.03f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProximityStartsInPlaceInteractionAndClosingRestoresControl()
        {
            Assert.That(map.InRange(Vector2.zero), Is.False);
            Assert.That(map.BeginInteraction(player), Is.False);
            yield return Approach();
            Assert.That(map.IsInteracting, Is.True);
            Assert.That(player.ControlEnabled, Is.False);
            Assert.That(ui.IsOpen, Is.False, "E must leave the physical map visible.");
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(interaction.TryInteract(), Is.False);
            interaction.EndMapInteraction();
            Assert.That(player.ControlEnabled, Is.True);
            Assert.That(map.IsInteracting, Is.False);
        }

        [UnityTest]
        public IEnumerator ClicksMatchAllFourDrawingsAndIgnoreFrameAndEmptyParchment()
        {
            Assert.That(map.WorldAtScreenPoint(Camera.main, ScreenPoint(312,238)), Is.Null);
            yield return Approach();
            Assert.That(map.WorldAtScreenPoint(Camera.main, ScreenPoint(312,238)), Is.EqualTo("meadow"));
            Assert.That(map.WorldAtScreenPoint(Camera.main, ScreenPoint(541,244)), Is.EqualTo("forest"));
            Assert.That(map.WorldAtScreenPoint(Camera.main, ScreenPoint(272,384)), Is.EqualTo("factory"));
            Assert.That(map.WorldAtScreenPoint(Camera.main, ScreenPoint(541,408)), Is.EqualTo("lava"));
            Assert.That(interaction.TrySelectWorld(ScreenPoint(405,347)), Is.False);
            Assert.That(interaction.TrySelectWorld(ScreenPoint(90,520)), Is.False);
            Assert.That(map.IsInteracting, Is.True);
        }

        [UnityTest]
        public IEnumerator EveryZoneOpensTheExistingLevelSelectionWithoutChangingProgressOrArt()
        {
            string before = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            var sprite = map.GetComponent<SpriteRenderer>().sprite;
            string[] worlds = { "meadow", "forest", "factory", "lava" };
            Vector2[] points = { new Vector2(312,238), new Vector2(541,244), new Vector2(272,384), new Vector2(541,408) };
            for (int i = 0; i < worlds.Length; i++)
            {
                yield return Approach();
                Assert.That(interaction.TrySelectWorld(ScreenPoint(points[i].x, points[i].y)), Is.True);
                yield return null;
                Assert.That(ui.CurrentScreen, Is.EqualTo(HubScreen.Worlds));
                Assert.That(interaction.ActiveMap, Is.Null);
                Assert.That(Object.FindAnyObjectByType<WorldMapPanelUI>().SelectedWorld.id, Is.EqualTo(worlds[i]));
                Assert.That(player.ControlEnabled, Is.False);
                if (i >= 2)
                {
                    var flow = HubSceneFlowController.Instance;
                    Assert.That(flow.CanLaunch(flow.Configuration.World(worlds[i]), 0), Is.False);
                    Assert.That(flow.TryLaunch(flow.Configuration.World(worlds[i]), 0), Is.False);
                }
                ui.HandleEscape();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(player.ControlEnabled, Is.True);
                Assert.That(map.GetComponent<SpriteRenderer>().sprite, Is.SameAs(sprite));
                Assert.That(map.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
                Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key, ""), Is.EqualTo(before));
            }
        }

        [UnityTest]
        public IEnumerator DisablingTheBoardOrOpeningAnotherModalCannotStrandControls()
        {
            yield return Approach();
            map.enabled = false;
            Assert.That(player.ControlEnabled, Is.True);
            map.enabled = true;
            yield return Approach();
            ui.Open(HubScreen.Settings);
            Assert.That(interaction.ActiveMap, Is.Null);
            Assert.That(player.ControlEnabled, Is.False);
            ui.Close();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.ControlEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator MeadowLaunchAndReturnUseExistingFlowAndRestoreTheBoard()
        {
            yield return Approach();
            Assert.That(interaction.TrySelectWorld(ScreenPoint(312,238)), Is.True);
            yield return null;
            Object.FindAnyObjectByType<WorldMapPanelUI>().Play();
            yield return new WaitForSeconds(.3f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Tutorial"));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled, Is.True);
            HubSceneFlowController.Instance.ReturnToHub();
            yield return new WaitForSeconds(.3f);
            Assert.That(Object.FindObjectsByType<WorldMapInteractable>().Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator AlternateHubSceneAlsoContainsTheBoardAndNoPortals()
        {
            yield return SceneManager.LoadSceneAsync(TrappleUIManager.ScenePath);
            yield return null;
            Assert.That(Object.FindObjectsByType<WorldMapInteractable>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Any(value => value.name.StartsWith("Portal")), Is.False);
            Object.FindAnyObjectByType<HubUIController>().Close();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled, Is.True);
        }
    }
}
