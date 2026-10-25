using System;
using System.Collections;
using System.IO;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    // Opt-in native-input smoke probe. It never runs in the Editor or a normal player session.
    public sealed class ForestKeyboardCheck : MonoBehaviour
    {
        private string output;
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !Debug.isDebugBuild) return;
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-forest-keyboard-check");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Forest native keyboard check").AddComponent<ForestKeyboardCheck>();
            runner.output = Path.GetFullPath(args[index + 1]); Application.runInBackground = true;
            runner.StartCoroutine(runner.Run());
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(1);
            var player = FindAnyObjectByType<PlayerController>(); var body = player.GetComponent<Rigidbody2D>();
            yield return Key(KeyCode.Space); yield return new WaitForSecondsRealtime(.15f);
            Check(body.linearVelocity.y > 0, "Space jumps");
            yield return new WaitForSecondsRealtime(1);
            yield return Key(KeyCode.Escape); yield return new WaitForSecondsRealtime(.15f);
            Check(GameManager.Instance.State == GameState.Paused && Time.timeScale == 0, "Escape pauses");
            yield return Key(KeyCode.Escape); yield return new WaitForSecondsRealtime(.15f);
            Check(GameManager.Instance.State == GameState.Playing && Time.timeScale == 1 && player.ControlEnabled, "Escape resumes");
            yield return Key(KeyCode.Space); yield return new WaitForSecondsRealtime(.15f);
            Check(body.linearVelocity.y > 0, "Space after resume still jumps");
            yield return new WaitForSecondsRealtime(1);
            body.position = new Vector2(36.3f, .65f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(.7f);
            yield return Key(KeyCode.E); yield return null;
            var stick = FindAnyObjectByType<ForestStick>(); Check(stick.IsHeld, "E picks up");
            body.position = new Vector2(38.8f, .65f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(.7f);
            yield return Key(KeyCode.F); yield return new WaitForSecondsRealtime(.6f);
            Check(FindAnyObjectByType<ForestStickTarget>().IsOpen && !stick.IsHeld, "F throws and opens barrier");
            File.WriteAllText(Path.Combine(output, "result.txt"), failed ? "FAILED" : "PASS: native Space, Escape, Escape, Space, E, F; jump, pause, resume, pickup, throw and barrier.");
            File.WriteAllText(Path.Combine(output, "waiting.txt"), "COMPLETE");
        }
        private IEnumerator Key(KeyCode key)
        {
            File.WriteAllText(Path.Combine(output, "waiting.txt"), key.ToString());
            float deadline = Time.realtimeSinceStartup + 180;
            while (!Input.GetKeyDown(key) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Time.realtimeSinceStartup < deadline, "native key received: " + key);
        }
        private void Check(bool ok, string message)
        {
            if (!ok) failed = true;
            File.AppendAllText(Path.Combine(output, "checks.txt"), (ok ? "PASS " : "FAIL ") + message + "\n");
        }
    }
}
