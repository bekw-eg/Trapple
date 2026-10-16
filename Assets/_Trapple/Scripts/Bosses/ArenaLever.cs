using Trapple.Player;
using UnityEngine;

namespace Trapple.Bosses
{
    public sealed class ArenaLever : MonoBehaviour
    {
        [SerializeField] private BossHealth boss;
        [SerializeField] private PlayerDeath player;
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.1f)] private float activationDistance = 1.6f;
        [SerializeField] private Transform fallingStone;
        [SerializeField] private Vector3 stoneStart = new Vector3(6.2f, 7f, 0f);
        [SerializeField] private Vector3 stoneEnd = new Vector3(6.2f, 3f, 0f);
        private int lastUsedOpening = -1;
        private float stoneAnimation = -1f;

        public bool PlayerInRange => player != null && !player.IsDead &&
            Vector2.Distance(player.transform.position, transform.position) <= activationDistance;
        public bool CanActivate => boss != null && !boss.IsDead && boss.IsVulnerable &&
            lastUsedOpening != boss.VulnerabilityVersion && PlayerInRange;

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (Input.GetKeyDown(KeyCode.E)) TryActivate();
            if (stoneAnimation < 0f || fallingStone == null) return;
            stoneAnimation += Time.deltaTime;
            fallingStone.position = Vector3.Lerp(stoneStart, stoneEnd, stoneAnimation / 0.3f);
            if (stoneAnimation >= 0.6f)
            {
                fallingStone.gameObject.SetActive(false);
                stoneAnimation = -1f;
            }
        }

        public bool TryActivate()
        {
            if (Time.timeScale <= 0f || !CanActivate) return false;
            // One hit per opening prevents repeated E presses from skipping the encounter.
            lastUsedOpening = boss.VulnerabilityVersion;
            if (!boss.TryTakeDamage(damage)) return false;
            if (fallingStone != null)
            {
                fallingStone.position = stoneStart;
                fallingStone.gameObject.SetActive(true);
                stoneAnimation = 0f;
            }
            return true;
        }
    }
}
