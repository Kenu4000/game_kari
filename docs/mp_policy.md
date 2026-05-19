# MP Policy

## Status

This document supersedes the previous Battle MVP v0.1 policy that removed MP from the active battle prototype.

MP has been reintroduced as the primary skill resource. The previous cooldown-based skill restriction model is no longer the target design.

Current implementation status:

- `CharacterData.MaxMP` exists and currently defaults to 4.
- `CharacterData.DefaultSkills` exists and stores default skill ownership for a character.
- Default ally and enemy `CharacterData` assets exist under `Assets/Resources/Battle/Characters`.
- `CharacterAssetProvider.CreateCharacterDataById(...)` loads character assets through `Resources.Load<CharacterData>(...)` and throws if the requested asset is missing.
- `CharacterAssetProvider` does not have a runtime character fallback path; battle participants are expected to have `CharacterData` assets.
- `DefaultBattleUnitFactory.CreateAllyUnitById(...)` creates ally units by character id and assigns player skills from `CharacterData.DefaultSkills`.
- `DefaultBattleUnitFactory.CreateEnemyUnitById(...)` creates enemy units by character id and assigns enemy skills from `CharacterData.DefaultSkills`.
- `DefaultBattleSetupFactory` now creates default ally and enemy units by character id instead of duplicating HP/Speed values in setup code.
- `DefaultBattleSetupFactory` now creates runtime inventory items from `DefaultInventoryProvider` and stores them on `BattleSetupData.InventoryItems`.
- `BattleSetupData` stores default battle unit placements, reserves, fallback active unit, enemy references, and runtime inventory.
- Enemy `CharacterData` assets now have one default enemy skill each.
- Legacy dummy battle factory/setup/helper names have been removed from the battle scripts.
- `BattleUnit.CurrentMP` exists and is initialized from `CharacterData.MaxMP`.
- `SkillData` has been converted to `ScriptableObject` and can be created from `Create > GameKari > Battle > Skill Data`.
- Default player `SkillData` assets and default enemy `SkillData` assets exist under `Assets/Resources/Battle/Skills`.
- `DefaultSkillAssetProvider` loads required default player/enemy skill assets through `Resources.Load<SkillData>(...)` and throws if a required asset is missing.
- `DefaultSkillAssetProvider` does not create runtime fallback `SkillData` instances.
- `SkillData.MpCost` exists.
- `SkillData.LinkPartnerCharacterId` exists for per-skill specified link partners.
- `SkillData.TargetPattern` uses opponent-relative names such as `FrontTopOpponent`, `BothFrontOpponents`, and `SameGridPosOpponent` so player and enemy actions can share the same targeting model.
- `SkillData` currently represents skill definition data only and does not store runtime cooldown/state.
- Runtime unit state is stored on `BattleUnit` through HP, MP, KO state, grid position, and buffs.
- Default player skill MP costs and enemy skill target patterns are implemented in `SkillData` assets and accessed through `DefaultSkillAssetProvider`.
- Skills are assigned per character through `CharacterData.DefaultSkills` for both allies and enemies.
- `UnitSkillInitializer` copies skills from `unit.Data.DefaultSkills` to `unit.Skills`.
- Knight currently owns the Link skill `TwinHit`.
- Rogue is the specified partner for `TwinHit` and does not currently own `TwinHit`.
- Enemy actions use `EnemyActionData` as a runtime wrapper around `SkillData`.
- `EnemyActionSelector` reads the enemy unit's primary skill from `BattleUnit.Skills[0]` and uses `enemy_strike` only as fallback.
- Enemy-specific `EnemyTargetPattern` has been removed; enemy target preview and enemy damage resolution read `action.Skill.TargetPattern`.
- Current default enemy skills are `enemy_claw`, `enemy_arrow`, `enemy_bite`, `enemy_hex`, and `enemy_strike`.
- `ItemData.ItemKind` exists for Heal / Pass item behavior.
- `ItemData` has been converted to `ScriptableObject` and can be created from `Create > GameKari > Battle > Item Data`.
- Default `ItemData` assets exist under `Assets/Resources/Battle/Items`.
- `DefaultInventoryProvider` loads default inventory ownership/count from `InventoryLoadoutData` under `Assets/Resources/Battle/Inventory`.
- `InventoryLoadoutData` exists as a `ScriptableObject` and stores initial item ownership/count entries.
- `default_inventory.asset` currently contains Potion x3 and Pass x99.
- `ItemData` represents item definition only and does not store runtime count.
- `InventoryItem` stores an `ItemData` reference and runtime item count.
- `BattleUIManager` stores runtime inventory items and passes them to `CommandPanelController.Setup(...)`.
- `CommandPanelController` no longer creates inventory items directly; it only displays and updates the inventory list it receives.
- `Pass` is implemented as an item with count 99 through the default inventory loadout.
- Item buttons are generated and positioned under `itemListPanel` when needed.
- Item button generation now re-parents existing item buttons to `itemListPanel`, creates missing buttons, and reapplies fixed size/position.
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

