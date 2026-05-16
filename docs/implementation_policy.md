# Implementation Policy

## Purpose

This file records the current workflow policy for this project.

## Chat separation policy

- Planning and design discussion may happen in other chats.
- Final review, implementation guidance, repository-state confirmation, and documentation updates should happen in this implementation chat.
- Chats outside this implementation chat must not write to repository markdown files.
- Markdown updates are allowed in this implementation chat without asking for confirmation each time, when the update is useful for preserving project state.

## Documentation update policy

The assistant may update repository markdown files at appropriate timing without asking the user first.

Appropriate timing includes:

- A gameplay rule is confirmed.
- An implementation is completed and pushed.
- A bug and its fix are confirmed.
- A workflow rule changes.
- A next-chat handoff would otherwise lose important context.

The assistant must report whether the markdown update succeeded or failed.

## Implementation policy

- Prefer small, isolated changes.
- For Unity Scene / Inspector-dependent work, the user performs Unity Play checks.
- For code changes, the assistant provides concrete method-level diffs or replacement blocks.
- Avoid large full-file rewrites of `BattleUIManager.cs` unless there is a strong reason.
- Confirm GitHub `main` after user push when practical.

## Current repository note

Repository:

```text
https://github.com/Kenu4000/game_kari
```

This policy supplements `docs/PROJECT_CONTEXT.md`.
