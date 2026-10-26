using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class ExpandedLevelVisualTests
    {
        [UnityTest]
        public IEnumerator CaptureSectionScenery()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture-expanded-levels") < 0)
                Assert.Ignore("Opt-in graphics capture: use -capture-expanded-levels without -nographics.");
            string output = Path.Combine(Application.dataPath, "../Docs/LevelExpansion/Images");
            Directory.CreateDirectory(output);
            foreach (string scene in new[] { GameManager.TutorialScene, ForestGoal.ScenePath })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                var course = Object.FindAnyObjectByType<LevelCourse>();
                var player = course.Player;
                player.GetComponent<PlayerController>().enabled = false;
                player.GetComponent<Rigidbody2D>().simulated = false;
                var camera = Camera.main; camera.aspect = 16f / 9;
                for (int i = 0; i < course.Sections.Length; i++)
                {
                    float x = course.Sections[i].startX + (i == 0 ? 3 : 9.2f);
                    var hit = Physics2D.Raycast(new Vector2(x, 8), Vector2.down, 12, 1 << 6);
                    player.transform.position = new Vector3(x, hit.collider == null ? .5f : hit.point.y + .5f, 0);
                    yield return new WaitForSeconds(.8f);
                    var target = new RenderTexture(1280, 720, 24);
                    var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    var previous = RenderTexture.active;
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, (scene == GameManager.TutorialScene ? "meadow" : "forest") + $"-{i + 1:00}.png"), image.EncodeToPNG());
                    camera.targetTexture = null; RenderTexture.active = previous;
                    target.Release(); Object.Destroy(target); Object.Destroy(image);
                }
            }
        }
    }
}
