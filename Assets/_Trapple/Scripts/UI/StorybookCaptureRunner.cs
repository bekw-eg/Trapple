using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace Trapple.UI
{
    // Opt-in development player verification; never runs in normal play.
    public sealed class StorybookCaptureRunner : MonoBehaviour
    {
        private string output, savedProgress;
        private float savedVolume;
        private bool hadProgress, hadVolume, captured, failed, previousBackground;
        private readonly List<string> checks=new List<string>();
#if UNITY_EDITOR
        private static int previousGameViewSize=-1;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if(Application.isEditor)
            {
#if UNITY_EDITOR
                if(!SessionState.GetBool("Storybook.Capture",false)) return;
                SessionState.SetBool("Storybook.Capture",false);
                var editorRunner=new GameObject("Storybook verification").AddComponent<StorybookCaptureRunner>();
                DontDestroyOnLoad(editorRunner.gameObject); editorRunner.output=Path.GetFullPath("Temp/StorybookEditorCaptures");
                editorRunner.StartCoroutine(editorRunner.Run());
#endif
                return;
            }
            if(!Debug.isDebugBuild) return;
            string[] args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-storybook-capture");
            if(i<0 || i+1>=args.Length) return;
            var runner=new GameObject("Storybook verification").AddComponent<StorybookCaptureRunner>();
            DontDestroyOnLoad(runner.gameObject); runner.output=Path.GetFullPath(args[i+1]); runner.StartCoroutine(runner.Run());
        }
        private IEnumerator Run()
        {
            previousBackground=Application.runInBackground; Application.runInBackground=true; Directory.CreateDirectory(output);
            foreach(string name in new[]{"result.txt","errors.txt","checks.txt"})
                if(File.Exists(Path.Combine(output,name))) File.Delete(Path.Combine(output,name));
            hadProgress=PlayerPrefs.HasKey(PreviewSaveService.Key); savedProgress=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            hadVolume=PlayerPrefs.HasKey(UISettingsPanel.VolumeKey); savedVolume=PlayerPrefs.GetFloat(UISettingsPanel.VolumeKey,1); captured=true;
            PlayerPrefs.DeleteKey(PreviewSaveService.Key); Application.logMessageReceived+=OnLog;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(2560,1440)})
            {
                SetResolution(size);
                float deadline=Time.realtimeSinceStartup+3;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<deadline) yield return null;
                yield return new WaitForSecondsRealtime(.4f);
                Check(Screen.width==size.x && Screen.height==size.y,"Resolution "+size);
                GameManager.Instance.ReturnToMenu(); yield return new WaitForSecondsRealtime(.4f);
                yield return Capture(size,"MainMenu"); Click("Settings"); yield return null;
                var settings=FindAnyObjectByType<UISettingsPanel>(); settings.Master.value=.37f;
                Check(Mathf.Approximately(AudioListener.volume,.37f),"Master volume applies");
                yield return Capture(size,"Settings"); Click("Back"); yield return null;
                Check(Mathf.Approximately(PlayerPrefs.GetFloat(UISettingsPanel.VolumeKey),.37f),"Master volume saves");
                Click("Play"); yield return new WaitForSecondsRealtime(.6f);
                Check(SceneManager.GetActiveScene().path==HubSceneFlowController.HubScenePath,"Play opens the existing hub");
                yield return Capture(size,"Hub");
                Check(!FindAnyObjectByType<ControlHintView>(FindObjectsInactive.Include).gameObject.activeSelf,"Hub has no generic controls board");
                var hubActor=FindAnyObjectByType<PlayerController>(); var hubBody=hubActor.GetComponent<Rigidbody2D>();
                Vector3 hubPosition=hubActor.transform.position;
                var wardrobeDoor=FindObjectsByType<HubInteractable>().First(door=>door.Screen==HubScreen.Wardrobe);
                hubBody.position=wardrobeDoor.transform.position; Physics2D.SyncTransforms(); yield return null; yield return null;
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Interact,"Existing hub proximity shows E");
                yield return Capture(size,"Interact"); hubBody.position=hubPosition; Physics2D.SyncTransforms(); yield return null;
                Click("WorldMap"); yield return null;
                var hub=FindAnyObjectByType<HubUIController>(); var player=FindAnyObjectByType<PlayerController>();
                Check(hub.CurrentScreen==HubScreen.Worlds && !player.ControlEnabled && Time.timeScale==1,"Hub modal owns player input");
                yield return Capture(size,"WorldMap");
                var map=FindAnyObjectByType<WorldMapPanelUI>(); map.SelectWorld(2); yield return null;
                Check(!FindButton("Play level").interactable,"Unassigned/locked world cannot launch");
                map.SelectWorld(0); yield return null; Click("Play level"); yield return new WaitForSecondsRealtime(.4f);
                Check(SceneManager.GetActiveScene().path==GameManager.TutorialScene,"World map launches configured scene");
                yield return Capture(size,"HUD");
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Move,"Tutorial starts with A / D only");
                var tutorialActor=FindAnyObjectByType<PlayerController>(); var tutorialBody=tutorialActor.GetComponent<Rigidbody2D>();
                Vector3 tutorialPosition=tutorialActor.transform.position; tutorialBody.simulated=false;
                tutorialActor.transform.position=new Vector3(2,1,0); yield return null; yield return null;
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Jump,"Existing first obstacle shows SPACE");
                yield return Capture(size,"Jump"); tutorialBody.linearVelocity=Vector2.up*2; yield return null; yield return null;
                Check(!FindAnyObjectByType<ControlHintView>(FindObjectsInactive.Include).gameObject.activeSelf,"Demonstrated jump dismisses hint");
                tutorialBody.linearVelocity=Vector2.zero; tutorialActor.transform.position=new Vector3(16,1,0); yield return null; yield return null;
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Sprint,"Safe tutorial runway shows SHIFT");
                yield return Capture(size,"Sprint");
                tutorialActor.transform.position=tutorialPosition; tutorialBody.simulated=true; yield return null;
                Click("Pause"); yield return null;
                Check(GameManager.Instance.State==GameState.Paused && Time.timeScale==0,"Pause freezes time");
                yield return Capture(size,"Pause"); Click("SETTINGS"); yield return null;
                Check(FindAnyObjectByType<GameInterface>().SettingsOpen && Time.timeScale==0,"Settings retains pause");
                Check(FindAnyObjectByType<GameInterface>().HandleEscape(),"Escape closes settings first"); yield return null;
                Click("CONTINUE"); yield return null;
                Check(GameManager.Instance.State==GameState.Playing && Time.timeScale==1 && FindAnyObjectByType<PlayerController>().ControlEnabled,"Continue restores controls");
                var goal=FindAnyObjectByType<TutorialGoal>(); var collider=FindAnyObjectByType<PlayerDeath>().GetComponent<Collider2D>();
                goal.SendMessage("OnTriggerEnter2D",collider); yield return new WaitForSecondsRealtime(.2f);
                Check(goal.IsComplete,"Existing goal completes"); yield return Capture(size,"Complete");
                Check(!FindButton("Next level").interactable,"Unavailable next level stays disabled");
                Click("Lobby"); yield return new WaitForSecondsRealtime(.4f);
                hub=FindAnyObjectByType<HubUIController>(); Click("Wardrobe"); yield return null;
                yield return Capture(size,"Wardrobe");
                Click("Outfit forest"); yield return null;
                Check(!FindButton("Equip outfit").interactable,"Locked outfit cannot equip");
                hub.Close(); yield return new WaitForSecondsRealtime(.25f); Click("Workshop");
                yield return WaitForWorkshop(hub);
                Check(hub.Workshop.View.Interactable,"Workshop cutscene finishes with live UI");
                yield return Capture(size,"Workshop");
                Check(!hub.Workshop.View.GetComponentsInChildren<Button>().First(b=>b.name=="Purchase upgrade").interactable,"Unconfigured upgrade price disables purchase");
                hub.Close(); yield return WaitForWorkshop(hub);
                Check(FindAnyObjectByType<PlayerController>().ControlEnabled,"Leaving workshop restores input");
                GameManager.Instance.LoadScene(Trapple.DeceptiveForest.ForestGoal.ScenePath); yield return new WaitForSecondsRealtime(.35f);
                Check(FindAnyObjectByType<Trapple.DeceptiveForest.ForestUI>().Root.resolvedStyle.display==UnityEngine.UIElements.DisplayStyle.None,"Forest uses the shared UI");
                Check(!FindAnyObjectByType<ControlHintView>(FindObjectsInactive.Include).gameObject.activeSelf,"Forest does not repeat tutorial controls");
                yield return Capture(size,"ForestWarning");
                var forestUI=typeof(Trapple.DeceptiveForest.ForestUI);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
                forestUI.GetField("pickupShown",flags).SetValue(null,false); forestUI.GetField("throwShown",flags).SetValue(null,false);
                var forestActor=FindAnyObjectByType<PlayerController>(); var forestBody=forestActor.GetComponent<Rigidbody2D>();
                var stick=FindObjectsByType<Trapple.DeceptiveForest.ForestStick>().OrderBy(item=>item.transform.position.x).First();
                forestBody.simulated=false; forestActor.transform.position=stick.transform.position+Vector3.left*.6f; Physics2D.SyncTransforms();
                yield return null; yield return null;
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Interact,"Existing pickup context shows E");
                yield return Capture(size,"Pickup"); Check(stick.TryPickup(),"Original stick pickup works"); yield return null; yield return null;
                Check(FindAnyObjectByType<ControlHintView>().Current==ControlHint.Throw,"Existing held-stick context shows F");
                yield return Capture(size,"Throw"); Check(stick.TryThrow(),"Original stick throw works"); yield return null; yield return null;
                Check(!FindAnyObjectByType<ControlHintView>(FindObjectsInactive.Include).gameObject.activeSelf,"Throw prompt leaves when stick is released");
                yield return new WaitForSecondsRealtime(5.2f);
                Check(!FindAnyObjectByType<TimedNotice>(FindObjectsInactive.Include).IsVisible,"Warning expires after its entry event");
                yield return Capture(size,"ForestIdle");
                Click("Pause"); yield return null; yield return Capture(size,"ForestPause");
                Click("RESTART LEVEL"); yield return new WaitForSecondsRealtime(.35f);
                Check(Time.timeScale==1 && GameManager.Instance.State==GameState.Playing,"Restart clears pause");
            }
            File.WriteAllLines(Path.Combine(output,"checks.txt"),checks); File.WriteAllText(Path.Combine(output,"result.txt"),failed?"FAILED":"PASS");
            Restore();
