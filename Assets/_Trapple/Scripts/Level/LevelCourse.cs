using System;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    /// <summary>Scene-owned section boundaries and design estimates, never a gameplay speed limit.</summary>
    public sealed class LevelCourse : MonoBehaviour
    {
        [Serializable]
        public struct Section
        {
            public string name;
            public float startX, endX, estimatedSeconds;
        }

        [SerializeField] private PlayerDeath player;
        [SerializeField] private Vector2 worldBounds;
        [SerializeField] private Section[] sections;
        public PlayerDeath Player => player;
        public Vector2 WorldBounds => worldBounds;
        public Section[] Sections => sections;

        public int SectionAt(float x)
        {
            for (int i = 0; i < sections.Length - 1; i++)
                if (x < sections[i].endX) return i;
            return sections.Length - 1;
        }
    }
}
