# Data Migration Status

## Purpose

This document tracks the post Battle MVP v0.1 data-migration work.

The current policy is to reduce hardcoded dummy-data responsibility from `BattleUIManager.cs` without changing battle behavior and without immediately converting the system to ScriptableObject assets.

## Confirmed steps

- Battle MVP v0.1 is complete.
- `DummyBattleFactory` owns temporary unit creation and default skill creation.
- `BattleUIManager` calls `DummyBattleFactory.CreateUnit(...)` when preparing the current dummy ally and enemy units.
- `DummyEnemyActionFactory` owns temporary enemy action data.
- `EnemyTargetPattern` and `EnemyActionData` have been moved out of `BattleUIManager`.
- `BattleUIManager` now calls `DummyEnemyActionFactory.SetDefaultEnemyActions(...)` during dummy enemy setup.
- `BattleUIManager` now calls `DummyEnemyActionFactory.SelectEnemyAction(...)` when selecting or previewing enemy actions.

## Current dummy enemy actions

- Goblin A: `Claw`, damage 60, target `SameGridPosAlly`
- Archer: `Arrow`, damage 45, target `AllyFrontTop`
- Goblin B: `Bite`, damage 60, target `AllyFrontBottom`
- Shaman: `Hex`, damage 25, target `AllAllies`
- Enemy Reserve: `Strike`, damage 60, target `SameGridPosAlly`
- Fallback action: `Strike`, damage 60, target `SameGridPosAlly`

## Not yet migrated

- Dummy ally formation setup remains in `BattleUIManager.SetupDummyAllies()`.
- Dummy enemy formation setup remains in `BattleUIManager.SetupDummyEnemies()`.
- CharacterData is still constructed in code through dummy factories.
- SkillData is still constructed in code through dummy factories.
- No ScriptableObject asset migration has started yet.

## Next suggested step

Create a small battle setup factory that returns the initial dummy ally units, enemy units, reserve units, and grid placement data, while keeping battle behavior unchanged.
