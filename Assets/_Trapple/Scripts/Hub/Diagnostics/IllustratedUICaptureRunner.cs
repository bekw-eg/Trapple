using System;
using System.Collections;
using System.IO;
using System.Linq;
using Trapple.Hub.Illustrated;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Trapple.Hub
{
    // Explicit development-player verification. Never runs during a normal game session.
    public sealed class IllustratedUICaptureRunner : MonoBehaviour
    {
        private string output;
        private bool failed, hadSave;
        private string previousSave;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !Debug.isDebugBuild) return;
            string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-trapple-ui-capture");
            if(index<0 || index+1>=args.Length) return;
            var runner=new GameObject("Illustrated UI verification").AddComponent<IllustratedUICaptureRunner>();
            DontDestroyOnLoad(runner.gameObject); runner.output=Path.GetFullPath(args[index+1]); runner.StartCoroutine(runner.Run());
        }
        private IEnumerator Run()
        {
            Application.runInBackground=true; Directory.CreateDirectory(output);
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key); previousSave=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            PlayerPrefs.DeleteKey(PreviewSaveService.Key); Application.logMessageReceived+=OnLog;
            SceneManager.LoadScene(HubSceneFlowController.HubScenePath); yield return new WaitForSecondsRealtime(1);
            var facade=FindAnyObjectByType<HubUIController>(); var ui=facade.Illustrated;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
                Check(Screen.width==size.x && Screen.height==size.y,"Resolution "+size);
                foreach(var screen in new[]{HubScreen.Worlds,HubScreen.Workshop,HubScreen.Wardrobe})
                {
                    facade.Open(screen); yield return WaitForNavigation(facade);
                    Check(facade.CurrentScreen==screen,"Screen "+screen);
                    var buttons=screen==HubScreen.Workshop && facade.Workshop!=null?
                        facade.Workshop.View.GetComponentsInChildren<Button>():ui.GetComponentsInChildren<Button>();
                    foreach(var button in buttons)
                    {
                        var rect=(RectTransform)button.transform;
                        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                        var hits=new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                        Check(hits.Count>0 && hits[0].gameObject==button.gameObject,"Pointer hotspot "+button.name);
                    }
                    yield return new WaitForEndOfFrame();
                    var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+screen+".png"),image.EncodeToPNG()); Destroy(image);
                    yield return null;
                }
            }
            facade.Open(HubScreen.Worlds); yield return WaitForNavigation(facade);
            Click(ui,"worlds-secret_worlds"); Check(ui.Message=="Скоро","Secret worlds placeholder");
            Click(ui,"worlds-tabs-workshop"); Check(facade.CurrentScreen==HubScreen.Workshop,"Actual tab click");
            yield return WaitForNavigation(facade);
            if(facade.Workshop!=null)
            {
                facade.Workshop.View.Purchase(); Check(facade.Workshop.View.Feedback.Contains("не настроена"),"Unconfigured price guard");
                facade.Close(); yield return WaitForNavigation(facade);
            }
            else
            {
                Click(ui,"workshop-upgrade_buttons-speed"); Check(ui.Message.Contains("не настроена"),"Unconfigured price guard");
                Click(ui,"workshop-close"); yield return new WaitForSecondsRealtime(.25f);
            }
            Check(FindAnyObjectByType<Trapple.Player.PlayerController>().ControlEnabled,"Close restores gameplay");
            SceneManager.LoadScene(TrappleUIManager.ScenePath); yield return new WaitForSecondsRealtime(.7f);
            ui=FindAnyObjectByType<TrappleUIManager>(); Check(ui.CurrentScreen==HubScreen.Worlds,"Standalone scene opens map");
            Check(FindAnyObjectByType<Trapple.Player.PlayerController>()!=null,"Standalone scene contains the playable hub");
            ui.Close(); yield return new WaitForSecondsRealtime(.5f);
            Check(SceneManager.GetActiveScene().path==TrappleUIManager.ScenePath && !ui.IsOpen &&
                FindAnyObjectByType<Trapple.Player.PlayerController>().ControlEnabled,"Standalone close restores the game underneath");
            File.WriteAllText(Path.Combine(output,"result.txt"),failed?"FAILED":"PASS");
            Restore(); Application.Quit(failed?1:0);
        }
        private void Click(TrappleUIManager ui,string name)
        {
            var button=ui.GetComponentsInChildren<Button>().First(value=>value.name==name);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator WaitForNavigation(HubUIController facade)
        {
            float until=Time.realtimeSinceStartup+15;
            while(facade.Workshop!=null && facade.Workshop.IsBusy && Time.realtimeSinceStartup<until) yield return null;
            Check(facade.Workshop==null || !facade.Workshop.IsBusy,"Navigation completes");
            yield return new WaitForSecondsRealtime(.25f);
        }
        private void Check(bool result,string name) { if(!result) failed=true; Debug.Log((result?"UI PASS: ":"UI FAIL: ")+name); }
        private void OnLog(string message,string trace,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true; }
        private void Restore()
        {
            Application.logMessageReceived-=OnLog;
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,previousSave); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
        }
        private void OnApplicationQuit() => Restore();
    }
}
