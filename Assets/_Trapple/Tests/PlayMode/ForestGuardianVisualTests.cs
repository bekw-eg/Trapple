using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class ForestGuardianVisualTests
    {
        [UnityTest]
        public IEnumerator CaptureAuthoredEncounter()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture-guardian") < 0)
                Assert.Ignore("Opt-in rendered capture: run with -capture-guardian and graphics enabled.");
            Time.timeScale = 1;
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Capture systems", typeof(GameManager));
            GameManager.Instance.LoadScene(ForestGoal.ScenePath);
            yield return null; yield return null;
            var player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false; player.GetComponent<Rigidbody2D>().simulated = false;
            yield return new WaitForSeconds(.8f);
            Capture("01-route", 1280, 720);
            // The dedicated test scene intentionally has no world record in the production configuration.
            GameManager.Instance.LoadScene("Assets/_Trapple/Scenes/BossArenas/ForestGuardianTest.unity");
            yield return null; yield return null;
            var encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>();
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false; player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position += Vector3.right * 5;
            encounter.Inventory.Restore(8); encounter.EnterArena();
            yield return new WaitForSeconds(1.8f);
            Capture("02-introduction", 1280, 720);
            encounter.SkipIntro();
            yield return new WaitForSeconds(.65f);
            encounter.Guardian.Health.TryTakeDamage(1);
            yield return new WaitForSeconds(.25f);
            Capture("03-combat", 1280, 720);
            encounter.Guardian.Resolve(true);
            yield return new WaitForSeconds(2.9f);
            Capture("04-victory", 1280, 720);
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Complete));
            GameManager.Instance.ReturnToMenu(); yield return null;
        }

        private static void Capture(string name, int width, int height)
        {
            string output = Path.Combine(Application.dataPath, "../Docs/ForestGuardian/Images"); Directory.CreateDirectory(output);
            var camera = Camera.main; camera.aspect = (float)width / height;
            var canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length]; var cameras = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera;
                if (modes[i] == RenderMode.ScreenSpaceOverlay)
                { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1; }
            }
            var target = new RenderTexture(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous;
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
            target.Release(); Object.Destroy(target); Object.Destroy(image);
        }
    }
}
