using System;
using System.Collections;
using System.IO;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    // Explicit development-player check of real OS key events, not simulated controller fields.
    public sealed class TutorialKeyboardCheck : MonoBehaviour
    {
        private string output;
        private bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !Debug.isDebugBuild) return;
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-tutorial-keyboard-check");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Tutorial native keyboard check").AddComponent<TutorialKeyboardCheck>();
            DontDestroyOnLoad(runner.gameObject);
            runner.output = Path.GetFullPath(args[index + 1]);
            runner.StartCoroutine(runner.Run());
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            SceneManager.LoadScene(GameManager.TutorialScene);
            yield return new WaitForSecondsRealtime(1f);
            var player = FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var manager = GameManager.Instance;
            yield return WaitForKey(KeyCode.Space, "jump");
            yield return new WaitForSecondsRealtime(.1f);
            Check(body.linearVelocity.y > 0f && player.ControlEnabled && manager.State == GameState.Playing, "Space jumps");
            yield return new WaitForSecondsRealtime(.9f);
            yield return WaitForKey(KeyCode.Escape, "pause");
            yield return null;
            Check(manager.State == GameState.Paused && Time.timeScale == 0f && !player.ControlEnabled, "Escape opens pause");
            Vector2 pausedPosition = body.position;
            yield return WaitForKey(KeyCode.Space, "ignore-space-while-paused");
            yield return new WaitForSecondsRealtime(.15f);
            Check(manager.State == GameState.Paused && body.position == pausedPosition, "Paused Space neither jumps nor submits UI");
            Debug.Log("TUTORIAL INPUT WAIT: click Continue");
            float until = Time.realtimeSinceStartup + 180f;
            while (manager.State == GameState.Paused && Time.realtimeSinceStartup < until) yield return null;
            Check(manager.State == GameState.Playing && player.ControlEnabled, "Continue restores control");
            yield return WaitForKey(KeyCode.Space, "jump-after-continue");
            yield return new WaitForSecondsRealtime(.1f);
            Check(body.linearVelocity.y > 0f && manager.State == GameState.Playing, "Space after Continue jumps without re-opening pause");
            yield return new WaitForSecondsRealtime(.9f);
            yield return WaitForKey(KeyCode.Escape, "pause-again");
            yield return null;
            Check(manager.State == GameState.Paused, "Escape re-opens pause");
            yield return WaitForKey(KeyCode.Escape, "resume-with-escape");
            yield return null;
            Check(manager.State == GameState.Playing && Time.timeScale == 1f, "Escape resumes");
            File.WriteAllText(Path.Combine(output, "result.txt"), failed ? "FAILED" : "PASS");
            Debug.Log("TUTORIAL INPUT COMPLETE: " + (failed ? "FAILED" : "PASS"));
        }

        private IEnumerator WaitForKey(KeyCode key, string phase)
        {
            Debug.Log("TUTORIAL INPUT WAIT: " + phase + " / " + key);
            float until = Time.realtimeSinceStartup + 180f;
            while (!Input.GetKeyDown(key) && Time.realtimeSinceStartup < until) yield return null;
            Check(Time.realtimeSinceStartup < until, "Native key received: " + key);
        }

        private void Check(bool value, string label)
        {
            if (!value) failed = true;
            Debug.Log("TUTORIAL INPUT " + (value ? "PASS: " : "FAIL: ") + label);
        }
    }
}
