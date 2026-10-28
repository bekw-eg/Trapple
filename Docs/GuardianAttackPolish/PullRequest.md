The first-world Orchard Warden previously switched from a small sprite pose directly into a live hazard. Its attacks now have shoulder-driven anticipation, a fast contact swing, body recoil, crown follow-through and a recovery pose. Roots emerge from the ground, leaves flutter, and pooled dust/debris/leaf bursts accompany contact.

The Orchard prefab adds a 0.2-second contact lead-in. Ground warnings and HUD advice persist until contact; damage and camera impact occur at contact. Cancellation clears pending strikes and particles, and pause freezes motion immediately. The shared hazard artwork animates independently of its physics root, with root damage growing alongside the visible root. The second-world guardian keeps its existing zero-delay timing.

This continues unmerged #33 and its earlier integration ancestry; the incremental attack-polish changes are after `198da78`. Target is `main` per repository workflow. Existing scene files and routes are unchanged in this increment; the updated shared Orchard prefab supplies the rig to Tutorial and MeadowBossTest. No merges were performed.

Validation in Unity 6000.6.0f1:

- Latest unique PlayMode results across two runs: 54 passed, 0 failed, 1 old opt-in capture skipped; final first-boss/animation run: 16/16 passed.
- Real PlayerController/Physics2D evasion of all three attacks, timed survival/retry/completion, old boss/forest/stick/leaf/death-screen regressions.
- 30/30 EditMode passed; Windows x64 Development build succeeded; git diff --check passed.
- 126 actual GPU frames reviewed and exported as three animated previews under Docs/GuardianAttackPolish.

The pause test initially caught one final LateUpdate pose step after the pause request; this was fixed and the rerun passed. Full long-route playthroughs, physical-keyboard difficulty assessment and launching the exported executable remain manual checks. Documentation and the exact per-test results are in Docs/GuardianAttackPolish/README.md and TestResults.json.
