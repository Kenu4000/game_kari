# MP Policy

## Status

This document supersedes the previous Battle MVP v0.1 policy that removed MP from the active battle prototype.

MP is being reintroduced as the primary skill resource. The previous cooldown-based skill restriction model is no longer the target design.

## Core MP rules

- Ally characters have MP.
- Initial implementation uses max MP 4 for every ally character.
- At quest start, all ally front-line and reserve characters start at MP 4/4.
- MP is preserved across Waves.
- Wave start does not grant additional MP recovery.
- Returning to base restores all allies to MP 4/4.
- Initial implementation does not give MP to enemies.

## Game loop context

Wave is not a skill.

Wave means one battle segment inside the future quest loop:

- Base
- Quest selection
- Conversation event
- Multiple battles / Waves
- Result
- Base

The current dummy battle may continue to behave as a single battle while the broader quest/Wave loop is not implemented.

## Turn recovery

- `StartNextTurn()` is the MP recovery timing.
- At `StartNextTurn()`, all ally front-line and reserve characters recover MP +1.
- MP recovery is clamped at max MP 4.
- This is not per-character command-entry recovery.

## Swap, reserve, Item, and Pass

- Swap does not change MP.
- Returning to reserve does not change MP.
- Reserve allies keep their current MP.
- Reserve allies are included in the `StartNextTurn()` MP +1 recovery.
- Item costs 0 MP.
- Pass costs 0 MP.
- Pass does not grant additional MP recovery.

## Skill costs

Temporary dummy skill costs:

- Slash: MP 0
- Pierce: MP 1
- TwinHit: MP 2
- Focus: MP 0

## MP spending

- Skills have `MpCost`.
- MP is checked before skill execution.
- If current MP is lower than `MpCost`, the skill is blocked.
- If the skill is accepted, MP is consumed when the skill action begins.
- MP is consumed even if the target cell is empty and the skill misses.

## Skill button behavior

- Skills with insufficient MP are visually dimmed.
- Skills with insufficient MP remain interactable so the click still reaches `BattleUIManager`.
- `BattleUIManager` is responsible for blocking insufficient-MP skill execution.
- This follows the previous CT-style defensive blocking pattern rather than disabling the button entirely.

## Cooldown and LinkCooldown policy

- Skill CT is not part of the target design.
- LinkCooldown is not part of the target design.
- `WAIT:N` and `LINK:N` are not target UI states.
- Existing CT / LinkCooldown implementation may be removed or disabled in small steps during the MP migration.

## Link skill policy

- Automatic link partner selection is not part of the target design.
- Link skills should specify their partner per skill.
- Initial implementation may keep TwinHit as a Link skill that consumes only the user's MP 2.
- Future link skill cost policy is user MP 2 + partner MP 2.
- Future link skill availability should check both the user and the specified partner.

## Implementation notes

Recommended migration order:

1. Restore MP-related data fields: `CharacterData.MaxMP`, `BattleUnit.CurrentMP`, and `SkillData.MpCost`.
2. Assign dummy skill MP costs in `DummySkillCatalog`.
3. Show ally MP in status UI.
4. Dim insufficient-MP skill buttons while keeping them interactable.
5. Block insufficient-MP skills in `BattleUIManager`.
6. Consume MP on accepted skill use.
7. Recover ally front-line and reserve MP at `StartNextTurn()`.
8. Disable or remove CT and LinkCooldown behavior in small steps.
9. Replace automatic link partner selection with per-skill specified partner logic.
