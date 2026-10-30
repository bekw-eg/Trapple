using System.Collections.Generic;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.DeceptiveForest
{
    public enum ForestEncounterState { Route, Intro, BossFight, Resolving, Complete, Failed }

    public sealed class ForestGuardianEncounter : MonoBehaviour, IWorldVictoryPresentation
    {
        [SerializeField] private PlayerDeath player;
        [SerializeField] private ForestGuardianController guardian;
        [SerializeField] private StickInventory inventory;
        [SerializeField] private WorldProgressController progress;
        [SerializeField] private BossArenaCameraController arenaCamera;
        [SerializeField] private GameObject entranceBarrier;
        [SerializeField] private Vector2 arenaBounds;
        [SerializeField] private Vector3 checkpointPosition;
        [SerializeField, Min(1)] private float survivalTime = 60;
        [SerializeField, Range(4, 6)] private float introDuration = 5;
        [SerializeField, Min(.5f)] private float resolutionDuration = 2.6f;
        [SerializeField] private KeyCode skipKey = KeyCode.Return;
        [SerializeField] private AudioSource atmosphere;
        private static string checkpointScene;
        private static int checkpointSticks;
        private static readonly HashSet<string> collected = new HashSet<string>();
        private float stateAge;
        private PlayerController controller;
        private Rigidbody2D body;
        private bool reported;
        public ForestEncounterState State { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool Retreated { get; private set; }
        public ForestGuardianController Guardian => guardian;
        public StickInventory Inventory => inventory;
        public WorldProgressController Progress => progress;
        public float IntroProgress => Mathf.Clamp01(stateAge / introDuration);
        public static bool HasCheckpoint => !string.IsNullOrEmpty(checkpointScene);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            ClearCheckpoint();
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        { if (mode == LoadSceneMode.Single && scene.path != checkpointScene) ClearCheckpoint(); }
        public static void ClearCheckpoint() { checkpointScene = null; checkpointSticks = 0; collected.Clear(); }
        private void Start()
        {
            controller = player.GetComponent<PlayerController>(); body = player.GetComponent<Rigidbody2D>();
            TimeRemaining = survivalTime;
            guardian.Initialize(player, arenaBounds, 0);
            guardian.Resolved += Resolved;
            if (arenaCamera != null) guardian.Impact += arenaCamera.Shake;
            entranceBarrier.SetActive(false);
            WorldCompletion.EnsureProgression();
            if (checkpointScene == gameObject.scene.path)
            {
                inventory.Restore(checkpointSticks);
                foreach (var pickup in FindObjectsByType<StickPickup>(FindObjectsInactive.Include))
                    if (collected.Contains(pickup.PickupId)) pickup.MarkCollected();
                body.position = checkpointPosition; body.linearVelocity = Vector2.zero;
                player.transform.position = checkpointPosition;
                Physics2D.SyncTransforms();
            }
        }
        public void CaptureCheckpoint()
        {
            if (State != ForestEncounterState.Route || player == null || player.IsDead || checkpointScene == gameObject.scene.path) return;
            checkpointScene = gameObject.scene.path; checkpointSticks = inventory.Count; collected.Clear();
            foreach (var pickup in FindObjectsByType<StickPickup>(FindObjectsInactive.Include))
                if (pickup.IsCollected) collected.Add(pickup.PickupId);
        }
        public void EnterArena()
        {
            if (State != ForestEncounterState.Route || !CanTick || controller == null) return;
            CaptureCheckpoint(); progress.ReachArena();
            State = ForestEncounterState.Intro; stateAge = 0;
            controller.SetControlEnabled(false); body.linearVelocity = Vector2.zero;
            entranceBarrier.SetActive(true);
            guardian.BeginIntro();
            arenaCamera.Focus(new Vector3(guardian.transform.position.x - 2.5f, 3.2f, -10), 5.4f);
            arenaCamera.Shake(.1f);
            if (atmosphere != null) atmosphere.Play();
        }
        public void SkipIntro()
        {
            if (State != ForestEncounterState.Intro || !CanTick) return;
            BeginFight();
        }
        private bool CanTick => player != null && !player.IsDead && Time.timeScale > 0 &&
            GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
        private void Update()
        {
            if (player == null || guardian == null) return;
            // Death wins on the same frame as clock expiry.
            if (player.IsDead && State != ForestEncounterState.Complete && State != ForestEncounterState.Failed)
            {
                State = ForestEncounterState.Failed; guardian.CancelFight();
                player.GetComponent<StickThrowController>()?.ClearProjectiles(); arenaCamera.Release();
                if (atmosphere != null) atmosphere.Stop();
                return;
            }
            if (!CanTick) return;
            stateAge += Time.deltaTime;
            if (State == ForestEncounterState.Intro)
            {
                controller.SetControlEnabled(false);
                if (Input.GetKeyDown(skipKey) || stateAge >= introDuration) BeginFight();
            }
            else if (State == ForestEncounterState.BossFight)
            {
                TimeRemaining = Mathf.Max(0, TimeRemaining - Time.deltaTime);
                if (TimeRemaining <= 0 && !player.IsDead) guardian.Resolve(true);
            }
            else if (State == ForestEncounterState.Resolving && stateAge >= resolutionDuration)
            {
                State = ForestEncounterState.Complete;
                if (!reported) { reported = true; WorldCompletion.Record("forest", gameObject.scene.path); }
                ClearCheckpoint(); GameManager.Instance.CompleteLevel();
            }
        }
        private void BeginFight()
        {
            State = ForestEncounterState.BossFight; stateAge = 0; TimeRemaining = survivalTime;
            float halfWidth = (arenaBounds.y - arenaBounds.x) * .5f + 1;
            float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9;
            arenaCamera.Focus(new Vector3((arenaBounds.x + arenaBounds.y) * .5f, 3.2f, -10), Mathf.Max(5.4f, halfWidth / aspect));
            controller.SetControlEnabled(true); guardian.BeginFight();
        }
        private void Resolved(bool retreat)
        {
            if (State != ForestEncounterState.BossFight || player.IsDead) return;
            Retreated = retreat; State = ForestEncounterState.Resolving; stateAge = 0;
            controller.SetControlEnabled(false); body.linearVelocity = Vector2.zero;
            player.GetComponent<StickThrowController>()?.ClearProjectiles();
            if (atmosphere != null) atmosphere.Stop();
            arenaCamera.Release();
        }
        public void Continue()
        {
            var flow = HubSceneFlowController.Instance;
            if (flow != null)
                foreach (var world in flow.Configuration.worlds)
                    if (world.requiredCompletedWorldId == "forest" && flow.CanLaunch(world, 0) && flow.TryLaunch(world, 0)) return;
            GoToHub();
        }
        public void GoToHub() { ClearCheckpoint(); GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath); }
        private void OnDestroy()
        {
            if (guardian != null) { guardian.Resolved -= Resolved; if (arenaCamera != null) guardian.Impact -= arenaCamera.Shake; }
            if (arenaCamera != null) arenaCamera.Release();
        }
    }
}
