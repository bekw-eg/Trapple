using System;
using System.Collections;
using System.IO;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Trapple.Hub
{
    // Explicit opt-in development-player smoke test. Never active during ordinary play.
    public sealed class HubCaptureRunner : MonoBehaviour
    {
        private string output;
        private string previousSave;
        private bool hadSave;
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if(Application.isEditor || !Debug.isDebugBuild) return;
            string[] args=Environment.GetCommandLineArgs();
            int index=Array.IndexOf(args,"-hub-capture");
            bool keyboardCheck=index<0;
            if(keyboardCheck) index=Array.IndexOf(args,"-hub-keyboard-check");
            if(index<0 || index+1>=args.Length) return;
            Application.runInBackground=true;
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            var runner=new GameObject("Hub capture (development only)").AddComponent<HubCaptureRunner>();
            DontDestroyOnLoad(runner.gameObject);
            runner.output=Path.GetFullPath(args[index+1]);
            runner.StartCoroutine(keyboardCheck ? runner.CheckKeyboard() : runner.Run());
        }

        private IEnumerator CheckKeyboard()
        {
            Directory.CreateDirectory(output);
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key);
            previousSave=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            Application.logMessageReceived+=OnLog;
            SceneManager.LoadScene(HubSceneFlowController.HubScenePath);
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForSeconds(.3f);
            var ui=FindAnyObjectByType<HubUIController>();
            var player=FindAnyObjectByType<PlayerController>();
            var keys=new[] {KeyCode.Space,KeyCode.Escape,KeyCode.Escape,KeyCode.Space};
            for(int i=0;i<keys.Length;i++)
            {
                Debug.Log("HUB KEYBOARD WAIT: "+keys[i]);
                float deadline=Time.unscaledTime+180;
                while(!Input.GetKeyDown(keys[i]) && Time.unscaledTime<deadline) yield return null;
                Check(Time.unscaledTime<deadline,"native key received: "+keys[i]);
                yield return new WaitForSecondsRealtime(.25f);
                if(keys[i]==KeyCode.Space)
                    Check(!ui.IsOpen && player.ControlEnabled && player.GetComponent<Rigidbody2D>().linearVelocity.y>0,
                        "native Space jumps without opening settings (step "+i+")");
                else if(i==1) Check(ui.CurrentScreen==HubScreen.Settings && !player.ControlEnabled,"native Esc opens settings");
                else Check(!ui.IsOpen && player.ControlEnabled,"native Esc closes settings and restores movement");
                yield return Capture("keyboard-step-"+i);
                yield return new WaitForSecondsRealtime(.7f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),failed ? "FAILED" : "PASS — native Space, Esc, Esc, Space");
            RestoreSave();
            Debug.Log("HUB KEYBOARD COMPLETE");
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            hadSave=PlayerPrefs.HasKey(PreviewSaveService.Key);
            previousSave=PlayerPrefs.GetString(PreviewSaveService.Key,"");
            PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            Application.logMessageReceived+=OnLog;
            SceneManager.LoadScene(HubSceneFlowController.HubScenePath);
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(.25f);
            Check(FindAnyObjectByType<PlayerController>()?.IsGrounded==true,"grounded spawn");
            foreach(var resolution in new[] {new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768)})
            {
                Screen.SetResolution(resolution.x,resolution.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.7f);
                var ui=FindAnyObjectByType<HubUIController>();
                string prefix=resolution.x+"x"+resolution.y;
                Check(Screen.width==resolution.x && Screen.height==resolution.y,"screen "+prefix);
                yield return Capture(prefix+"-hub");
                var player=FindAnyObjectByType<PlayerController>();
                player.GetComponent<Rigidbody2D>().position=new Vector2(-7.5f,.6f);
                yield return new WaitForSecondsRealtime(.8f);
                var interaction=FindAnyObjectByType<HubInteractionController>();
                interaction.RefreshTarget();
                Check(interaction.Nearest?.Screen==HubScreen.Workshop,"workshop proximity");
                Check(interaction.TryInteract(),"workshop interaction");
                Check(!player.ControlEnabled && Time.timeScale==1,"modal control lock with live time");
                yield return new WaitForSecondsRealtime(.5f);
                CheckLayout(ui,"upgrade-cards");
                yield return Capture(prefix+"-workshop");
                ui.Open(HubScreen.Worlds);
                yield return new WaitForSecondsRealtime(.4f);
                CheckLayout(ui,"world-cards");
                Check(ui.Root.Q("world-cards").childCount==4,"four worlds");
                yield return Capture(prefix+"-worlds");
                ui.Open(HubScreen.Wardrobe);
                yield return new WaitForSecondsRealtime(.4f);
                CheckLayout(ui,"cosmetics");
                Check(ui.Root.Q<Button>("equip").text.Contains("EQUIPPED"),"equipped base appearance");
                yield return Capture(prefix+"-wardrobe");
                ui.Open(HubScreen.Settings);
                yield return new WaitForSecondsRealtime(.35f);
                yield return Capture(prefix+"-settings");
                ui.Close();
                yield return new WaitForSecondsRealtime(.3f);
                Check(player.ControlEnabled,"control restored");
            }
            var hubUI=FindAnyObjectByType<HubUIController>();
            hubUI.Open(HubScreen.Workshop);
            yield return new WaitForSecondsRealtime(.3f);
            for(int i=0;i<3;i++)
            {
                Submit(hubUI.Root.Q<Button>("buy-speed"));
                yield return null;
            }
            Check(HubSceneFlowController.Instance.Progression.Level(UpgradeKind.Speed)==3 &&
                hubUI.Root.Q<Button>("buy-speed").text=="MAX LEVEL","max upgrade stops purchases");
            Submit(hubUI.Root.Q<Button>("buy-jump"));
            yield return null;
            Check(!hubUI.Root.Q<Button>("buy-jump").enabledSelf,"insufficient currency disables purchase");
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture("hub-workshop-upgraded");
            hubUI.Open(HubScreen.Worlds);
            yield return null;
            Submit(hubUI.Root.Q<Button>("select-forest"));
            Check(!hubUI.Root.Q<Button>("launch-forest-0").enabledSelf,"locked forest cannot launch");
            Submit(hubUI.Root.Q<Button>("select-meadow"));
            yield return null;
            Submit(hubUI.Root.Q<Button>("launch-meadow-0"));
            yield return new WaitForSecondsRealtime(.8f);
            Check(SceneManager.GetActiveScene().name=="Tutorial","registered Tutorial launched");
            var goal=FindAnyObjectByType<TutorialGoal>();
            var tutorialPlayer=FindAnyObjectByType<PlayerController>();
            Check(goal!=null && tutorialPlayer!=null,"existing Tutorial intact");
            tutorialPlayer.GetComponent<Rigidbody2D>().position=goal.transform.position;
            yield return new WaitForSecondsRealtime(.4f);
            Check(goal.IsComplete && HubSceneFlowController.Instance.Progression.TutorialComplete,"Tutorial completion saved");
            yield return Capture("tutorial-complete-return");
            HubSceneFlowController.Instance.ReturnToHub();
            yield return new WaitForSecondsRealtime(.7f);
            Check(FindAnyObjectByType<PlayerController>().ControlEnabled,"return to hub controls");
            hubUI=FindAnyObjectByType<HubUIController>(); hubUI.Open(HubScreen.Worlds);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("hub-world-completed");
            File.WriteAllText(Path.Combine(output,"result.txt"),failed ? "FAILED — inspect player.log" : "PASS — 3 resolutions, 5 screens, upgrades, Tutorial completion and return");
            RestoreSave();
            Application.Quit(failed ? 1 : 0);
        }

        private void CheckLayout(HubUIController ui,string container)
        {
            Rect window=ui.Root.Q("window").worldBound;
            Rect viewport=ui.Root.Q("hub-root").worldBound;
            Check(window.xMin>=viewport.xMin && window.xMax<=viewport.xMax+.5f && window.yMin>=viewport.yMin && window.yMax<=viewport.yMax+.5f,"window within screen");
            foreach(var card in ui.Root.Q(container).Children())
                Check(card.worldBound.xMin>=window.xMin && card.worldBound.xMax<=window.xMax && card.worldBound.yMin>=window.yMin && card.worldBound.yMax<=window.yMax,"card within window: "+card.name);
        }
        private void Check(bool condition,string message)
        {
            if(condition) Debug.Log("HUB VERIFY PASS: "+message);
            else { failed=true; Debug.LogError("HUB VERIFY FAIL: "+message); }
        }
        private void OnLog(string condition,string trace,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true;
            if(type==LogType.Exception)
            {
                File.WriteAllText(Path.Combine(output,"result.txt"),"FAILED: "+condition);
                RestoreSave();
                Application.Quit(1);
            }
        }
        private static void Submit(Button button)
        {
            button.Focus(); using(var evt=NavigationSubmitEvent.GetPooled()) button.SendEvent(evt);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG()); Destroy(texture);
        }
        private void RestoreSave()
        {
            if(hadSave) PlayerPrefs.SetString(PreviewSaveService.Key,previousSave); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
        }
        private void OnApplicationQuit() { RestoreSave(); Application.logMessageReceived-=OnLog; }
    }
}
