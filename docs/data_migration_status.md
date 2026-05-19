# Data Migration Status

## Purpose

This document tracks the post Battle MVP v0.1 data-migration work.

The current policy is to reduce hardcoded dummy-data responsibility from `BattleUIManager.cs` without changing battle behavior and without immediately converting the system to ScriptableObject assets.

## Confirmed steps

- Battle MVP v0.1 is complete.
- `DummyBattleFactory` owns temporary unit creation and default skill creation.
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

- CharacterData is still constructed in code through dummy factories.
- SkillData is still constructed in code through dummy factories.
- Dummy battle setup is still hardcoded in `DummyBattleSetupFactory`.
- No ScriptableObject asset migration has started yet.

## Next suggested step

Keep the current factory-based dummy setup stable, then consider moving from code-built `CharacterData` / `SkillData` toward Inspector or ScriptableObject-backed data in a separate, small step.
