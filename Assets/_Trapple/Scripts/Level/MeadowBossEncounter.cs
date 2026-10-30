using Trapple.Bosses;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    public enum MeadowEncounterState { Route, Intro, Fighting, Resolving, Complete, Failed }

    /// <summary>First-world survival trial, using the forest's animated and telegraphed combat.</summary>
    public sealed class MeadowBossEncounter : MonoBehaviour, IWorldVictoryPresentation
    {
        [SerializeField] private TutorialGoal goal;
        [SerializeField] private PlayerDeath player;
        [SerializeField] private ForestGuardianController boss;
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private TutorialCamera follow;
        [SerializeField] private GameObject entranceBarrier;
        [SerializeField] private Vector2 arenaBounds;
        [SerializeField] private Vector3 checkpointPosition;
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField, Min(1f)] private float cameraSize = 6f;
        [SerializeField, Range(3f, 6f)] private float introDuration = 4.5f;
        [SerializeField, Min(5f)] private float survivalDuration = 45f;
        [SerializeField, Min(.1f)] private float resolutionDuration = 2.7f;
        [SerializeField] private AudioSource atmosphere;
        private static string checkpointScene;
        private PlayerController control;
        private Rigidbody2D body;
        private float age, originalSize, shake, shakePhase;
        private Vector3 cameraVelocity;
        private bool reported;
        public MeadowEncounterState State { get; private set; }
        public ForestGuardianController Boss => boss;
        public float TimeRemaining { get; private set; }
        public float Duration => survivalDuration;
        public float Progress => Mathf.Clamp01(1 - TimeRemaining / survivalDuration);
        public Vector3 CheckpointPosition => checkpointPosition;
        public bool IsNearArena => player != null && player.transform.position.x >= checkpointPosition.x - 8;
        public static bool HasCheckpoint => !string.IsNullOrEmpty(checkpointScene);
        public bool CanTick => player != null && !player.IsDead && Time.timeScale > 0 &&
            GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            ClearCheckpoint(); SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        { if (mode == LoadSceneMode.Single && scene.path != checkpointScene) ClearCheckpoint(); }
        public static void ClearCheckpoint() => checkpointScene = null;

        private void Start()
        {
            control = player.GetComponent<PlayerController>(); body = player.GetComponent<Rigidbody2D>();
            originalSize = arenaCamera.orthographicSize;
            TimeRemaining = survivalDuration;
            boss.Initialize(player, arenaBounds, 0);
            boss.Resolved += Finished; boss.Impact += Shake;
            entranceBarrier.SetActive(false);
            WorldCompletion.EnsureProgression();
            if (checkpointScene == gameObject.scene.path)
            {
                player.transform.position = checkpointPosition; body.position = checkpointPosition;
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            }
        }
        public void CaptureCheckpoint()
        {
            if (State == MeadowEncounterState.Route && CanTick) checkpointScene = gameObject.scene.path;
        }
        public void EnterArena()
        {
            if (State != MeadowEncounterState.Route || !CanTick || control == null) return;
            CaptureCheckpoint(); State = MeadowEncounterState.Intro; age = 0;
            control.SetControlEnabled(false); body.linearVelocity = Vector2.zero;
            entranceBarrier.SetActive(true); follow.enabled = false;
            boss.BeginIntro(); Shake(.08f);
            if (atmosphere != null) atmosphere.Play();
        }
        public void SkipIntro()
        {
            if (State != MeadowEncounterState.Intro || !CanTick) return;
            State = MeadowEncounterState.Fighting; age = 0; TimeRemaining = survivalDuration;
            control.SetControlEnabled(true); boss.BeginFight();
        }
        private void Update()
        {
            if (player == null || boss == null) return;
            if (player.IsDead && State != MeadowEncounterState.Complete && State != MeadowEncounterState.Failed)
            {
                State = MeadowEncounterState.Failed; boss.CancelFight(); ReleaseCamera();
                if (atmosphere != null) atmosphere.Stop(); return;
            }
            if (!CanTick) return;
            age += Time.deltaTime;
            if (State == MeadowEncounterState.Intro)
            {
                control.SetControlEnabled(false);
                if (age >= introDuration || Input.GetKeyDown(KeyCode.Return)) SkipIntro();
            }
            else if (State == MeadowEncounterState.Fighting)
            {
                TimeRemaining = Mathf.Max(0, TimeRemaining - Time.deltaTime);
                if (TimeRemaining <= 0 && !player.IsDead) boss.Resolve(true);
            }
            else if (State == MeadowEncounterState.Resolving && age >= resolutionDuration)
            {
                State = MeadowEncounterState.Complete;
                if (!reported) { reported = true; WorldCompletion.Record("meadow", gameObject.scene.path); }
                ClearCheckpoint(); goal.CompleteAfterBoss(player); GameManager.Instance.CompleteLevel();
            }
        }
        private void LateUpdate()
        {
            if (arenaCamera == null || Time.deltaTime <= 0 ||
                (State != MeadowEncounterState.Intro && State != MeadowEncounterState.Fighting && State != MeadowEncounterState.Resolving)) return;
            Vector3 target = cameraPosition;
            float size = Mathf.Max(cameraSize, ((arenaBounds.y - arenaBounds.x) * .5f + 1.5f) / Mathf.Max(.5f, arenaCamera.aspect));
            if (State == MeadowEncounterState.Intro && age < 2.4f)
            { target.x = boss.transform.position.x - 2; size = 5.5f; }
            arenaCamera.transform.position = Vector3.SmoothDamp(arenaCamera.transform.position,
                target, ref cameraVelocity, .4f, Mathf.Infinity, Time.deltaTime);
            arenaCamera.orthographicSize = Mathf.Lerp(arenaCamera.orthographicSize, size, 1 - Mathf.Exp(-Time.deltaTime * 5));
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * .45f); shakePhase += Time.deltaTime * 43;
            arenaCamera.transform.position += new Vector3(Mathf.Sin(shakePhase), Mathf.Cos(shakePhase * 1.3f), 0) * shake;
        }
        private void Shake(float strength) => shake = Mathf.Max(shake, Mathf.Min(.12f, strength));
        private void Finished(bool retreat)
        {
            if (!retreat || State != MeadowEncounterState.Fighting || player.IsDead) return;
            State = MeadowEncounterState.Resolving; age = 0;
            control.SetControlEnabled(false); body.linearVelocity = Vector2.zero;
            if (atmosphere != null) atmosphere.Stop();
        }
        public void Continue() { ClearCheckpoint(); WorldCompletion.Continue(); }
        public void GoToHub() { ClearCheckpoint(); GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath); }
        private void ReleaseCamera()
        {
            shake = 0;
            if (arenaCamera != null) arenaCamera.orthographicSize = originalSize;
            if (follow != null) follow.enabled = true;
        }
        private void OnDestroy()
        {
            if (boss != null) { boss.Resolved -= Finished; boss.Impact -= Shake; }
            ReleaseCamera();
        }
    }
}
