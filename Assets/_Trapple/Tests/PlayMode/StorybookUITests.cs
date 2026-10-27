using System.Collections;
using System.Linq;
using NUnit.Framework;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.Player;
using Trapple.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class StorybookUITests
    {
        private bool hadSave,hadVolume;
        private string save;
        private float volume;
        private int[] oldCosts;
        private HubConfiguration config;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key); save=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            hadVolume=PlayerPrefs.HasKey(UISettingsPanel.VolumeKey); volume=PlayerPrefs.GetFloat(UISettingsPanel.VolumeKey,1);
            PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if(HubSceneFlowController.Instance!=null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            yield return null; yield return SceneManager.LoadSceneAsync(GameManager.MenuScene); yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(config!=null && oldCosts!=null) config.Upgrade(UpgradeKind.Speed).costs=oldCosts;
            if(HubSceneFlowController.Instance!=null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,save); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if(hadVolume) PlayerPrefs.SetFloat(UISettingsPanel.VolumeKey,volume); else PlayerPrefs.DeleteKey(UISettingsPanel.VolumeKey);
            PlayerPrefs.Save(); AudioListener.volume=volume; Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync(GameManager.MenuScene); yield return null;
        }
        private static Button Button(string name) => Object.FindObjectsByType<Button>().First(b=>b.name==name && b.isActiveAndEnabled);
        [UnityTest]
        public IEnumerator TutorialHintsFollowExistingCourseAndHideInModals()
        {
            Assert.That(Object.FindAnyObjectByType<ControlHintView>(),Is.Null,"No control hints in the main menu");
            GameManager.Instance.LoadScene(GameManager.TutorialScene); yield return null; yield return null;
            var hint=Object.FindAnyObjectByType<ControlHintView>();
            Assert.That(hint.Current,Is.EqualTo(ControlHint.Move));
            var actor=Object.FindAnyObjectByType<PlayerController>(); var body=actor.GetComponent<Rigidbody2D>();
            body.simulated=false; actor.transform.position=new Vector3(2,1,0); yield return null; yield return null;
            Assert.That(hint.Current,Is.EqualTo(ControlHint.Jump));
            body.linearVelocity=Vector2.up*2; yield return null; yield return null;
            Assert.That(hint.gameObject.activeSelf,Is.False,"A demonstrated jump dismisses the prompt");
            body.linearVelocity=Vector2.zero; actor.transform.position=new Vector3(16,1,0); yield return null; yield return null;
            Assert.That(hint.Current,Is.EqualTo(ControlHint.Sprint));
            GameManager.Instance.Pause(); yield return null;
            Assert.That(hint.gameObject.activeInHierarchy,Is.False);
            GameManager.Instance.Resume(); yield return null; yield return null;
            Assert.That(hint.Current,Is.EqualTo(ControlHint.Sprint));
            actor.transform.position=new Vector3(20,1,0); yield return null; yield return null;
            Assert.That(hint.gameObject.activeSelf,Is.False,"Leaving the teaching area dismisses its prompt");
        }
        [UnityTest]
        public IEnumerator LevelWarningFadesExpiresAndDoesNotAgeDuringPause()
        {
            GameManager.Instance.LoadScene(GameManager.TutorialScene); yield return null; yield return null;
            var notice=Object.FindAnyObjectByType<TimedNotice>(); var group=notice.GetComponent<CanvasGroup>();
            notice.Show("WATCH YOUR STEP",1.4f); yield return new WaitForSecondsRealtime(.08f);
            Assert.That(group.alpha,Is.GreaterThan(0).And.LessThan(1),"Warning fades in");
            GameManager.Instance.Pause(); yield return null; float pausedAlpha=group.alpha;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(group.alpha,Is.EqualTo(pausedAlpha)); Assert.That(notice.IsVisible,Is.False);
            GameManager.Instance.Resume(); yield return new WaitForSecondsRealtime(.35f);
            Assert.That(group.alpha,Is.EqualTo(1).Within(.01f));
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(group.alpha,Is.GreaterThan(0).And.LessThan(1),"Warning fades out");
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(notice.IsVisible,Is.False);
        }
        [UnityTest]
        public IEnumerator MenuPlayUsesExistingSceneSequenceAndOnlyOneEventSystem()
        {
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            Button("Play").onClick.Invoke(); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
            Assert.That(Object.FindObjectsByType<Button>().Any(button=>button.name=="Pause"),Is.False,"The hub keeps its existing Escape/settings flow");
        }
        [UnityTest]
        public IEnumerator PauseSettingsBackAndResumePreserveTimeAndSavedVolume()
        {
            GameManager.Instance.LoadScene(GameManager.TutorialScene); yield return null; yield return null;
            Button("Pause").onClick.Invoke(); yield return null;
            Assert.That(Time.timeScale,Is.Zero); Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.False);
            Button("SETTINGS").onClick.Invoke(); yield return null;
            Object.FindAnyObjectByType<UISettingsPanel>().Master.value=.42f;
            var view=Object.FindAnyObjectByType<GameInterface>(); Assert.That(view.HandleEscape(),Is.True); yield return null;
            Assert.That(Time.timeScale,Is.Zero); Assert.That(view.SettingsOpen,Is.False);
            Assert.That(PlayerPrefs.GetFloat(UISettingsPanel.VolumeKey),Is.EqualTo(.42f).Within(.001));
            Button("CONTINUE").onClick.Invoke(); yield return null;
            Assert.That(Time.timeScale,Is.EqualTo(1)); Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
        }
        [UnityTest]
        public IEnumerator MasterVolumeSliderReceivesPointerInputAndPersists()
        {
            Button("Settings").onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
            var slider=Object.FindAnyObjectByType<UISettingsPanel>().Master; var rect=(RectTransform)slider.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*.25f,rect.rect.center.y)));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0)); Assert.That(hits[0].gameObject.GetComponentInParent<Slider>(),Is.SameAs(slider));
            slider.OnPointerDown(data); slider.OnDrag(data); slider.OnPointerUp(data);
            Assert.That(slider.value,Is.InRange(.15f,.35f)); Assert.That(AudioListener.volume,Is.EqualTo(slider.value).Within(.001f));
            float value=slider.value; Button("Back").onClick.Invoke(); yield return null;
            Button("Settings").onClick.Invoke(); yield return null;
            Assert.That(Object.FindAnyObjectByType<UISettingsPanel>().Master.value,Is.EqualTo(value).Within(.001f));
        }
        [UnityTest]
        public IEnumerator WorldSelectionReflectsDataAndNeverUnlocksOrLaunchesMissingLevels()
        {
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath); yield return null;
            string before=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            var hub=Object.FindAnyObjectByType<HubUIController>(); hub.Open(HubScreen.Worlds); yield return null;
            var map=Object.FindAnyObjectByType<WorldMapPanelUI>(); map.SelectWorld(2); yield return null;
            Assert.That(map.SelectedWorld.id,Is.EqualTo("factory")); Assert.That(Button("Play level").interactable,Is.False);
            map.Play(); yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key,""),Is.EqualTo(before));
            hub.Close(); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
        }
        [UnityTest]
        public IEnumerator WardrobeUsesExistingUnlockAndEquipServices()
        {
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath); yield return null;
            var hub=Object.FindAnyObjectByType<HubUIController>(); hub.Open(HubScreen.Wardrobe); yield return null;
            var panel=Object.FindAnyObjectByType<WardrobePanelUI>(); panel.Select("forest"); yield return null;
            Assert.That(Button("Equip outfit").interactable,Is.False);
            var progress=HubSceneFlowController.Instance.Progression; Assert.That(progress.EquippedCosmetic,Is.EqualTo("classic"));
            progress.GrantCosmetic("forest"); yield return null;
            Button("Equip outfit").onClick.Invoke(); yield return null;
            Assert.That(progress.EquippedCosmetic,Is.EqualTo("forest"));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().GetComponent<SpriteRenderer>().sprite,
                Is.EqualTo(HubSceneFlowController.Instance.Configuration.Cosmetic("forest").sprite));
            Assert.That(Button("Equip outfit").interactable,Is.False);
        }
        [UnityTest]
        public IEnumerator WorkshopPreservesCutsceneAndDebitsOnlyConfiguredPurchases()
        {
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath); yield return null;
            var hub=Object.FindAnyObjectByType<HubUIController>(); hub.Open(HubScreen.Workshop);
            float until=Time.realtimeSinceStartup+20; while(hub.Workshop.IsBusy && Time.realtimeSinceStartup<until) yield return null;
            Assert.That(hub.Workshop.View.Interactable,Is.True);
            Assert.That(Button("Purchase upgrade").interactable,Is.False);
            config=HubSceneFlowController.Instance.Configuration; var def=config.Upgrade(UpgradeKind.Speed); oldCosts=def.costs;
            def.costs=new[]{10,20,30}; var progress=HubSceneFlowController.Instance.Progression; progress.AddSeeds(60);
            var view=hub.Workshop.View; view.Select(UpgradeKind.Speed);
            for(int i=0;i<3;i++)
            {
                Assert.That(Button("Purchase upgrade").interactable,Is.True);
                view.Purchase(); view.Purchase(); yield return null;
                Assert.That(progress.Level(UpgradeKind.Speed),Is.EqualTo(i+1));
            }
            Assert.That(progress.Seeds,Is.Zero); Assert.That(Button("Purchase upgrade").interactable,Is.False);
            hub.Close(); until=Time.realtimeSinceStartup+20; while(hub.Workshop.IsBusy && Time.realtimeSinceStartup<until) yield return null;
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled,Is.True);
        }
    }
}
