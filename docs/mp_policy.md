# MP Policy

## Status

This document supersedes the previous Battle MVP v0.1 policy that removed MP from the active battle prototype.

MP has been reintroduced as the primary skill resource. The previous cooldown-based skill restriction model is no longer the target design.

Current implementation status:

- `CharacterData.MaxMP` exists and currently defaults to 4.
- `BattleUnit.CurrentMP` exists and is initialized from `CharacterData.MaxMP`.
- `SkillData.MpCost` exists.
- `SkillData.LinkPartnerCharacterId` exists for per-skill specified link partners.
- Dummy skill MP costs are implemented in `DummySkillCatalog`.
- `ItemData.ItemKind` exists for Heal / Pass item behavior.
- Temporary item definitions are implemented in `DummyItemCatalog`.
- `CommandPanelController` obtains temporary items through `DummyItemCatalog.CreateDefaultItems()`.
- `Pass` is implemented as an item with count 99.
- Item buttons are generated and positioned under `itemListPanel` when needed.
- Ally status UI shows MP in the existing status text area.
- Insufficient-MP skills are visually dimmed but remain interactable.
- `BattleUIManager` blocks insufficient-MP skill execution.
- Accepted skill use consumes user MP when the skill action begins.
- Link skills also require the specified partner to have enough MP.
- Accepted Link skill use consumes MP from both the user and the specified partner.
- Link partner resolution excludes the active user from being their own partner.
- Reserve partners can pay MP for Link skills, but only active-grid partners are used for source flash cells.
- `StartNextTurn()` recovers MP +1 for ally front-line and reserve characters.
- Skill CT and LinkCooldown data fields have been removed from the active data model.
- `WAIT:N`, `LINK:N`, and LinkCooldown status display are no longer part of the active UI flow.
- `LinkPartnerPolicy` has been removed after confirming no active code references it.

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
- Pass is an item with count 99.
- Pass costs 0 MP.
- Pass ends the current action.
- Pass does not heal HP.
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
- Active data fields and active UI flow for Skill CT / LinkCooldown have been removed.

## Link skill policy

- Automatic link partner selection is not part of the target design.
- Link skills specify their partner per skill through `SkillData.LinkPartnerCharacterId`.
- Current dummy TwinHit specifies `rogue` as its link partner id.
- TwinHit currently requires user MP 2 + specified partner MP 2.
- If either the user or the specified partner lacks MP, the Link skill is blocked.
- Current implementation consumes the same `MpCost` from both the user and the specified partner.
- A character cannot be their own Link partner.
- A reserve character may be a Link partner and pay MP.
- Reserve Link partners are not shown as source flash cells because they are not placed on the active grid.
- Future Link skill cost values may be split into separate user/partner costs if needed.

## Remaining cleanup

- Move item definitions from `DummyItemCatalog` to ScriptableObject or another asset-backed data source when item count or item behavior increases.
- Implement the broader quest/Wave loop later; the current dummy battle still restarts as a single battle.
