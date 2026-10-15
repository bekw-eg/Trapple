# Trapple - AI Agent Instructions

These permanent instructions apply to all AI agents, including Codex, working in
this repository.

## 1. Project Context

- **Project:** Trapple
- **Engine:** Unity 2D
- **Language:** C#
- **Team:** Two developers
- **Repository:** [bekw-eg/Trapple](https://github.com/bekw-eg/Trapple/)

Trapple is a 2D trap platformer inspired by Level Devil and Red Ball.

The player controls a character, avoids unexpected traps, and attempts to reach
an apple at the end of each level.

The game includes deceptive platforms, hidden traps, unexpected environmental
changes, and boss encounters.

Planned locations:

- **Apple Meadow:** Tutorial.
- **Deceptive Forest:** Forest traps and Ancient Tree boss.
- **Abandoned Factory:** Mechanical traps and Robot boss.
- **Lava Fortress:** Fire hazards and final boss.

These locations are planned features, not necessarily implemented.

## 2. Mandatory Git Workflow

All AI agents MUST follow these rules:

- NEVER commit directly to `main`.
- NEVER push directly to `main`.
- NEVER force-push to `main`.
- NEVER merge Pull Requests without explicit authorization.
- Before starting a task, inspect the repository status and existing branches.
- Start from the latest appropriate base branch.
- Create a new feature branch for every independent task.
- Never discard existing uncommitted changes without permission.

Use the following branch naming conventions:

```text
feature/<feature-name>
fix/<bug-name>
refactor/<component-name>
chore/<task-name>
```

Examples:

```text
feature/player-controller
feature/boss-system
fix/player-jump
```

## 3. Git Identity - IMPORTANT

The GitHub repository owner is `bekw-eg`.

When working on behalf of the repository owner, use the owner's authorized Git
identity for commits.

- **Required Git author name:** `bekw-eg`.
- **Required Git author email:** An email verified for the `bekw-eg` GitHub
  account, including its GitHub-provided noreply email if configured.

Before committing:

- Check `git config user.name`.
- Check `git config user.email`.
- Check the authenticated GitHub account.
- Ensure the author name is `bekw-eg` when acting on behalf of the owner.
- Use only an email confirmed to belong to the owner.
- Prefer repository-local Git configuration.
- Never invent an email address.
- Never use an AI agent's name as the commit author when performing changes on
  behalf of `bekw-eg`.

Create Pull Requests using the authenticated GitHub account `bekw-eg` when
explicitly acting on the owner's behalf.

**Git commit identity and GitHub authentication are different.** Setting
`git config user.name` does not change the GitHub account that creates a Pull
Request.

If authentication belongs to another account, do not impersonate `bekw-eg`.
Request proper authentication or provide the owner with the steps needed to
create the Pull Request.

When the second human developer works using their own account, preserve their
real authorship. Never falsely attribute their changes to `bekw-eg`.

## 4. Mandatory Commits and Pull Requests

Every completed development task MUST follow this workflow:

1. Inspect repository status.
2. Create or switch to the appropriate feature branch.
3. Implement the requested changes.
4. Run available checks and tests.
5. Review modified files.
6. Create descriptive Git commits.
7. Push the feature branch to GitHub.
8. Create a Pull Request targeting `main`.
9. Provide the Pull Request URL and a summary of changes.

Use Conventional Commits, for example:

```text
feat: implement player movement
feat: add forest boss
fix: resolve jumping issue
refactor: simplify trap controller
chore: configure Unity project
```

Do not create empty or misleading commits.

Do not claim that a commit, push, test, or Pull Request succeeded unless the
operation actually completed.

If GitHub authentication or permissions are unavailable, report the blocker
instead of claiming success.

## 5. Unity Development Rules

- Use C# and Unity 2D.
- Maintain clean, modular architecture.
- Follow existing project conventions.
- Prefer reusable components.
- Use `[SerializeField]` for configurable Inspector values.
- Avoid unnecessary dependencies.
- Keep player, trap, boss, and UI logic separate.
- Do not rewrite unrelated systems.
- Do not introduce breaking changes without justification.
- Use prefabs for reusable game objects.
- Preserve Unity `.meta` files.
- Use Visible Meta Files and Force Text serialization.
- Never commit Unity-generated temporary directories such as `Library`, `Temp`,
  and `Logs`.
- Do not edit Unity scenes or prefabs by generating unverified YAML.
- If Unity Editor is unavailable, clearly report what could not be tested.

## 6. Team Collaboration

Two developers work on this repository.

### Developer 1

- Player controller.
- Movement and jumping.
- Traps.
- Platforming levels.
- Player death and respawn.

### Developer 2

- Bosses.
- Boss attacks and phases.
- Main menu.
- GameManager.
- Game UI.

These responsibilities describe the initial plan and may change.

Collaboration rules:

- Avoid modifying another developer's files without a clear reason.
- Do not modify the same Unity scene concurrently.
- Develop bosses in separate test scenes.
- Build reusable prefabs for integration.
- Communicate required interface changes through Pull Request descriptions.
- Preserve compatibility with existing gameplay systems.

## 7. Scope Control

Implement only the task requested.

Do not automatically implement the entire game. Do not create additional levels,
bosses, or mechanics unless requested.

Prioritize:

1. Correct functionality.
2. Unity project compatibility.
3. Maintainable code.
4. Simple architecture.
5. Performance.
6. Visual polish.

## 8. Required Final Report

After each task, provide:

- Feature implemented.
- Git branch name.
- List of changed files.
- Tests performed.
- Tests that could not be performed.
- Commit hash.
- Pull Request URL.
- Known issues or limitations.

If a Pull Request could not be created, state exactly why.
