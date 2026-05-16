# Current Battle Status

## Latest confirmed implementation notes

- Play start clears the skill description text, so default `New Text` is not shown.
- `CommandPanelController.Setup()` clears the skill description when no skill is currently hovered.
- Skill hover description can refresh while hovering the same skill.
- Swap and Item panels clear skill description and target preview.
- `SkillData` now stores `MpCost` and `Damage`.
- Skill damage is routed through `BattleUIManager.CalculateDamage()`.
- `CalculateDamage()` currently returns the base damage unchanged and is reserved for future buff/debuff modifiers.
- `BattleUIManager` has base buff/debuff helper methods:
  - `ApplyBuff()`
  - `FindBuff()`
  - `GetOppositeBuffType()`
  - `TickBuffsAtTurnStart()`
  - `TickBuffsInUnits()`
  - `TickBuffs()`
- Buff ticking is called from `StartNextTurn()` before rebuilding turn order.
- Buffs are not yet applied from skills.
- Buffs do not yet affect damage.
- Buff UI is not yet implemented.
- `ItemData` now stores `HealAmount` and `Count`.
- Dummy Potion starts at count 3.
- Potion use decrements count only after a valid forward ally target is found.
- Potion disappears from the item slot as `-` when count reaches 0.
- Dummy skill creation is split into `AddDefaultSkills()` and `CreateSkill()`.
- Dummy potion creation is split into `CreateDummyPotion()`.

## Stable reference

See the existing design notes in:

```text
docs/implementation_policy.md
docs/enemy_action_preview_policy.md
docs/enemy_action_preview_confirmed_notes.md
docs/enemy_frontline_replacement_policy.md
docs/enemy_frontline_replacement_confirmed_notes.md
```
