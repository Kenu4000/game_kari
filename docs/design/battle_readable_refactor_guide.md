# Battle readable refactor guide

This document is a reading guide for the battle code refactor.
The goal is not to make an advanced architecture. The goal is to make the code understandable and debuggable.

## Refactor principle

Keep `main` as the working version.
Do large edits on `refactor/battle-readable`.
If the branch becomes too broken, switch back to `main`.

```powershell
git switch main
```

## First-pass rule

The first pass must not change battle behavior.
It may:

- make `BattleUIManager` partial
- add readable comments
- add partial scaffold files
- normalize obviously compressed lines

It must not:

- change KO rules
- change enemy replacement rules
- change turn order rules
- change damage timing
- change status panel timing

## File map

### BattleUIManager.cs

Still contains most existing code.
This is the old large manager and should be reduced gradually.

### BattleUIManager.Actions.cs

Future home for player/enemy action resolution.

### BattleUIManager.Animation.cs

Future home for methods that call `SkillAnimationPlayer`.
The animation player itself stays in `Assets/Scripts/Battle/Animation/`.

### BattleUIManager.KO.cs

Future home for KO, reserve replacement, and enemy compacting.
This is fragile and should be moved late.

### BattleUIManager.Preview.cs

Future home for hover, target preview, silhouette preview, and enemy preview visuals.
Preview code should not change battle data.

### BattleUIManager.StatusPanels.cs

Future home for status panel drawing and HP bar display.

### BattleUIManager.Turns.cs

Future home for turn selection and phase transitions.

## Debug checklist after each refactor commit

1. Battle starts.
2. Skill list opens.
3. Hover preview appears.
4. Rotate still works.
5. Skill animation plays.
6. Damage number appears.
7. HP bar changes.
8. KO does not break status panels.
9. Enemy turn proceeds.
10. Wave clear still works.

## Comment style

Comments should explain why something exists, not only what the line does.

Good:

```csharp
// KO fade must finish before status panel compaction.
// Otherwise the reused HP bar can look like a defeated enemy healed.
```

Bad:

```csharp
// Set alpha to 0.
```