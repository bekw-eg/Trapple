using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Trapple.DeceptiveForest;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    // Execute the authored route with the production controller and Physics2D. Never teleport or disable hazards.
    internal static class ExpandedRoutePlayback
    {
        [Serializable] private sealed class Command { public string action, context; public float x, seconds; }
        [Serializable] private sealed class Route { public Command[] commands; }
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static IEnumerator Continue(PlayerController player, PlayerDeath death, bool forest)
        {
            var body = player.GetComponent<Rigidbody2D>();
            var path = Path.Combine(Application.dataPath, "../Docs/LevelExpansion/" + (forest ? "forest" : "meadow") + "-route.json");
            var route = JsonUtility.FromJson<Route>(File.ReadAllText(path));
            var camera = Camera.main;
            var bounds = Object.FindAnyObjectByType<LevelCourse>().WorldBounds;
            int completedCommands = 0;
            foreach (var command in route.commands)
            {
                switch (command.action)
                {
                    case "walk": yield return Move(player, death, body, command.x, false, command.context); break;
                    case "hop": yield return Move(player, death, body, command.x, true, command.context); break;
                    case "wait": yield return new WaitForSeconds(command.seconds); break;
                    case "pickup":
                        var nearby = Object.FindObjectsByType<ForestStick>().OrderBy(s => Vector2.Distance(s.transform.position, body.position)).First();
                        Assert.That(nearby.TryPickup(), Is.True, command.context); break;
                    case "throw":
                        var held = Object.FindObjectsByType<ForestStick>().Single(s => s.IsHeld);
                        Assert.That(held.TryThrow(), Is.True, command.context); break;
                    default: Assert.Fail("Unknown traversal command " + command.action); break;
                }
                Assert.That(death.IsDead, Is.False, command.context);
                float half = camera.orthographicSize * camera.aspect;
                Assert.That(camera.transform.position.x, Is.InRange(bounds.x + half - .02f, bounds.y - half + .02f));
                if (++completedCommands % 50 == 0)
                    Debug.Log($"EXPANDED ROUTE progress {player.gameObject.scene.name}: {completedCommands}/{route.commands.Length}, x={body.position.x:F1}");
            }
            yield return null;
            var timer = Object.FindAnyObjectByType<LevelRunTimer>();
            Assert.That(timer.IsSuccessful, Is.True);
            Assert.That(timer.SectionSeconds.All(t => t > 0), Is.True, "Every section must be traversed");
            Debug.Log($"EXPANDED ROUTE physics completed: {player.gameObject.scene.name}; active={timer.ActiveSeconds:F2}s. Scripted input is NOT a novice timing playtest.");
        }

        private static IEnumerator Move(PlayerController player, PlayerDeath death, Rigidbody2D body, float target, bool jump, string context)
        {
            Tick(player);
            if (jump)
            {
                for (int i = 0; !player.IsGrounded && i < 25; i++)
                { Set(player, "moveInput", 0f); yield return new WaitForFixedUpdate(); Tick(player); }
                Assert.That(player.IsGrounded, Is.True, $"{context}: no support at {body.position} before {target}");
                Set(player, "jumpRequested", true);
            }
            float direction = Mathf.Sign(target - body.position.x);
            for (int i = 0; i < 350; i++)
            {
                if (!player.ControlEnabled && !death.IsDead) yield break;
                Set(player, "moveInput", (target - body.position.x) * direction > .15f ? direction : 0f);
                Tick(player); yield return new WaitForFixedUpdate();
                Assert.That(death.IsDead, Is.False, $"{context}: died {(jump ? "jumping" : "walking")} to {target} at {body.position}");
                if (!player.ControlEnabled) yield break; // The apple stops Appi at the edge of its trigger.
                if (i > 4 && (!jump || player.IsGrounded) && Mathf.Abs(body.linearVelocity.x) < .01f)
                {
                    Assert.That(Mathf.Abs(body.position.x - target), Is.LessThan(.4f), context + ": landed short");
                    yield break;
                }
            }
            Assert.Fail($"{context}: could not reach {target}; position={body.position}");
        }
        private static void Tick(PlayerController player) => typeof(PlayerController).GetMethod("FixedUpdate", Private).Invoke(player, null);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    }
}