The current battle may continue to behave as a single battle while the broader quest/Wave loop is not implemented.

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

Temporary default player skill costs:

- Slash: MP 0
- Pierce: MP 1
- TwinHit: MP 2
- Focus: MP 0

Temporary default enemy skills:

- Goblin A: Claw, Damage 60, same-grid opponent target
- Archer: Arrow, Damage 45, front-top opponent target
- Goblin B: Bite, Damage 60, front-bottom opponent target
- Shaman: Hex, Damage 25, all opponents target
- Enemy Reserve: Strike, Damage 60, same-grid opponent target

## Temporary default skill ownership

- Knight: Slash, Pierce, TwinHit, Focus
- Mage: Slash, Pierce, Focus
- Cleric: Slash, Focus
- Rogue: Slash, Pierce, Focus
- Reserve: Slash
- Goblin A: Claw
- Archer: Arrow
- Goblin B: Bite
- Shaman: Hex
- Enemy Reserve: Strike

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

## Character model

- `CharacterData` is character definition data.
- `CharacterData` is a `ScriptableObject` type.
- Default ally and enemy characters currently exist as assets in `Assets/Resources/Battle/Characters`.
- `CharacterAssetProvider.CreateCharacterDataById(...)` first tries to load character assets by character id and fails loudly when missing.
- Default battle setup now uses character ids for ally/enemy creation, so HP/MP/Speed/DefaultSkills are sourced from `CharacterData` assets.
- Enemies now store their default enemy skill in `CharacterData.DefaultSkills`.

## Skill model

- `SkillData` is skill definition data.
- `SkillData` is a `ScriptableObject` type.
- Default player and enemy skills currently exist as assets in `Assets/Resources/Battle/Skills`.
- `CharacterData.DefaultSkills` currently controls temporary player and enemy character skill ownership.
- `DefaultSkillAssetProvider` currently acts as the required skill asset lookup.
- `SkillTargetPattern` is opponent-relative, so the same skill target pattern can be interpreted against the enemy board when used by an ally and against the ally board when used by an enemy.
- Runtime cooldown state is not part of the current design.
- Runtime link participation state is not part of the current design.
- The current runtime mutable skill-related state is MP on `BattleUnit` and buffs on `BattleUnit.Buffs`.
- This separation is intended to make future ScriptableObject-based skill definitions safer.

## Enemy action model

- `EnemyActionData` currently remains code-defined runtime data.
- `EnemyActionData` wraps a `SkillData` reference instead of storing action name, damage, and target pattern directly.
- `EnemyActionSelector` reads the enemy unit's first runtime skill as its default action.
- Enemy action names, damage values, target previews, and damage target positions are read from the referenced `SkillData`.
- Enemy-specific skills are now represented by normal `SkillData` assets and assigned through `CharacterData.DefaultSkills`.

## Item model

- `ItemData` is item definition data.
- `ItemData` is a `ScriptableObject` type.
- Default items currently exist as assets in `Assets/Resources/Battle/Items`.
- `InventoryLoadoutData` is initial inventory ownership/count data.
- Default inventory currently exists as `Assets/Resources/Battle/Inventory/default_inventory.asset`.
- `DefaultInventoryProvider` currently acts as the required inventory loadout asset lookup.
- `InventoryItem` is runtime inventory state.
- Runtime count is stored in `InventoryItem.Count`, not `ItemData` or `InventoryLoadoutData`.
- Runtime inventory is created during default battle setup and passed into the command UI.
- `CommandPanelController` is now a display/controller consumer of inventory state rather than the inventory creation owner.
- This separation is intended to make future ScriptableObject-based item definitions and inventories safer.

## Cooldown and LinkCooldown policy

- Skill CT is not part of the target design.
- LinkCooldown is not part of the target design.
- `WAIT:N` and `LINK:N` are not target UI states.
- Active data fields and active UI flow for Skill CT / LinkCooldown have been removed.

## Link skill policy

- Automatic link partner selection is not part of the target design.
- Link skills specify their partner per skill through `SkillData.LinkPartnerCharacterId`.
- Current TwinHit specifies `rogue` as its link partner id.
- TwinHit currently requires user MP 2 + specified partner MP 2.
- If either the user or the specified partner lacks MP, the Link skill is blocked.
- Current implementation consumes the same `MpCost` from both the user and the specified partner.
- A character cannot be their own Link partner.
- A reserve character may be a Link partner and pay MP.
- Reserve Link partners are not shown as source flash cells because they are not placed on the active grid.
- Future Link skill cost values may be split into separate user/partner costs if needed.

## Remaining cleanup

- Move item ownership/count from `DefaultInventoryProvider` to a non-dummy battle loadout or quest state source later.
- Decide whether enemies should eventually support multiple action skills and selection policies instead of always using the first skill.
- Implement the broader quest/Wave loop later; the current battle still restarts as a single battle.
