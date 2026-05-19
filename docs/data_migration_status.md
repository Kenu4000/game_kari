# Data Migration Status

## Purpose

This document tracks the post Battle MVP v0.1 data-migration work.

The current policy is to reduce hardcoded dummy-data responsibility from `BattleUIManager.cs` without changing battle behavior and while migrating data toward ScriptableObject-backed assets in small steps.

## Confirmed steps

- Battle MVP v0.1 is complete.
- `CharacterData` is now a `ScriptableObject` type with a Create menu entry: `GameKari/Battle/Character Data`.
- `DummyCharacterFactory` still owns temporary `CharacterData` creation for the active dummy battle.
- `DummyCharacterFactory.CreateCharacterData(...)` creates runtime `CharacterData` instances through `ScriptableObject.CreateInstance<CharacterData>()` and sets character id, display name, max HP, and speed.
- `DummyBattleFactory` owns temporary `BattleUnit` creation.
- `DummyBattleFactory.CreateUnit(...)` creates `CharacterData` through `DummyCharacterFactory`, creates a `BattleUnit`, and delegates default skill assignment to `DummySkillFactory`.
- `DummySkillCatalog` owns the current temporary dummy skill definitions.
- `DummySkillCatalog.CreateDefaultSkills()` creates the current Slash, Pierce, TwinHit, and Focus dummy skill list.
- `DummySkillFactory` owns temporary default skill assignment to a `BattleUnit`.
- `DummySkillFactory.AddDefaultSkills(...)` obtains the default skill list from `DummySkillCatalog` and adds non-null skills to `BattleUnit.Skills`.
- `DummyEnemyActionFactory` owns temporary enemy action data.
- `EnemyTargetPattern` and `EnemyActionData` have been moved out of `BattleUIManager`.
- `DummyBattleSetupFactory` owns the current temporary battle setup data.
- `DummyBattleSetupFactory.CreateDefaultSetup()` creates the default ally units, enemy units, ally reserve, enemy reserve, and initial grid placements.
- `BattleUIManager` now calls `DummyBattleSetupFactory.CreateDefaultSetup()` during dummy battle bootstrap.
- `BattleUIManager` now applies setup data through `ApplyDummyBattleSetup(...)` and `ApplyDummyUnitPlacements(...)`.
- `BattleUIManager` still owns runtime grid/list application, UI updates, turn flow, and action resolution.
- `BattleUIManager` still calls `DummyEnemyActionFactory.SetDefaultEnemyActions(...)` after applying dummy enemy setup.
- `BattleUIManager` still calls `DummyEnemyActionFactory.SelectEnemyAction(...)` when selecting or previewing enemy actions.

## Current dummy ally setup

- Knight: HP 130, speed 12, position `FrontTop`
- Mage: HP 80, speed 15, position `BackTop`
- Cleric: HP 90, speed 9, position `FrontBottom`
- Rogue: HP 95, speed 18, position `BackBottom`
- Reserve: HP 100, speed 11, ally reserve
- Fallback active unit: Knight

## Current dummy skills

- Slash: `s1`, Personal, damage 20, target `FrontTopEnemy`, CT 0
- Pierce: `s2`, Personal, damage 20, target `FrontBottomEnemy`, CT 0
- TwinHit: `s3`, Link, damage 15, target `BothFrontEnemies`, CT 2, LinkCooldown 1
- Focus: `s4`, Personal, damage 0, target `Self`, effect `ApplyBuff`, effect target `Self`, buff `AttackUp`, buff turns 2, CT 2

## Current dummy enemy setup

- Goblin A: HP 70, speed 10, position `FrontTop`
- Archer: HP 30, speed 13, position `BackTop`
- Goblin B: HP 50, speed 8, position `FrontBottom`
- Shaman: HP 25, speed 7, position `BackBottom`
- Enemy Reserve: HP 65, speed 11, enemy reserve

## Current dummy enemy actions

- Goblin A: `Claw`, damage 60, target `SameGridPosAlly`
- Archer: `Arrow`, damage 45, target `AllyFrontTop`
- Goblin B: `Bite`, damage 60, target `AllyFrontBottom`
- Shaman: `Hex`, damage 25, target `AllAllies`
- Enemy Reserve: `Strike`, damage 60, target `SameGridPosAlly`
- Fallback action: `Strike`, damage 60, target `SameGridPosAlly`

## Not yet migrated

- CharacterData is not yet loaded from committed `.asset` files in battle setup.
- SkillData is still constructed in code through `DummySkillCatalog`.
- Dummy battle setup is still hardcoded in `DummyBattleSetupFactory`.
- `SkillData` has not been converted to ScriptableObject yet.

## Next suggested step

Move `CharacterData` from runtime-created ScriptableObject instances toward committed `.asset` references in a separate, small step. Do not convert `SkillData` until the CharacterData asset-reference path is stable.