#if UNITY_EDITOR
            EditorApplication.isPlaying=false;
#else
            Application.Quit(failed?1:0);
#endif
        }
        private IEnumerator WaitForWorkshop(HubUIController hub)
        {
            float until=Time.realtimeSinceStartup+20;
            while(hub.Workshop.IsBusy && Time.realtimeSinceStartup<until) yield return null;
            Check(!hub.Workshop.IsBusy,"Workshop transition completes"); yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Capture(Vector2Int size,string name)
        {
            yield return new WaitForSecondsRealtime(.15f); Canvas.ForceUpdateCanvases();
            Check(FindObjectsByType<EventSystem>().Length==1,"One EventSystem: "+name);
            var buttons=FindObjectsByType<Button>(); var seen=new List<Rect>();
            foreach(var button in buttons)
            {
                if(!button.isActiveAndEnabled) continue;
                Vector3[] corners=new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                Check(corners.All(p=>p.x>=-2 && p.y>=-2 && p.x<=Screen.width+2 && p.y<=Screen.height+2),"Button bounds: "+name+" / "+button.name);
                if(button.interactable)
                {
                    var hits=new List<RaycastResult>(); var center=(corners[0]+corners[2])*.5f;
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);
                    Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Pointer hit: "+name+" / "+button.name);
                }
            }
            foreach(var text in FindObjectsByType<TMP_Text>()) if(text.isActiveAndEnabled)
            {
                Check(text.font!=null && text.font.name.Contains("Thaleah"),"Original Thaleah: "+text.name);
                Check(!text.isTextOverflowing,"Text fits: "+name+" / "+text.name);
            }
            var hint=FindAnyObjectByType<ControlHintView>();
            if(hint!=null)
            {
                var hintRect=(RectTransform)hint.transform;
                Check(hintRect.rect.width<=500 && Mathf.Abs(hintRect.rect.height-80)<1,"Compact context hint: "+name);
                Vector3[] corners=new Vector3[4]; hintRect.GetWorldCorners(corners);
                Check(corners.All(p=>p.x>=0 && p.y>=0 && p.x<=Screen.width && p.y<=Screen.height),"Context hint stays inside screen: "+name);
                var keyImages=hint.GetComponentsInChildren<Image>().Where(img=>img.sprite!=null).ToArray();
                Check(keyImages.Length==(hint.Current==ControlHint.Move?2:1),"Only relevant keys: "+name);
                Check(keyImages.All(img=>img.preserveAspect),"Key art preserves aspect: "+name);
                var hintBounds=new Rect(corners[0],corners[2]-corners[0]);
                foreach(var button in buttons.Where(value=>value.isActiveAndEnabled))
                {
                    Vector3[] buttonCorners=new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(buttonCorners);
                    Check(!hintBounds.Overlaps(new Rect(buttonCorners[0],buttonCorners[2]-buttonCorners[0])),"Context hint clears button: "+name+" / "+button.name);
                }
                var actor=FindAnyObjectByType<PlayerController>(); var camera=Camera.main;
                if(actor!=null && camera!=null)
                {
                    var actorRect=new Rect((Vector2)camera.WorldToScreenPoint(actor.transform.position)-Vector2.one*32,Vector2.one*64);
                    Check(!actorRect.Overlaps(new Rect(corners[0],corners[2]-corners[0])),"Context hint clears player: "+name);
                }
            }
            yield return new WaitForEndOfFrame();
            Debug.developerConsoleVisible=false;
            var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+name+".png"),image.EncodeToPNG()); Destroy(image);
        }
        private static Button FindButton(string name) => FindObjectsByType<Button>().First(b=>b.name==name && b.isActiveAndEnabled);
        private void Click(string name)
        {
            var button=FindButton(name); Check(button.interactable,"Clickable: "+name);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private void Check(bool value,string label) { checks.Add((value?"PASS ":"FAIL ")+label); if(!value) { failed=true; Debug.LogError("STORYBOOK CHECK: "+label); } }
        private void OnLog(string message,string trace,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) { failed=true; File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n"); } }
        private void Restore()
        {
            if(!captured) return; captured=false; Application.logMessageReceived-=OnLog;
            if(hadProgress) PlayerPrefs.SetString(PreviewSaveService.Key,savedProgress); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            if(hadVolume) PlayerPrefs.SetFloat(UISettingsPanel.VolumeKey,savedVolume); else PlayerPrefs.DeleteKey(UISettingsPanel.VolumeKey);
            PlayerPrefs.Save(); AudioListener.volume=savedVolume;
            Application.runInBackground=previousBackground;
#if UNITY_EDITOR
            if(previousGameViewSize>=0)
            {
                var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                var view=EditorWindow.GetWindow(type);
                type.GetProperty("selectedSizeIndex").SetValue(view,previousGameViewSize); previousGameViewSize=-1;
            }
#endif
        }
        private static void SetResolution(Vector2Int size)
        {
#if UNITY_EDITOR
            var assembly=typeof(Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance").GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var entry=Activator.CreateInstance(sizeType,new[]{Enum.Parse(kind,"FixedResolution"),(object)size.x,size.y,"Trapple UI QA "+size.x+"x"+size.y});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});
            int index=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null)-1;
            var viewType=assembly.GetType("UnityEditor.GameView"); var view=EditorWindow.GetWindow(viewType);
            var property=viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if(previousGameViewSize<0) previousGameViewSize=(int)property.GetValue(view);
            property.SetValue(view,index); view.Focus(); view.Repaint();
#else
            Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
        }
        private void OnApplicationQuit() => Restore();
        private void OnDestroy() => Restore();
    }
}
