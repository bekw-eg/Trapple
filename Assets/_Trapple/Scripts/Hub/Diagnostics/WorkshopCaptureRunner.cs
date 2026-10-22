using System;
using System.Collections;
using System.IO;
using System.Linq;
using Trapple.Player;
using Trapple.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Hub
{
    // Only an explicit development-player argument enables this validation harness.
    public sealed class WorkshopCaptureRunner : MonoBehaviour
    {
        private string output, oldSave;
        private bool hadSave, failed, capturedSave;
        private int[][] oldCosts;
        private HubConfiguration config;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if(Application.isEditor || !Debug.isDebugBuild) return;
            string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-trapple-workshop-capture");
            if(index<0 || index+1>=args.Length) return;
            var runner=new GameObject("Workshop verification").AddComponent<WorkshopCaptureRunner>();
            DontDestroyOnLoad(runner.gameObject); runner.output=Path.GetFullPath(args[index+1]); runner.StartCoroutine(runner.Run());
        }
        private IEnumerator Run()
        {
            Application.runInBackground=true; Directory.CreateDirectory(output);
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key); oldSave=PlayerPrefs.GetString(PreviewSaveService.Key,""); capturedSave=true;
            PlayerPrefs.DeleteKey(PreviewSaveService.Key); Application.logMessageReceived+=OnLog;
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return new WaitForSecondsRealtime(.5f);
            var hub=FindAnyObjectByType<HubUIController>(); var transition=hub.Workshop;
            var player=FindAnyObjectByType<PlayerController>(); player.GetComponent<Rigidbody2D>().position=new Vector2(-7.5f,.6f);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("01-outside");
            hub.Open(HubScreen.Workshop);
            yield return new WaitForSecondsRealtime(.55f); yield return Capture("02-walking");
            float until=Time.realtimeSinceStartup+12;
            while(!transition.Iris.IsVisible && Time.realtimeSinceStartup<until) yield return null;
            yield return new WaitForSecondsRealtime(.46f); yield return Capture("03-iris-close");
            yield return Wait(transition,WorkshopTransitionState.Inside);
            Check(transition.View.Interactable,"UI unlocked after reveal");
            yield return new WaitForSecondsRealtime(.2f);
            yield return Capture("04-speed-level-0");
            config=HubSceneFlowController.Instance.Configuration;
            oldCosts=Array.ConvertAll(config.upgrades,value=>value.costs);
            foreach(var def in config.upgrades) def.costs=new[]{1,1,1};
            var progress=HubSceneFlowController.Instance.Progression; progress.AddSeeds(20);
            foreach(var kind in new[]{UpgradeKind.Speed,UpgradeKind.Agility,UpgradeKind.Jump})
            {
                transition.View.Select(kind);
                for(int level=0;level<=3;level++)
                {
                    if(level>0) { transition.View.Purchase(); yield return new WaitForSecondsRealtime(.5f); }
                    if(kind==UpgradeKind.Jump) { transition.View.Select(kind); yield return new WaitForSecondsRealtime(JumpPreviewAnimator.AirTime(level)/2); }
                    else yield return new WaitForSecondsRealtime(.55f);
                    yield return Capture(kind+"-level-"+level);
                }
                Check(progress.Level(kind)==3,kind+" purchases saved");
            }
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1280,720)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.5f);
                yield return Capture(size.x+"x"+size.y+"-workshop");
            }
            hub.Close(); yield return Wait(transition,WorkshopTransitionState.Outside);
            Check(player.ControlEnabled && player.GetComponent<Rigidbody2D>().simulated,"Player control and physics restored");
            Check(Mathf.Abs(player.transform.position.x+8.65f)<.25f,"Player exits near the workshop");
            yield return Capture("05-exited");
            hub.Open(HubScreen.Workshop); yield return Wait(transition,WorkshopTransitionState.Inside);
            Check(transition.View.Interactable,"Repeated entry"); hub.Close(); yield return Wait(transition,WorkshopTransitionState.Outside);
            File.WriteAllText(Path.Combine(output,"result.txt"),failed?"FAILED":"PASS");
            Restore(); Application.Quit(failed?1:0);
        }
        private IEnumerator Wait(WorkshopTransitionController transition,WorkshopTransitionState state)
        {
            float until=Time.realtimeSinceStartup+15;
            while(transition.State!=state && Time.realtimeSinceStartup<until) yield return null;
            Check(transition.State==state,"Transition reaches "+state);
        }
        private IEnumerator Capture(string name)
        {
            yield return null;
            // A hidden Windows player has no presented backbuffer. Render the same live cameras and
            // overlay canvases into an explicit GPU target instead of reading an unpresented frame.
            var camera=Camera.main;
            var canvases=FindObjectsByType<Canvas>().Where(canvas=>canvas.isRootCanvas && canvas.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var previousCameras=Array.ConvertAll(canvases,canvas=>canvas.worldCamera);
            var distances=Array.ConvertAll(canvases,canvas=>canvas.planeDistance);
            var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            var previousTarget=camera.targetTexture; var previousActive=RenderTexture.active;
            var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=camera.nearClipPlane+1; }
                camera.targetTexture=target; Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); image.Apply();
            }
            finally
            {
                camera.targetTexture=previousTarget; RenderTexture.active=previousActive;
                for(int i=0;i<canvases.Length;i++) { canvases[i].renderMode=RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera=previousCameras[i]; canvases[i].planeDistance=distances[i]; }
                RenderTexture.ReleaseTemporary(target); Canvas.ForceUpdateCanvases();
            }
            if(name=="03-iris-close")
            {
                int left=Screen.width,right=0,bottom=Screen.height,top=0;
                var pixels=image.GetPixels32();
                for(int y=0;y<Screen.height;y++) for(int x=0;x<Screen.width;x++)
                {
                    var color=pixels[y*Screen.width+x];
                    if(color.r<10 && color.g<10 && color.b<10) continue;
                    left=Math.Min(left,x); right=Math.Max(right,x); bottom=Math.Min(bottom,y); top=Math.Max(top,y);
                }
                Check(right-left>150 && Math.Abs((right-left)-(top-bottom))<8,"Iris aperture is circular in rendered pixels");
                File.WriteAllText(Path.Combine(output,"iris-pixels.txt"),"Visible aperture: "+(right-left+1)+" x "+(top-bottom+1)+" px");
            }
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG()); Destroy(image);
        }
        private void Check(bool value,string message) { if(!value) failed=true; Debug.Log((value?"WORKSHOP PASS: ":"WORKSHOP FAIL: ")+message); }
        private void OnLog(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true; }
        private void Restore()
        {
            Application.logMessageReceived-=OnLog;
            if(config!=null && oldCosts!=null) for(int i=0;i<oldCosts.Length;i++) config.upgrades[i].costs=oldCosts[i];
            if(!capturedSave) return; capturedSave=false;
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,oldSave); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
        }
        private void OnApplicationQuit() => Restore();
    }
}
