using System;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.UI
{
    // Presentation-only regions in the existing tutorial. They never gate movement,
    // alter obstacles, or change completion/respawn conditions.
    public sealed class TutorialControlHints : MonoBehaviour
    {
        [Serializable]
        public struct Region
        {
            public ControlHint hint;
            public Vector2 horizontalRange;
        }

        [SerializeField] private PlayerController player;
        [SerializeField] private Region[] regions;
        [SerializeField, Min(1)] private float displaySeconds = 6;
        private bool[] dismissed;
        private float[] ages;
        private Rigidbody2D body;
        private PlayerDeath death;
        private float startX;
        public ControlHint Current { get; private set; }

        public void Configure(PlayerController actor, Region[] areas) { player = actor; regions = areas; }
        private void Awake()
        {
            dismissed = new bool[regions.Length]; ages = new float[regions.Length];
            if (player == null) return;
            body = player.GetComponent<Rigidbody2D>(); death = player.GetComponent<PlayerDeath>();
            startX = player.transform.position.x;
        }
        private void Update()
        {
            Current = ControlHint.None;
            if (player == null || !player.ControlEnabled || death != null && death.IsDead ||
                GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            float x = player.transform.position.x;
            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                bool demonstrated = region.hint == ControlHint.Move ? Mathf.Abs(x - startX) >= 1.5f :
                    region.hint == ControlHint.Jump ? body.linearVelocity.y > .5f :
                    region.hint == ControlHint.Sprint && player.IsSprinting;
                if (demonstrated || x > region.horizontalRange.y) dismissed[i] = true;
                if (dismissed[i] || x < region.horizontalRange.x || x > region.horizontalRange.y) continue;
                ages[i] += Time.deltaTime;
                if (ages[i] >= displaySeconds) { dismissed[i] = true; continue; }
                Current = region.hint;
                break;
            }
        }
    }
}
