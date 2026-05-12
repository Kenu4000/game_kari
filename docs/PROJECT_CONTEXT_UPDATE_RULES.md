# PROJECT_CONTEXT.md Update Rules

## Purpose

`docs/PROJECT_CONTEXT.md` is the main handoff file for continuing the project across ChatGPT and Codex sessions.

This file should contain the current project assumptions, finalized specifications, workflow rules, and a compact work log. It is not a scratchpad for every conversation detail.

## Responsibility split

### ChatGPT

ChatGPT is responsible for maintaining `docs/PROJECT_CONTEXT.md`.

ChatGPT should update the file when a decision becomes stable enough that a future chat or Codex task would need it.

ChatGPT should also update the `Work log` section when the change matters as project history.

### Codex

Codex should read `docs/PROJECT_CONTEXT.md` as reference material before implementation tasks.

Codex should not edit `docs/*` unless the user explicitly asks it to do documentation work.

For normal implementation tasks, Codex should treat `docs/*` as read-only.

## When to update PROJECT_CONTEXT.md

Update `docs/PROJECT_CONTEXT.md` when any of the following are finalized:

- battle system specification changes
- UI layout or interaction specification changes
- Unity workflow decisions
- GitHub workflow decisions
- Codex usage rules
- important troubleshooting results
- PR or merge results that affect the next work step
- decisions needed for new-chat handoff

Do not update it for minor brainstorming that has not been adopted.

## Work log policy

When a change is important as project history, update the `Work log` section inside `docs/PROJECT_CONTEXT.md`.

Work log entries should be:

- grouped by date
- short and factual
- written as bullet points
- focused on decisions, completed work, important failures, and follow-up actions

A good Work log entry records what changed and why it matters for the next session.

## Meeting notes policy

Detailed notes may be written under `docs/meeting_notes/*`.

However, important finalized decisions from meeting notes should eventually be reflected in `docs/PROJECT_CONTEXT.md` so that future chats do not need to read every separate note file.

## Reporting rule

After attempting to update `docs/PROJECT_CONTEXT.md`, ChatGPT must report in the next response:

- whether the file was updated
- whether the update succeeded or failed
- if it failed, what text should be copied manually

## Codex prompt rule

Codex prompts may reference these files:

```text
docs/PROJECT_CONTEXT.md
docs/PROJECT_CONTEXT_UPDATE_RULES.md
```

But normal Codex implementation prompts should also include:

```text
docs/* is reference-only. Do not modify docs/*.
```
