# PROJECT_CONTEXT

## Current design direction

This project is currently moving from the old Wave/Distance progression model to a fixed-route Quest model.

The old model used Wave Clear to increase Distance and Progress. That design is no longer the target. Distance-based progression, random encounters, stealth, Kakera Drive, and safe-lane mechanics are removed from the target design.

The new target is:

```text
Base
↓
Quest Select
↓
Quest Start
↓
Movement UI
↓
Fixed Battle Point
↓
Battle Preparation
↓
Battle
↓
Battle Result
↓
Movement UI
↓
Event Point / Battle Point / Boss
↓
Quest Result
↓
Return to Base
```

## Terminology

Internal code may temporarily keep existing `WaveData` names to reduce churn.

Player-facing terminology should avoid `Wave` and use route / battle point / raid / battle / event / boss wording.

Current interpretation:

- `Quest`: one fixed route from Start to Boss.
- `RoutePoint`: one point on the fixed route.
- `Battle`: a fixed combat encounter at a route point.
- `Boss`: the final fixed combat point.
- `WaveData`: transitional internal name for fixed battle-point combat data.

## Route point types

Initial route point types are fixed to five:

- `Start`.
- `Normal`.
- `Battle`.
- `Event`.
- `Boss`.

`Goal` is not used as a separate type. The final point is `Boss`.

Initial behavior:

- `Start`: route start marker.
- `Normal`: no gameplay effect.
- `Battle`: fixed battle.
- `Event`: text-only event, then proceed.
- `Boss`: final battle, then Quest Result.

## Removed systems

Do not extend or build new features around these systems:

- Distance reward progression.
- `BaseDistance`.
- `DistanceGain`.
- `CurrentDistance`.
- `TargetDistance`.
- Distance clear-rank multipliers.
- Distance / Progress display in battle result.
- Random encounters.
- Stealth.
- Kakera Drive.
- Safe lane / safety conversion.
- Kakera as movement safety resource.
- Movement GO button.
- Movement text window.
- Always-visible status / formation / item controls on movement UI.

Existing code may still contain these names while migration is in progress.

## Battle resource rules to keep

- Quest state preserves ally HP, MP, KO, formation state, reserves, and item counts across fixed battles.
- Returning to Base restores HP, MP, and KO state.
- Ally max MP is currently 4.
- At a new battle start, living ally front-line and reserve characters recover MP +1.
- KO allies do not recover MP.
- KO allies preserve MP while KO.
- Items are Quest-wide resources.
- Enemies are replaced for each fixed battle.
- Enemies do not use MP.
- Core battle commands remain Skill / Swap / Item / Rotate.
- Turn order remains Speed-based.
- Buffs should be cleared at battle end.
- 1Turn Kill keeps living-party HP +5.

## Clear evaluation

Player-facing clear categories:

- `1Turn Kill`.
- `2Turn Kill`.
- `3+ Turn`.

`4+Turn` should not be shown separately. Old internal values can be mapped into `3+ Turn` during migration.

## Kakera

Kakera is now a Quest-only battle preparation resource.

Rules:

- Earned from battle clear evaluation.
- Valid only in the current Quest.
- Cannot be brought from Base.
- Cannot be taken back to Base.
- Max stock is 9.

Gain values:

- `1Turn Kill`: +3.
- `2Turn Kill`: +2.
- `3+ Turn`: +1.

Initial use:

- Spend 1 Kakera to scout the next battle's enemy information.

Scout target:

- Enemy count.
- Enemy placement.
- Enemy role.

Enemy role display may be deferred; placeholder or fixed strings are acceptable initially.

Do not initially use Kakera for HP recovery, KO revival, MP recovery, enemy pre-damage, battle skip, or party-wide battle buffs.

## Battle preparation screen

Fixed battle points should enter a preparation screen before combat.

Initial implementation should use a temporary panel inside the existing Battle UI.

Initial preparation screen scope:

- Party HP / MP / KO overview.
- Kakera stock.
- Enemy info state: unscouted / scouted.
- Spend 1 Kakera to scout.
- Start Battle.

Deferred:

- Formation editing.
- Item use.
- Skill inspection.
- Link condition inspection.
- Detailed enemy role display.

Back to movement UI is basically not allowed after a raid has started. Retreat can be designed later.

## Battle Result

Battle Result replaces old Wave Result / Distance Result.

Display:

```text
Battle Clear
Clear: 1Turn Kill
Kakera: +3
EXP: +10
Lv Up: None

[Formation] [Next]
```

EXP and Lv Up are placeholder display values for now. Real growth processing is deferred.

`Formation` opens the same preparation-style panel. `Next` returns to movement UI and advances the route.

## 1Turn Kill HP bonus

1Turn Kill grants living-party HP +5.

Rules:

- Living front-line allies: HP +5.
- Living reserve allies: HP +5.
- KO allies are excluded.
- Clamp at MaxHP.
- Show as a short `1TurnKill BONUS!` cue / recovery animation, not as a major result-table field.

## Movement UI

Movement UI is a route confirmation and travel presentation screen, not a management screen.

Layout:

- Top: radar-style presentation.
- Bottom: accurate route bar.

Top radar:

- Black semicircle radar.
- Outer tick marks.
- Truck / bus pictogram.
- Nearest 1-2 upcoming symbols.
- Abstract radar/sensor style.

Bottom route bar:

- Accurate route information.
- Current position marker.
- Start / normal / battle / event / boss symbols.
- Remaining point or segment count.

Example:

```text
S ─ ○ ─ 🚚 ─ ⚔ ─ ⚠ ─ ○ ─ ⚔ ─ Boss
```

Movement animation:

- Around 1 second by default.
- Click can pause / resume.
- Show a pause mark while paused.
- Click / skip behavior should be kept simple during initial implementation.

## Quest Result

Distance is not shown.

Initial Quest Result:

```text
Quest Clear
Battles Cleared: 3 / 3
Kakera Earned: total
EXP: total
Next: Return to Base
```

Do not show fixed `Party: Alive x/y, KO z` on the main Quest Result. Party details can remain internal or move to a future detail / formation screen.

Deferred:

- Grade.
- Item usage.
- Remaining items.
- Rewards.
- Skill enhancement resources.
- Level-up processing.

## Implementation order from here

Recommended migration order:

1. Keep docs aligned with fixed-route design.
2. Stop extending Distance-based code.
3. Remove Distance / Progress from displayed results.
4. Map clear display to `1Turn Kill`, `2Turn Kill`, `3+ Turn`.
5. Add Kakera stock and Kakera gain.
6. Replace current Wave/Quest result display with Battle Result fields.
7. Add `RoutePointData` / route model.
8. Add temporary movement route bar.
9. Add temporary battle preparation panel.
10. Add radar presentation later.

## Current warning

The repository may still contain transitional code from the previous Wave/Distance implementation, including `WaveProgressState`, `BaseDistance`, `TargetDistance`, and related result fields. These are migration targets, not the desired final design.
