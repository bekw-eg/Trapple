using System;
using System.Collections;
using System.IO;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Trapple.DeceptiveForest
{
    // Opt-in visual smoke checks in a development player. Normal play never creates this object.
    public sealed class ForestCaptureRunner : MonoBehaviour
    {
        private string output;
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !Debug.isDebugBuild) return;
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-forest-capture");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Forest visual verification").AddComponent<ForestCaptureRunner>();
            DontDestroyOnLoad(runner.gameObject); Application.runInBackground = true;
            runner.output = Path.GetFullPath(args[index + 1]); runner.StartCoroutine(runner.Run());
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(output); Application.logMessageReceived += OnLog;
            foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1366, 768) })
            {
                Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
                yield return SceneManager.LoadSceneAsync(ForestGoal.ScenePath);
                yield return new WaitForSecondsRealtime(1);
                Check(Screen.width == resolution.x && Screen.height == resolution.y, "resolution " + resolution);
                string prefix = resolution.x + "x" + resolution.y;
                var player = FindAnyObjectByType<PlayerController>(); player.enabled = false;
                var ui = FindAnyObjectByType<ForestUI>();
                yield return Capture(prefix + "-entrance");
                ui.Pause(); yield return new WaitForSecondsRealtime(.2f);
                Check(Time.timeScale == 0, "shared pause"); CheckBounds(ui.Root);
                yield return Capture(prefix + "-pause");
                Submit(ui.Root.Q<Button>("resume")); yield return null;
                Check(Time.timeScale == 1 && player.ControlEnabled, "resume");
                if (resolution.x == 1920)
                {
                    yield return new WaitForSeconds(4);
                    yield return Pose(new Vector2(16, .65f)); yield return Capture("section-02-false-ground");
                    yield return Pose(new Vector2(25.7f, 4f)); yield return Capture("section-03-vertical");
                    yield return Pose(new Vector2(36.3f, .65f));
                    var stick = FindAnyObjectByType<ForestStick>(); Check(stick.TryPickup(), "pickup");
                    yield return null; yield return Capture("section-04-stick-held");
                    yield return Pose(new Vector2(38.8f, .65f)); Check(stick.TryThrow(), "throw");
                    yield return new WaitForSeconds(.5f); Check(FindAnyObjectByType<ForestStickTarget>().IsOpen, "barrier opened");
                    yield return Capture("section-04-barrier-open");
                    yield return Pose(new Vector2(51, .65f)); yield return Capture("section-05-apple-bait");
                    yield return Pose(new Vector2(59, .65f));
                    var tree = FindAnyObjectByType<ForestFallingTree>(); tree.Trigger(player.GetComponent<PlayerDeath>());
                    yield return new WaitForSeconds(.45f); yield return Capture("section-06-tree-warning");
                    yield return new WaitForSeconds(1.7f); yield return Capture("section-06-tree-settled");
                    player.GetComponent<PlayerDeath>().Die(); yield return null; yield return Capture("death-retry");
                    yield return new WaitForSecondsRealtime(1);
                    player = FindAnyObjectByType<PlayerController>(); player.enabled = false; ui = FindAnyObjectByType<ForestUI>();
                }
                var body = player.GetComponent<Rigidbody2D>(); body.simulated = true; body.position = new Vector2(76, 1);
                yield return new WaitForSeconds(.7f);
                Check(FindAnyObjectByType<ForestGoal>().IsComplete, "goal"); CheckBounds(ui.Root);
                yield return Capture(prefix + "-complete");
                Submit(ui.Root.Q<Button>("retry")); yield return new WaitForSeconds(.5f);
                Check(!FindAnyObjectByType<ForestGoal>().IsComplete, "retry resets goal");
                ui = FindAnyObjectByType<ForestUI>(); ui.Pause(); yield return null;
                Submit(ui.Root.Q<Button>("hub")); yield return new WaitForSecondsRealtime(.7f);
                Check(SceneManager.GetActiveScene().path == HubSceneFlowController.HubScenePath && Time.timeScale == 1, "return to Hub");
            }
            yield return Capture("hub-return");
            File.WriteAllText(Path.Combine(output, "result.txt"), failed ? "FAILED" : "PASS: three resolutions, panels, stick, barrier, tree, death, retry and Hub return.");
            Application.logMessageReceived -= OnLog; Application.Quit(failed ? 1 : 0);
        }
        private IEnumerator Pose(Vector2 position)
        {
            var body = FindAnyObjectByType<PlayerController>().GetComponent<Rigidbody2D>();
            body.simulated = false; body.transform.position = position;
            yield return new WaitForSeconds(.8f);
        }
        private void CheckBounds(VisualElement root)
        {
            Rect screen = root.worldBound;
            foreach (string name in new[] { "window", "heading", "detail", "resume", "retry", "hub" })
            {
                var element = root.Q(name); if (element.resolvedStyle.display == DisplayStyle.None) continue;
                Rect rect = element.worldBound;
                Check(rect.width > 0 && rect.height > 0 && screen.Contains(rect.min) && screen.Contains(rect.max), "bounds " + name);
            }
        }
        private static void Submit(Button button) { button.Focus(); using (var evt = NavigationSubmitEvent.GetPooled()) button.SendEvent(evt); }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG()); Destroy(texture);
        }
        private void Check(bool condition, string message) { if (!condition) { failed = true; Debug.LogError("FOREST CAPTURE: " + message); } }
        private void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true; }
    }
}
