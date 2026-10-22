using System.Collections;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Trapple.Tests
{
    public sealed class HubTests
    {
        private PlayerController player;
        private HubUIController ui;
        private HubInteractionController interaction;
        private bool hadSave;
        private string previousSave;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key);
            previousSave=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if(HubSceneFlowController.Instance!=null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return new WaitForSeconds(.3f);
            player=Object.FindAnyObjectByType<PlayerController>();
            ui=Object.FindAnyObjectByType<HubUIController>();
            interaction=Object.FindAnyObjectByType<HubInteractionController>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(HubSceneFlowController.Instance!=null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,previousSave);
            else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save(); Time.timeScale=1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerSpawnsGroundedAndCameraFollows()
        {
            Assert.That(player,Is.Not.Null);
            Assert.That(player.ControlEnabled,Is.True);
            Assert.That(player.IsGrounded,Is.True);
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("Classic"));
            player.GetComponent<Rigidbody2D>().position=new Vector2(7,.6f);
            float before=Camera.main.transform.position.x;
            yield return new WaitForSeconds(.8f);
            Assert.That(Camera.main.transform.position.x,Is.GreaterThan(before+1f));
        }

        [UnityTest]
        public IEnumerator PromptInteractionModalAndControlRestoration()
        {
            player.GetComponent<Rigidbody2D>().position=new Vector2(-7.5f,.6f);
            yield return new WaitForFixedUpdate();
            interaction.RefreshTarget();
            Assert.That(interaction.Nearest.Screen,Is.EqualTo(HubScreen.Workshop));
            Assert.That(ui.Root.Q("interaction-prompt").ClassListContains("visible"),Is.True);
            Assert.That(interaction.TryInteract(),Is.True);
            Assert.That(ui.CurrentScreen,Is.EqualTo(HubScreen.Workshop));
            Assert.That(player.ControlEnabled,Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1));
            ui.Close();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.ControlEnabled,Is.True);
            player.GetComponent<Rigidbody2D>().position=new Vector2(0,.6f);
            yield return new WaitForFixedUpdate();
            yield return null;
            interaction.RefreshTarget();
            Assert.That(interaction.Nearest,Is.Null);
            Assert.That(ui.Root.Q("interaction-prompt").ClassListContains("visible"),Is.False);
        }

        [UnityTest]
        public IEnumerator RapidReopenAndDisableNeverStrandControls()
        {
            ui.Open(HubScreen.Workshop); ui.Close(); ui.Open(HubScreen.Wardrobe);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.ControlEnabled,Is.False);
            ui.Close();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.ControlEnabled,Is.True);
            ui.Open(HubScreen.Workshop); ui.enabled=false;
            Assert.That(player.ControlEnabled,Is.True);
            ui.enabled=true;
            yield return null;
            Assert.That(ui.Root.Q("upgrade-cards").childCount,Is.EqualTo(3));
            Assert.That(ui.IsOpen,Is.False);
            Assert.That(player.ControlEnabled,Is.True);
        }

        [UnityTest]
        public IEnumerator SubmitOnPurchaseButtonUpdatesBalanceAndSavedLevel()
        {
            ui.Open(HubScreen.Workshop);
            var progression=HubSceneFlowController.Instance.Progression;
            int seeds=progression.Seeds;
            var buy=ui.Root.Q<Button>("buy-speed");
            buy.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()) buy.SendEvent(evt);
            yield return null;
            Assert.That(progression.Seeds,Is.EqualTo(seeds-10));
            Assert.That(progression.Level(UpgradeKind.Speed),Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key),Does.Contain("\"speed\":1"));
        }

        [UnityTest]
        public IEnumerator WorldCardsAndLockedLevelGuards()
        {
            ui.Open(HubScreen.Worlds);
            yield return null;
            Assert.That(ui.Root.Q("world-cards").childCount,Is.EqualTo(4));
            var config=Object.FindAnyObjectByType<HubBootstrap>();
            Assert.That(config,Is.Not.Null);
            var locked=ui.Root.Q<Button>("select-forest");
            locked.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()) locked.SendEvent(evt);
            yield return null;
            Assert.That(ui.Root.Q<Button>("launch-forest-0").enabledSelf,Is.False);
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(ui.Root.Q("world-forest").ClassListContains("locked"),Is.True);
        }

        [UnityTest]
        public IEnumerator WardrobeUsesProvidedCharactersAndLocksWorldSkins()
        {
            ui.Open(HubScreen.Wardrobe);
            yield return null;
            Assert.That(ui.Root.Q("cosmetics").childCount,Is.EqualTo(4));
            Assert.That(ui.Root.Q<Button>("equip").text,Does.Contain("EQUIPPED"));
            var option=ui.Root.Q<Button>("cosmetic-lava");
            option.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()) option.SendEvent(evt);
            yield return null;
            Assert.That(ui.Root.Q<Label>("cosmetic-name").text,Is.EqualTo("Сердце вулкана"));
            Assert.That(ui.Root.Q<Button>("equip").enabledSelf,Is.False);
            Assert.That(HubSceneFlowController.Instance.Progression.EquippedCosmetic,Is.EqualTo("classic"));
        }

        [UnityTest]
        public IEnumerator RegisteredTutorialLaunchAndReturnPreserveProgress()
        {
            ui.Open(HubScreen.Worlds);
            yield return null;
            var launch=ui.Root.Q<Button>("launch-meadow-0");
            Assert.That(launch.enabledSelf,Is.True);
            launch.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()) launch.SendEvent(evt);
            yield return new WaitForSeconds(.35f);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Tutorial"));
            Assert.That(Object.FindAnyObjectByType<Trapple.Level.TutorialGoal>(),Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
            Assert.That(Object.FindAnyObjectByType<UIDocument>().rootVisualElement.Q<Button>("return-hub"),Is.Not.Null);
            HubSceneFlowController.Instance.ReturnToHub();
            yield return new WaitForSeconds(.35f);
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
        }
    }
}
