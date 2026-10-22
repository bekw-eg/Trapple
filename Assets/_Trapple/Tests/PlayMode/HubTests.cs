using System.Collections;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Trapple.Hub.Illustrated;
using UnityEngine.EventSystems;
using UGUIButton = UnityEngine.UI.Button;

namespace Trapple.Tests
{
    public sealed class HubTests
    {
        private PlayerController player;
        private HubUIController ui;
        private HubInteractionController interaction;
        private bool hadSave;
        private string previousSave;
        private int[][] previousCosts;

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
            previousCosts=System.Array.ConvertAll(HubSceneFlowController.Instance.Configuration.upgrades,value=>value.costs);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(previousCosts!=null && HubSceneFlowController.Instance!=null)
                for(int i=0;i<previousCosts.Length;i++) HubSceneFlowController.Instance.Configuration.upgrades[i].costs=previousCosts[i];
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
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("Appi"));
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
            Assert.That(ui.Illustrated.GetComponentsInChildren<UpgradeCardUI>(true).Length,Is.EqualTo(3));
            Assert.That(ui.IsOpen,Is.False);
            Assert.That(player.ControlEnabled,Is.True);
        }

        [UnityTest]
        public IEnumerator SubmitOnPurchaseButtonUpdatesBalanceAndSavedLevel()
        {
            ui.Open(HubScreen.Workshop);
            var progression=HubSceneFlowController.Instance.Progression;
            HubSceneFlowController.Instance.Configuration.Upgrade(UpgradeKind.Speed).costs=new[]{10,20,35};
            progression.AddSeeds(100);
            int seeds=progression.Seeds;
            var buy=FindButton("workshop-upgrade_buttons-speed");
            ExecuteEvents.Execute(buy.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
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
            Assert.That(ui.Illustrated.GetComponentsInChildren<WorldCardUI>(true).Length,Is.EqualTo(4));
            var config=Object.FindAnyObjectByType<HubBootstrap>();
            Assert.That(config,Is.Not.Null);
            FindButton("worlds-world_buttons-deceptive_forest").onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(ui.Illustrated.Message,Does.Contain("закрыт"));
        }

        [UnityTest]
        public IEnumerator WardrobeUsesProvidedCharactersAndLocksWorldSkins()
        {
            ui.Open(HubScreen.Wardrobe);
            yield return null;
            Assert.That(ui.Illustrated.GetComponentsInChildren<SkinItemUI>(true).Length,Is.EqualTo(4));
            Assert.That(HubSceneFlowController.Instance.Progression.EquippedCosmetic,Is.EqualTo("classic"));
            FindButton("wardrobe-skin_rows-volcano_heart").onClick.Invoke();
            yield return null;
            Assert.That(HubSceneFlowController.Instance.Progression.SelectedCosmetic,Is.EqualTo("lava"));
            FindButton("Equip selected skin").onClick.Invoke();
            Assert.That(ui.Illustrated.Message,Does.Contain("закрыт"));
            Assert.That(HubSceneFlowController.Instance.Progression.EquippedCosmetic,Is.EqualTo("classic"));
        }

        [UnityTest]
        public IEnumerator EscapeOpensSettingsAndGameplaySpaceCannotSubmitHud()
        {
            Assert.That(ui.IsOpen,Is.False);
            var settings=ui.Root.Q<Button>("settings");
            Assert.That(settings.focusable,Is.False);
            using(var submit=NavigationSubmitEvent.GetPooled()) settings.SendEvent(submit);
            yield return null;
            Assert.That(ui.IsOpen,Is.False);
            ui.HandleEscape();
            Assert.That(ui.CurrentScreen,Is.EqualTo(HubScreen.Settings));
            Assert.That(player.ControlEnabled,Is.False);
            ui.HandleEscape();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.ControlEnabled,Is.True);
            Assert.That(ui.Root.panel.focusController.focusedElement,Is.Null);
            using(var submit=NavigationSubmitEvent.GetPooled()) settings.SendEvent(submit);
            yield return null;
            Assert.That(ui.IsOpen,Is.False);
            ui.Open(HubScreen.Workshop);
            ui.HandleEscape();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(ui.IsOpen,Is.False);
            Assert.That(player.ControlEnabled,Is.True);
        }

        [UnityTest]
        public IEnumerator SuppliedFramesAnimateWalkingJumpingAndModalIdle()
        {
            var animator=player.GetComponent<PlayerSpriteAnimator>();
            Assert.That(animator,Is.Not.Null);
            Assert.That(animator.Animations.idle.Length,Is.EqualTo(4));
            Assert.That(animator.Animations.walkRight.Length,Is.EqualTo(6));
            Assert.That(animator.Animations.walkLeft.Length,Is.EqualTo(6));
            Assert.That(animator.Animations.jump.Length,Is.EqualTo(4));
            Assert.That(animator.Animations.idle[0].texture.filterMode,Is.EqualTo(FilterMode.Point));
            var body=player.GetComponent<Rigidbody2D>();
            player.enabled=false; // Drive real velocity, leaving the existing movement code untouched.
            body.linearVelocity=new Vector2(3,0);
            yield return new WaitForSeconds(.12f);
            Assert.That(animator.State,Is.EqualTo(PlayerVisualState.WalkRight));
            int first=animator.FrameIndex;
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.FrameIndex,Is.Not.EqualTo(first));
            body.linearVelocity=new Vector2(-3,0);
            yield return new WaitForSeconds(.1f);
            Assert.That(animator.State,Is.EqualTo(PlayerVisualState.WalkLeft));
            Assert.That(player.GetComponent<SpriteRenderer>().flipX,Is.False);
            body.linearVelocity=new Vector2(0,10);
            yield return new WaitForSeconds(.12f);
            Assert.That(animator.State,Is.EqualTo(PlayerVisualState.Rising));
            yield return new WaitForSeconds(.3f);
            Assert.That(animator.State,Is.EqualTo(PlayerVisualState.Falling));
            yield return new WaitForSeconds(.6f);
            ui.HandleEscape();
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.State,Is.EqualTo(PlayerVisualState.Idle));
        }

        [UnityTest]
        public IEnumerator RegisteredTutorialLaunchAndReturnPreserveProgress()
        {
            ui.Open(HubScreen.Worlds);
            yield return null;
            var launch=FindButton("worlds-world_buttons-apple_meadow");
            Assert.That(launch.interactable,Is.True);
            launch.onClick.Invoke();
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

        private UGUIButton FindButton(string name) => System.Array.Find(ui.Illustrated.GetComponentsInChildren<UGUIButton>(true),value=>value.name==name);

        [UnityTest]
        public IEnumerator ActualTabsCloseAndSecretButtonsAreWiredAndDoNotReloadScene()
        {
            var scene=SceneManager.GetActiveScene().handle;
            ui.Open(HubScreen.Worlds); yield return null;
            Assert.That(ui.Illustrated.IsOpen,Is.True);
            FindButton("worlds-tabs-workshop").onClick.Invoke();
            Assert.That(ui.CurrentScreen,Is.EqualTo(HubScreen.Workshop));
            FindButton("workshop-tabs-wardrobe").onClick.Invoke();
            Assert.That(ui.CurrentScreen,Is.EqualTo(HubScreen.Wardrobe));
            FindButton("wardrobe-tabs-worlds").onClick.Invoke();
            FindButton("worlds-secret_worlds").onClick.Invoke();
            Assert.That(ui.Illustrated.Message,Is.EqualTo("Скоро"));
            Assert.That(SceneManager.GetActiveScene().handle,Is.EqualTo(scene));
            FindButton("worlds-close").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(ui.Illustrated.IsOpen,Is.False);
            Assert.That(player.ControlEnabled,Is.True);
        }

        [UnityTest]
        public IEnumerator PurchasesGuardUnconfiguredPricesFundsRapidClicksAndMaximum()
        {
            ui.Open(HubScreen.Workshop); yield return null;
            var button=FindButton("workshop-upgrade_buttons-speed");
            var service=HubSceneFlowController.Instance.Progression;
            button.onClick.Invoke(); Assert.That(ui.Illustrated.Message,Does.Contain("не настроена"));
            HubSceneFlowController.Instance.Configuration.Upgrade(UpgradeKind.Speed).costs=new[]{10,20,35};
            button.onClick.Invoke(); Assert.That(ui.Illustrated.Message,Does.Contain("Недостаточно"));
            service.AddSeeds(100);
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.That(service.Level(UpgradeKind.Speed),Is.EqualTo(1)); Assert.That(service.Seeds,Is.EqualTo(90));
            yield return null; button.onClick.Invoke(); yield return null; button.onClick.Invoke(); yield return null;
            Assert.That(service.Level(UpgradeKind.Speed),Is.EqualTo(3)); Assert.That(service.Seeds,Is.EqualTo(35));
            Assert.That(button.interactable,Is.False);
            button.onClick.Invoke(); Assert.That(service.Seeds,Is.EqualTo(35));
        }

        [UnityTest]
        public IEnumerator TutorialCompletionRefreshesWorldUnlockWithoutInventingSceneOrCurrency()
        {
            ui.Open(HubScreen.Worlds);
            var service=HubSceneFlowController.Instance.Progression;
            service.CompleteTutorial(); yield return null;
            Assert.That(service.IsWorldUnlocked(HubSceneFlowController.Instance.Configuration.World("forest")),Is.True);
            FindButton("worlds-world_buttons-deceptive_forest").onClick.Invoke();
            Assert.That(ui.Illustrated.Message,Does.Contain("сцена пока недоступна"));
            Assert.That(service.Seeds,Is.Zero);
        }

        [UnityTest]
        public IEnumerator HotspotsAreRaycastableAndArtworkScalesUniformly()
        {
            foreach(var screen in new[]{HubScreen.Worlds,HubScreen.Workshop,HubScreen.Wardrobe})
            {
                ui.Open(screen); yield return null; Canvas.ForceUpdateCanvases();
                var pages=ui.Illustrated.GetComponentsInChildren<ReferencePixelScale>();
                Assert.That(pages.Length,Is.EqualTo(1));
                Assert.That(pages[0].transform.lossyScale.x,Is.EqualTo(pages[0].transform.lossyScale.y).Within(.0001f));
                var buttons=ui.Illustrated.GetComponentsInChildren<UGUIButton>();
                foreach(var button in buttons)
                {
                    var rect=(RectTransform)button.transform;
                    var position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                    var data=new PointerEventData(EventSystem.current){position=position};
                    var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
                    Assert.That(hits.Exists(hit=>hit.gameObject==button.gameObject),Is.True,button.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator StandaloneSceneOpensMapOverPlayableHubAndCloseRestoresControl()
        {
            yield return SceneManager.LoadSceneAsync(TrappleUIManager.ScenePath);
            yield return null;
            var view=Object.FindAnyObjectByType<TrappleUIManager>();
            Assert.That(view.CurrentScreen,Is.EqualTo(HubScreen.Worlds));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            var hubPlayer=Object.FindAnyObjectByType<PlayerController>();
            Assert.That(hubPlayer,Is.Not.Null);
            Assert.That(hubPlayer.ControlEnabled,Is.False);
            Assert.That(Object.FindAnyObjectByType<HubCameraController>(),Is.Not.Null);
            view.Close(); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(TrappleUIManager.ScenePath));
            Assert.That(view.IsOpen,Is.False);
            Assert.That(hubPlayer.ControlEnabled,Is.True);
        }

        [UnityTest]
        public IEnumerator UnlockedSkinEquipsThroughActualUIAndRestoresFromDisk()
        {
            var service=HubSceneFlowController.Instance.Progression;
            service.GrantCosmetic("forest"); ui.Open(HubScreen.Wardrobe); yield return null;
            FindButton("wardrobe-skin_rows-forest_traveler").onClick.Invoke();
            FindButton("Equip selected skin").onClick.Invoke(); yield return null;
            Assert.That(service.EquippedCosmetic,Is.EqualTo("forest"));
            var config=HubSceneFlowController.Instance.Configuration;
            Assert.That(player.GetComponent<SpriteRenderer>().sprite,Is.EqualTo(config.Cosmetic("forest").sprite));
            Assert.That(service.IsWorldUnlocked(config.World("forest")),Is.False);
            var restored=new PreviewProgressionService(config,new PreviewSaveService());
            Assert.That(restored.SelectedCosmetic,Is.EqualTo("forest"));
            Assert.That(restored.EquippedCosmetic,Is.EqualTo("forest"));
        }
    }
}
