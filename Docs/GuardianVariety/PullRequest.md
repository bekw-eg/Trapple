Both tree bosses previously repeated the same three stationary/linear attacks. Their patterns now include a shared 50-branch rain barrage plus distinct attacks: Orchard Warden drops six bouncing/rolling apples, while Forest Guardian sends an advancing root wave and three diagonal fans of golden leaves.

Branch rain lifts the boss's arms with a newly generated giant branch sprite, then releases ten five-branch volleys. Each volley samples Appi's location before marking its landing lanes; targets remain fixed after reservation, warnings persist until impact and follow platform height. Gravity, bouncing and rolling live on the pooled hazard physics root; artwork animates independently. Both arenas show large attack-specific advice. The first boss remains survival-only; damaging the second boss interrupts its barrage.

Special attacks prewarm 32 hazard slots and 20 reservation markers. Pause freezes their clocks and motion, while death, cancellation, stun and victory clear pending and active hazards. Boss prefabs are upgraded through Unity Editor APIs without rebuilding scenes or routes; existing rebuild tools remain compatible with shoulder joints and special components.

This continues unmerged #34; the incremental changes are after `946ec13`. The PR targets `main` per repository instructions and includes the earlier integration ancestry. No PRs were merged.

Validation in Unity 6000.6.0f1:

- Latest unique PlayMode results across three runs: 63 passed, 0 failed, 2 old opt-in captures skipped. Final follow-up runs: 25 passed/1 skipped, then 2/2 passed for the golden fan and GPU capture.
- Exactly 50 branches for each boss; committed targets, pause/death cleanup, damage interruption, apple bounce/roll and real player death, advancing roots and three fan trajectories.
- Real PlayerController/Physics2D evasion of opening rain volleys, a root wave and an isolated rolling apple; existing three attacks, survival/retry/completion and forest/stick/death-screen regressions.
- 30/30 EditMode passed; final Windows x64 Development build succeeded; git diff --check passed.
- Reviewed 420 actual GPU frames, with five GIF previews and full-resolution stills in Docs/GuardianVariety. The branch art source and final generation prompt are documented there.

The full barrage lifecycle/count tests and demo capture disable player physics. Full keyboard boss runs, new-player balance assessment, long-route playthroughs and launching the exported executable remain manual checks. See Docs/GuardianVariety/README.md and TestResults.json for exact scope and results.
