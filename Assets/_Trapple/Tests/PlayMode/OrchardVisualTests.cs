using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Trapple.Core;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class OrchardVisualTests
    {
        [UnityTest]
        public IEnumerator CaptureAuthoredOrchardTrial()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture-orchard") < 0) Assert.Ignore("Opt-in GPU capture: -capture-orchard");
            Time.timeScale = 1; MeadowBossEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Orchard capture systems", typeof(GameManager));
            GameManager.Instance.LoadScene("Assets/_Trapple/Scenes/BossArenas/MeadowBossTest.unity"); yield return null; yield return null;
            var encounter = Object.FindAnyObjectByType<MeadowBossEncounter>();
            var player = Object.FindAnyObjectByType<PlayerDeath>(); var control = player.GetComponent<PlayerController>(); var body = player.GetComponent<Rigidbody2D>();
            control.enabled = false; yield return new WaitForSeconds(.7f); body.simulated = false;
            Capture("01-approach", 934, 524);
            var goal = Object.FindAnyObjectByType<TutorialGoal>(); player.transform.position = goal.transform.position; body.position = player.transform.position;
            encounter.EnterArena(); yield return new WaitForSeconds(1.8f); Capture("02-introduction", 934, 524);
            encounter.SkipIntro(); yield return new WaitForSeconds(1.1f); Capture("03-survival", 934, 524); Capture("03a-survival-wide", 1280, 720);
            // Capture one production telegraph, then the real timeout transition in the unrecorded test scene.
            typeof(Trapple.Bosses.ForestGuardianController).GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(encounter.Boss, 0f);
            yield return null; yield return new WaitForSeconds(.15f); Capture("04-root-warning", 934, 524);
            typeof(MeadowBossEncounter).GetField("<TimeRemaining>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(encounter, .02f);
            yield return new WaitForSeconds(3.5f); Capture("05-victory", 934, 524);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Complete));
            // Both result buttons remain enabled after the existing pause/death UI has updated.
            Canvas.ForceUpdateCanvases();
            foreach (var button in Object.FindObjectsByType<Button>())
                if (button.name == "Continue" || button.name == "Go to Hub") Assert.That(button.interactable, Is.True);
            GameManager.Instance.ReturnToMenu(); yield return null;
        }
        private static void Capture(string name, int width, int height)
        {
            string folder = Path.Combine(Application.dataPath, "../Docs/OrchardWarden/Images"); Directory.CreateDirectory(folder);
            var camera = Camera.main; camera.aspect = (float)width / height;
            var canvases = Object.FindObjectsByType<Canvas>(); var modes = new RenderMode[canvases.Length]; var views = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; views[i] = canvases[i].worldCamera;
                if (modes[i] == RenderMode.ScreenSpaceOverlay) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1; }
            }
            var target = new RenderTexture(width, height, 24); var texture = new Texture2D(width, height, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous;
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = views[i]; }
            target.Release(); Object.Destroy(target); Object.Destroy(texture);
        }
    }
}
