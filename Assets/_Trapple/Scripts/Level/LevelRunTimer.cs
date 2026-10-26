using System;
using System.IO;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    /// <summary>One scene load is one attempt. Pauses and failed attempts never inflate a successful run.</summary>
    [RequireComponent(typeof(LevelCourse))]
    public sealed class LevelRunTimer : MonoBehaviour
    {
        [Serializable]
        public sealed class RunRecord
        {
            public string scene, utc, outcome, source;
            public float activeSeconds;
            public float[] sectionSeconds;
        }

        [SerializeField] private bool showDuringPlaytest;
        private LevelCourse course;
        private TutorialGoal meadowGoal;
        private ForestGoal forestGoal;
        private bool started, finished;
        private int furthestSection;
        private float[] sectionSeconds;
        public float ActiveSeconds { get; private set; }
        public bool IsFinished => finished;
        public bool IsSuccessful { get; private set; }
        public float[] SectionSeconds => sectionSeconds;

        private void Start()
        {
            course = GetComponent<LevelCourse>();
            sectionSeconds = new float[course.Sections.Length];
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (meadowGoal == null) meadowGoal = root.GetComponentInChildren<TutorialGoal>();
                if (forestGoal == null) forestGoal = root.GetComponentInChildren<ForestGoal>();
            }
        }

        private void LateUpdate()
        {
            if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F8)) showDuringPlaytest = !showDuringPlaytest;
            if (finished || course.Player == null) return;
            if (course.Player.IsDead) { Finish(false); return; }
            if ((meadowGoal != null && meadowGoal.IsComplete) || (forestGoal != null && forestGoal.IsComplete))
            { Finish(true); return; }
            if (Time.timeScale <= 0 || !course.Player.GetComponent<PlayerController>().ControlEnabled) return;
            // Begin on the first real displacement; loading/reading controls before moving is excluded.
            if (!started && Mathf.Abs(course.Player.GetComponent<Rigidbody2D>().linearVelocity.x) > .1f) started = true;
            if (!started) return;
            furthestSection = Mathf.Max(furthestSection, course.SectionAt(course.Player.transform.position.x));
            ActiveSeconds += Time.deltaTime;
            sectionSeconds[furthestSection] += Time.deltaTime;
        }

        private void Finish(bool success)
        {
            finished = true; IsSuccessful = success;
            var record = new RunRecord { scene = gameObject.scene.path, utc = DateTime.UtcNow.ToString("o"),
                outcome = success ? "success" : "death", source = Application.isBatchMode ? "automation" : "player",
                activeSeconds = ActiveSeconds, sectionSeconds = sectionSeconds };
            string json = JsonUtility.ToJson(record);
            Debug.Log("TRAPPLE RUN " + json);
            try { File.AppendAllText(Path.Combine(Application.persistentDataPath, "level-runs.jsonl"), json + Environment.NewLine); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { Debug.LogWarning("Could not save playtest timing: " + exception.Message); }
        }

        private void OnGUI()
        {
            if (course == null || (!showDuringPlaytest && !IsSuccessful)) return;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) return;
            GUI.Box(new Rect(16, Screen.height - 90, 320, 64),
                $"Попытка: {TimeSpan.FromSeconds(ActiveSeconds):mm\\:ss} · {furthestSection + 1}/{course.Sections.Length}\n" +
                (IsSuccessful ? "Успешное прохождение" : "F8 · замер прохождения"));
        }
    }
}
