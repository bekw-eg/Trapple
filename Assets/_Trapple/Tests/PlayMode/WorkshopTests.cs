using System.Collections;
using System.Linq;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Player;
using Trapple.Workshop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Trapple.Tests
{
    public sealed class WorkshopTests
    {
        private HubUIController ui;
        private PlayerController player;
        private WorkshopTransitionController transition;
        private string oldSave;
        private bool hadSave;
        private int[][] costs;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key); oldSave=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if(HubSceneFlowController.Instance!=null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return new WaitForSeconds(.3f);
            ui=Object.FindAnyObjectByType<HubUIController>(); player=Object.FindAnyObjectByType<PlayerController>(); transition=ui.Workshop;
            player.GetComponent<Rigidbody2D>().position=new Vector2(-7.5f,.6f);
            costs=HubSceneFlowController.Instance.Configuration.upgrades.Select(value=>value.costs).ToArray();
            yield return new WaitForFixedUpdate();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            transition.Cancel();
            if(HubSceneFlowController.Instance!=null)
            {
                for(int i=0;i<costs.Length;i++) HubSceneFlowController.Instance.Configuration.upgrades[i].costs=costs[i];
                Object.Destroy(HubSceneFlowController.Instance.gameObject);
            }
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,oldSave); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save(); Time.timeScale=1; yield return null;
        }
        private IEnumerator Wait(WorkshopTransitionState state)
        {
            float until=Time.realtimeSinceStartup+12;
            while(transition.State!=state && Time.realtimeSinceStartup<until) yield return null;
            Assert.That(transition.State,Is.EqualTo(state));
        }
        private IEnumerator Enter() { ui.Open(HubScreen.Workshop); yield return Wait(WorkshopTransitionState.Inside); }
        [UnityTest]
        public IEnumerator EntryWalksClimbsIrisLocksAndExitRestoresThenReenters()
        {
            var door=Object.FindAnyObjectByType<WorkshopDoorAnimator>();
            Assert.That(door.FrameCount,Is.EqualTo(9)); Assert.That(door.Presentation.sprite.name,Is.EqualTo("door_00"));
            var camera=Camera.main; float size=camera.orthographicSize;
            var start=player.transform.position; ui.Open(HubScreen.Workshop);
            Assert.That(player.ControlEnabled,Is.False); Assert.That(transition.View.Interactable,Is.False);
            ui.Open(HubScreen.Wardrobe); ui.Close();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(player.transform.position.x,Is.LessThan(start.x));
            Assert.That(Vector2.Distance(player.transform.position,start),Is.LessThan(2));
            Assert.That(player.GetComponent<PlayerSpriteAnimator>().State,Is.EqualTo(PlayerVisualState.RunLeft));
            bool climbed=false,closed=false;
            float until=Time.realtimeSinceStartup+10;
            while(transition.State!=WorkshopTransitionState.Inside && Time.realtimeSinceStartup<until)
            {
                climbed|=player.transform.position.y>start.y+.2f;
                closed|=transition.Iris.IsCovered;
                if(!transition.View.Interactable) transition.View.Purchase();
                yield return null;
            }
            Assert.That(climbed && closed,Is.True);
            Assert.That(transition.View.Interactable,Is.True); Assert.That(camera.orthographic,Is.True);
            Assert.That(camera.orthographicSize,Is.LessThan(size)); Assert.That(transition.Iris.IsVisible,Is.False);
            ui.Close(); yield return Wait(WorkshopTransitionState.Outside);
            Assert.That(player.ControlEnabled,Is.True); Assert.That(player.GetComponent<Rigidbody2D>().simulated,Is.True);
            Assert.That(camera.orthographicSize,Is.EqualTo(size)); Assert.That(camera.GetComponent<HubCameraController>().enabled,Is.True);
            Assert.That(player.transform.position.x,Is.InRange(-8.8f,-8.5f));
            Assert.That(player.transform.position.y,Is.LessThan(1));
            Assert.That(door.FrameIndex,Is.Zero); Assert.That(door.Presentation.sprite.name,Is.EqualTo("door_00"));
            var interaction=Object.FindAnyObjectByType<HubInteractionController>();
            interaction.RefreshTarget(); Assert.That(interaction.Nearest.Screen,Is.EqualTo(HubScreen.Workshop));
            Assert.That(interaction.TryInteract(),Is.True);
            yield return Wait(WorkshopTransitionState.Inside); Assert.That(transition.View.Interactable,Is.True);
        }
        [UnityTest]
        public IEnumerator AnimatedPreviewsWrapAndPurchaseChangesSpeedWithoutResettingPosition()
        {
            yield return Enter(); var view=transition.View; var preview=view.Preview;
            var service=HubSceneFlowController.Instance.Progression;
            float speed=preview.RunSpeed; Vector2 before=preview.ActorPosition;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(preview.ActorPosition.x,Is.GreaterThan(before.x));
            Assert.That(preview.FrameIndex,Is.InRange(0,7));
            view.Purchase(); Assert.That(service.Level(UpgradeKind.Speed),Is.Zero);
            Assert.That(preview.DisplayedLevel,Is.Zero);
            HubSceneFlowController.Instance.Configuration.Upgrade(UpgradeKind.Speed).costs=new[]{10,20,30}; service.AddSeeds(100);
            before=preview.ActorPosition; view.Purchase(); view.Purchase();
            Assert.That(service.Level(UpgradeKind.Speed),Is.EqualTo(1)); Assert.That(service.Seeds,Is.EqualTo(90));
            Assert.That(Vector2.Distance(before,preview.ActorPosition),Is.LessThan(2));
            yield return new WaitForSecondsRealtime(.5f); Assert.That(preview.RunSpeed,Is.GreaterThan(speed));
            yield return new WaitForSecondsRealtime(4); Assert.That(preview.WrapCount,Is.GreaterThan(0));
            var restored=new PreviewProgressionService(HubSceneFlowController.Instance.Configuration,new PreviewSaveService());
            Assert.That(restored.Level(UpgradeKind.Speed),Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator JumpAndAgilityMoveInRealtimeAndUpgradesIncreaseHeight()
        {
            yield return Enter(); var view=transition.View; view.Select(UpgradeKind.Jump);
            float oldHeight=view.Preview.JumpHeight;
            Vector2 before=view.Preview.ActorPosition; yield return new WaitForSecondsRealtime(.25f);
            Assert.That(view.Preview.ActorPosition,Is.Not.EqualTo(before));
            var service=HubSceneFlowController.Instance.Progression;
            HubSceneFlowController.Instance.Configuration.Upgrade(UpgradeKind.Jump).costs=new[]{1,1,1}; service.AddSeeds(10);
            view.Purchase(); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(view.Preview.JumpHeight,Is.GreaterThan(oldHeight));
            view.Select(UpgradeKind.Agility); yield return new WaitForSecondsRealtime(.65f);
            before=view.Preview.ActorPosition; yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector2.Distance(before,view.Preview.ActorPosition),Is.GreaterThan(1));
            Assert.That(player.GetComponent<Rigidbody2D>().simulated,Is.False);
        }
        [UnityTest]
        public IEnumerator CancelDuringEntryRestoresOriginalPoseCameraAndControlAtZeroTimescale()
        {
            Vector3 original=player.transform.position; Vector3 camera=Camera.main.transform.position;
            Time.timeScale=0; ui.Open(HubScreen.Workshop); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(player.transform.position,Is.Not.EqualTo(original));
            transition.enabled=false;
            Assert.That(player.transform.position,Is.EqualTo(original)); Assert.That(Camera.main.transform.position,Is.EqualTo(camera));
            Assert.That(player.ControlEnabled,Is.True); Assert.That(transition.Iris.IsVisible,Is.False);
            Assert.That(ui.IsOpen,Is.False);
            transition.enabled=true; yield return Enter();
        }
        [UnityTest]
        public IEnumerator WorkshopButtonsReceiveRaycastsAndUseSingleEventSystem()
        {
            yield return Enter(); Canvas.ForceUpdateCanvases();
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            foreach(var button in transition.View.GetComponentsInChildren<Button>())
            {
                var rect=(RectTransform)button.transform;
                Vector2 center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var hits=new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);
                Assert.That(hits.Count,Is.GreaterThan(0),button.name);
                Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),button.name);
            }
        }
        [UnityTest]
        public IEnumerator CancelInsidePreservesPreviouslyDisabledControlAndFollow()
        {
            player.SetControlEnabled(false); Camera.main.GetComponent<HubCameraController>().enabled=false;
            var original=player.transform.position;
            yield return Enter(); transition.Cancel();
            Assert.That(player.ControlEnabled,Is.False);
            Assert.That(Camera.main.GetComponent<HubCameraController>().enabled,Is.False);
            Assert.That(player.transform.position,Is.EqualTo(original));
            Assert.That(transition.View.IsOpen,Is.False); Assert.That(transition.Iris.IsVisible,Is.False);
        }
    }
}
