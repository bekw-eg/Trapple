using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class AppiRunTests
    {
        private PlayerController player;
        private PlayerSpriteAnimator animation;
        private Rigidbody2D body;
        private SpriteRenderer renderer;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Tutorial");
            player = Object.FindAnyObjectByType<PlayerController>();
            body = player.GetComponent<Rigidbody2D>();
            animation = player.GetComponent<PlayerSpriteAnimator>();
            renderer = player.GetComponent<SpriteRenderer>();
            yield return new WaitForSeconds(.3f);
            player.enabled = false;
        }

        [UnityTest]
        public IEnumerator RealVelocityPlaysTheWholeCycleThenIdleAndPreservesJump()
        {
            // Keep movement inside the safe spawn area while real Physics2D runs.
            foreach (float direction in new[] { 1f, -1f })
            {
                typeof(PlayerController).GetField("sprintHeld", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
                typeof(PlayerController).GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, direction);
                var seen = new HashSet<Sprite>();
                float end = Time.time + 1.5f;
                while (Time.time < end)
                {
                    body.linearVelocity = new Vector2(direction * .5f, body.linearVelocity.y);
                    yield return null;
                    if (animation.State == (direction > 0 ? PlayerVisualState.RunRight : PlayerVisualState.RunLeft))
                    {
                        seen.Add(renderer.sprite);
                        Assert.That(renderer.flipX, Is.False, "Both directions have authored frames");
                        var frames = direction > 0 ? animation.Animations.walkRight : animation.Animations.walkLeft;
                        Assert.That(renderer.sprite, Is.EqualTo(frames[animation.FrameIndex]));
                    }
                }
                Assert.That(seen.Count, Is.EqualTo(8), "Every run pose must reach the renderer");
            }
            body.linearVelocity = Vector2.zero;
            yield return null;
            yield return null;
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.Idle));
            Assert.That(animation.Animations.idle, Does.Contain(renderer.sprite));
            body.linearVelocity = new Vector2(0, 10);
            yield return new WaitForSeconds(.12f);
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.Rising));
            Assert.That(animation.Animations.jump, Does.Contain(renderer.sprite));
            yield return new WaitForSeconds(.35f);
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.Falling));
        }

        [UnityTest]
        public IEnumerator DirectionChangeKeepsPhaseAndVelocityWinsOverDisabledInput()
        {
            player.SetControlEnabled(false);
            body.linearVelocity = new Vector2(.5f, 0);
            yield return new WaitForSeconds(.25f);
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.WalkRight));
            Assert.That(animation.FrameIndex, Is.GreaterThan(0));
            int phase = animation.FrameIndex;
            body.linearVelocity = new Vector2(-.5f, 0);
            // Evaluate the direction switch in the same frame, with no intervening physics.
            typeof(PlayerSpriteAnimator).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animation, null);
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.WalkLeft));
            Assert.That(animation.FrameIndex, Is.EqualTo(phase));
            Assert.That(renderer.sprite, Is.EqualTo(animation.Animations.walkingLeft[phase]));
        }
    }
}
