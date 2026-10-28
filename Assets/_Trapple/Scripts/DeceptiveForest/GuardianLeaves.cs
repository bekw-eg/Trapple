using UnityEngine;
using Trapple.Level;

namespace Trapple.DeceptiveForest
{
    /// <summary>A small fixed set of ambient leaves; no per-frame object allocations.</summary>
    public sealed class GuardianLeaves : MonoBehaviour
    {
        [SerializeField] private ForestGuardianEncounter encounter;
        [SerializeField] private MeadowBossEncounter meadow;
        [SerializeField] private Transform[] leaves;
        [SerializeField] private Vector2 bounds;
        private bool victoryBurst;
        private void Update()
        {
            if (leaves == null || Time.deltaTime <= 0) return;
            bool resolving = encounter != null && encounter.State == ForestEncounterState.Resolving || meadow != null && meadow.State == MeadowEncounterState.Resolving;
            if (resolving && !victoryBurst)
            {
                victoryBurst = true;
                Vector3 origin = (encounter != null ? encounter.Guardian.transform.position : meadow.Boss.transform.position) + Vector3.up * 4;
                for (int i = 0; i < Mathf.Min(10, leaves.Length); i++)
                    if (leaves[i] != null) leaves[i].position = origin + new Vector3(Mathf.Sin(i * 2.4f) * 2.2f, Mathf.Cos(i * 1.7f) * 1.1f, 0);
            }
            bool intro = encounter != null && encounter.State == ForestEncounterState.Intro || meadow != null && meadow.State == MeadowEncounterState.Intro;
            float wind = intro ? 3.3f : resolving ? 2.2f : 1;
            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = leaves[i]; if (leaf == null) continue;
                var p = leaf.position;
                p.x -= Time.deltaTime * wind * (.45f + (i % 3) * .14f);
                p.y -= Time.deltaTime * (.5f + (i % 4) * .12f);
                p.x += Mathf.Sin(Time.time * 1.3f + i) * Time.deltaTime * .25f;
                if (p.y < .1f || p.x < bounds.x) { p.x = Mathf.Lerp(bounds.x, bounds.y, (i * .618f + Time.time * .021f) % 1); p.y = 8; }
                leaf.position = p;
                leaf.Rotate(0, 0, Time.deltaTime * (25 + i));
            }
        }
    }
}
