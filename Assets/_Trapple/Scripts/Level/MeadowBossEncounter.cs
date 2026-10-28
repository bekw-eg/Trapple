using Trapple.Bosses;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Level
{
    public enum MeadowEncounterState { Route, Intro, Fighting, Resolving, Complete, Failed }

    /// <summary>Activates the existing Ancient Tree and E-lever fight only after the authored meadow route.</summary>
    public sealed class MeadowBossEncounter : MonoBehaviour
    {
        [SerializeField] private TutorialGoal goal;
        [SerializeField] private PlayerDeath player;
        [SerializeField] private BossController boss;
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private TutorialCamera follow;
        [SerializeField] private GameObject entranceBarrier;
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField, Min(1f)] private float cameraSize = 6f;
        [SerializeField, Range(1f, 4f)] private float introDuration = 2.5f;
        [SerializeField, Min(.1f)] private float resolutionDuration = 1.1f;
        private PlayerController control;
        private Rigidbody2D body;
        private float age, originalSize;
        private Vector3 cameraVelocity;
        public MeadowEncounterState State { get; private set; }
        public BossController Boss => boss;

        private void Start()
        {
            control = player.GetComponent<PlayerController>();
            body = player.GetComponent<Rigidbody2D>();
            originalSize = arenaCamera.orthographicSize;
            boss.FightFinished += Finished;
            if (entranceBarrier != null) entranceBarrier.SetActive(false);
            WorldCompletion.EnsureProgression();
        }

        private bool CanTick => player != null && !player.IsDead && Time.timeScale > 0f &&
            GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        public void EnterArena()
        {
            if (State != MeadowEncounterState.Route || !CanTick) return;
            State = MeadowEncounterState.Intro;
            age = 0f;
            control.SetControlEnabled(false);
            body.linearVelocity = Vector2.zero;
            if (entranceBarrier != null) entranceBarrier.SetActive(true);
            if (follow != null) follow.enabled = false;
            // Render the original boss during the pan, while its controller remains suspended.
            boss.enabled = false;
            boss.gameObject.SetActive(true);
        }

        public void SkipIntro()
        {
            if (State != MeadowEncounterState.Intro || !CanTick) return;
            State = MeadowEncounterState.Fighting;
            age = 0f;
            control.SetControlEnabled(true);
            boss.enabled = true;
        }

        private void Update()
        {
            if (player != null && player.IsDead && State != MeadowEncounterState.Complete && State != MeadowEncounterState.Failed)
            {
                State = MeadowEncounterState.Failed;
                boss.enabled = false;
                ReleaseCamera();
                return;
            }
            if (!CanTick) return;
            age += Time.deltaTime;
            if (State == MeadowEncounterState.Intro)
            {
                control.SetControlEnabled(false);
                if (age >= introDuration || Input.GetKeyDown(KeyCode.Return)) SkipIntro();
            }
            else if (State == MeadowEncounterState.Resolving && age >= resolutionDuration)
            {
                State = MeadowEncounterState.Complete;
                WorldCompletion.Record("meadow", gameObject.scene.path);
                goal.CompleteAfterBoss(player);
                ReleaseCamera();
                GameManager.Instance.CompleteLevel();
            }
        }

        private void LateUpdate()
        {
            if (arenaCamera == null || Time.deltaTime <= 0f ||
                (State != MeadowEncounterState.Intro && State != MeadowEncounterState.Fighting && State != MeadowEncounterState.Resolving)) return;
            arenaCamera.transform.position = Vector3.SmoothDamp(arenaCamera.transform.position,
                cameraPosition, ref cameraVelocity, .4f, Mathf.Infinity, Time.deltaTime);
            float aspect = Mathf.Max(.5f, arenaCamera.aspect);
            arenaCamera.orthographicSize = Mathf.Lerp(arenaCamera.orthographicSize,
                Mathf.Max(cameraSize, 8.4f / aspect), 1f - Mathf.Exp(-Time.deltaTime * 5f));
        }

        private void Finished()
        {
            if (State != MeadowEncounterState.Fighting || player.IsDead) return;
            State = MeadowEncounterState.Resolving;
            age = 0f;
            control.SetControlEnabled(false);
            body.linearVelocity = Vector2.zero;
        }

        private void ReleaseCamera()
        {
            if (arenaCamera != null) arenaCamera.orthographicSize = originalSize;
            if (follow != null) follow.enabled = true;
        }

        private void OnDestroy()
        {
            if (boss != null) boss.FightFinished -= Finished;
            ReleaseCamera();
        }
    }
}
