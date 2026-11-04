using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class FactoryCheckpoint : MonoBehaviour
    {
        private static string checkpointScene;
        private static Vector2 checkpointPosition;
        private static SpriteRenderer activeBeacon;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SpriteRenderer beacon;
        [SerializeField] private Color activeColor = new Color(1f, .7f, .18f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { checkpointScene = null; checkpointPosition = default; activeBeacon = null; }

        public static void Clear() { checkpointScene = null; checkpointPosition = default; activeBeacon = null; }

        public void Configure(Transform point, SpriteRenderer marker)
        { spawnPoint = point; beacon = marker; }

        private void Awake()
        {
            if (spawnPoint == null) spawnPoint = transform;
            SceneManager.sceneLoaded += SceneLoaded;
            if (beacon != null && checkpointScene == gameObject.scene.path &&
                Vector2.Distance(checkpointPosition, spawnPoint.position) < .1f)
            { beacon.color = activeColor; activeBeacon = beacon; }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player == null || player.IsDead) return;
            checkpointScene = gameObject.scene.path;
            checkpointPosition = spawnPoint.position;
            if (activeBeacon != null) activeBeacon.color = Color.white;
            activeBeacon = beacon;
            if (beacon != null) beacon.color = activeColor;
        }

        public static void RestoreCurrentScene()
        {
            if (string.IsNullOrEmpty(checkpointScene) || SceneManager.GetActiveScene().path != checkpointScene) return;
            var player = FindAnyObjectByType<PlayerDeath>();
            if (player == null) return;
            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = checkpointPosition;
            body.position = checkpointPosition;
            body.linearVelocity = Vector2.zero;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && scene.path != checkpointScene) Clear();
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= SceneLoaded;
    }

}
