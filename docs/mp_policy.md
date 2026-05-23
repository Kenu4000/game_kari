# Battle / Quest Resource Policy

## Status

This document replaces the previous Wave/Distance-centered policy.

The current design direction is now a fixed-route Quest system. Internally, existing `WaveData` names may remain during transition, but player-facing terminology should avoid "Wave" and use route, battle point, raid, battle, event, or boss wording instead.

## High-level loop

Target final flow:

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
  - Clear evaluation
  - Kakera gained
  - EXP
  - Lv Up
  - Formation / Next
↓
Next
↓
Movement UI
↓
Event Point or next Battle Point
↓
...
↓
Boss
↓
Quest Result
↓
Return to Base
```

## Route policy

Quest progression is no longer driven by Distance rewards.

A Quest is a fixed one-way route made of route points.

Initial route point types:

- `Start`.
- `Normal`.
- `Battle`.
- `Event`.
- `Boss`.

`Goal` is not used as a separate point type in the current design. The final route combat point is `Boss`.

Initial behavior:

- `Start`: route start marker.
- `Normal`: no gameplay effect in the initial implementation.
- `Battle`: fixed battle occurs here.
- `Event`: text-only event, then proceed.
- `Boss`: final fixed battle; after result, proceed to Quest Result.

Random battle encounters are removed. Battles occur only at fixed `Battle` or `Boss` route points.

## Removed systems

The following systems are removed from the active target design:

- Distance reward progression.
- `BaseDistance`.
- `DistanceGain`.
- `CurrentDistance`.
- `TargetDistance`.
- Clear-rank Distance multipliers.
- Distance / Progress display on battle result.
- Random encounters on route points or route edges.
- Stealth.
- Kakera Drive.
- Safe lane / safety conversion.
- Kakera as movement safety resource.
- GO button on movement UI.
- Text window on movement UI.
- Always-visible party status on movement UI.
- Always-visible formation or item buttons on movement UI.

Existing code may still contain transitional names or fields until cleanup. New implementation should not extend the removed systems.

## Systems to keep

The following battle-resource rules remain active:

- Quest state preserves ally HP, MP, KO state, formation state, reserves, and item counts across fixed battles.
- Returning to Base restores HP, MP, and KO state.
- Ally max MP is currently 4.
- At a new battle start, living ally front-line and reserve characters recover MP +1.
- KO allies do not recover MP.
- KO allies preserve their current MP while KO.
- Items are Quest-wide resources.
- Enemies are replaced for each fixed battle.
- Enemies do not use MP in the current design.
- Core battle commands remain: Skill, Swap, Item, Rotate.
- Turn order remains Speed-based.
- Buffs should be cleared at battle end.
- 1Turn Kill bonus keeps living-party HP +5.

## Clear evaluation

Clear evaluation is simplified to three player-facing categories:

- `1Turn Kill`.
- `2Turn Kill`.
- `3+ Turn`.

`4+Turn` is not shown separately. Internal code may temporarily keep old enum values, but display and rewards should map all turn counts of 3 or more to `3+ Turn`.

Evaluation basis:

- Battle starts at turn count 1.
- If all active enemies and enemy reserves are defeated during the first turn cycle, result is `1Turn Kill`.
- If defeated during turn count 2, result is `2Turn Kill`.
- If defeated during turn count 3 or later, result is `3+ Turn`.

## Kakera policy

Kakera is now a Quest-only battle-preparation resource.

Rules:

- Earned from battle clear evaluation.
- Valid only during the current Quest.
- Cannot be brought from Base.
- Cannot be taken back to Base.
- Used in later battle preparation screens.
- Initial max stock is 9.

Gain values:

- `1Turn Kill`: Kakera +3.
- `2Turn Kill`: Kakera +2.
- `3+ Turn`: Kakera +1.

Initial use:

- Spend 1 Kakera to scout the next battle's enemy information.

Scout display target:

- Enemy count.
- Enemy placement.
- Enemy role.

Enemy role display may be implemented later. Initial implementation may show placeholder or fixed role strings.

Do not add these initial Kakera effects:

- HP recovery.
- KO revival.
- MP recovery.
- Pre-battle enemy damage.
- Battle skip.
- Party-wide battle buff.

Reason: these effects can damage HP / MP / KO resource management before the route system is stable.

## Battle preparation screen

A fixed battle point should not enter battle immediately.

Flow:

```text
Movement UI
↓
Fixed Battle Point reached
↓
Raid / ambush title cue
↓
Battle Preparation screen
↓
Battle Start
```

Initial implementation should use a temporary panel inside the existing Battle UI rather than a separate scene.

Initial preparation screen scope:

Display:

- Party HP / MP / KO overview.
- Kakera stock.
- Enemy info state: unscouted / scouted.

Actions:

- Spend 1 Kakera to scout enemy information.
- Start Battle.

Deferred preparation features:

- Formation editing.
- Item use.
- Skill list inspection.
- Link condition inspection.
- Detailed enemy role display.

Back behavior:

- Returning from battle preparation to movement UI is basically not allowed once the raid has started.
- Retreat may be added later if explicitly designed.

## Battle Result policy

Battle Result replaces the previous Wave Result / Distance Result display.

Display:

- Clear evaluation.
- Kakera gained.
- EXP.
- Lv Up.

Bottom choices:

- Formation.
- Next.

Initial example:

```text
Battle Clear
Clear: 1Turn Kill
Kakera: +3
EXP: +10
Lv Up: None

[Formation] [Next]
```

EXP and Lv Up are initially placeholder display values only. Real growth processing is deferred.

`Formation` opens the same temporary preparation-style panel used before battle. It is a post-battle preparation screen, not only a grid edit command.

Initial `Formation` scope:

- HP / MP / KO overview.
- Kakera stock.
- Enemy info state where applicable.
- Return to result / continue controls.

Deferred `Formation` scope:

- Formation editing.
- Item use.
- Skill list inspection.
- Link condition inspection.

`Next` returns to movement UI and advances along the fixed route.

## 1Turn Kill HP bonus

1Turn Kill keeps the living-party HP recovery bonus.

Rules:

- Living front-line allies: HP +5.
- Living reserve allies: HP +5.
- KO allies are excluded.
- Recovery is clamped at MaxHP.
- This bonus should not be a major Battle Result table item.
- Show it as a short `1TurnKill BONUS!`-style cue and recovery animation immediately after battle.

## Movement UI policy

Movement UI is a route confirmation / travel presentation screen, not a management screen.

Layout direction:

- Top: radar-style presentation area.
- Bottom: accurate route bar.

Top radar area:

- Black semicircle radar.
- Outer tick marks.
- Truck / bus pictogram.
- Only the nearest 1-2 upcoming symbols.
- Abstract radar/sensor expression preferred over location-specific background dependency.
- Symbols can move in sync with the rotating lower arc / radar presentation.

Bottom route bar:

- Accurate route information.
- Current position marker.
- Start / normal / battle / event / boss symbols.
- Remaining point count or segment count.

Example:

```text
S ─ ○ ─ 🚚 ─ ⚔ ─ ⚠ ─ ○ ─ ⚔ ─ Boss
```

Remove from movement UI:

- GO button.
- Text window.
- Always-visible party status.
- Always-visible formation button.
- Always-visible item button.
- Explanation panel.
- Stealth range.
- Kakera Drive display.
- Random encounter warning.

Movement animation:

- Usually around 1 second.
- Click can skip or pause depending on state.
- Screen click pauses drawing/animation.
- Another click resumes.
- Show a pause mark while paused.

Visual direction from current mockup:

- Monotone black/white pictogram-like style for truck and symbols.
- Top and bottom route graphics should visually align.
- Background can be added later, but abstract radar style is acceptable.

## Quest Result policy

Distance is not shown.

Initial Quest Result should be minimal:

```text
Quest Clear
Battles Cleared: 3 / 3
Kakera Earned: total
EXP: total
Next: Return to Base
```

Do not show fixed `Party: Alive x/y, KO z` in the main Quest Result.

Party KO / alive details can remain internal or be shown in a future detail screen / formation screen.

Deferred Quest Result fields:

- Grade.
- Item usage.
- Remaining items.
- Rewards.
- Skill enhancement resources.
- Level-up processing.

## Failure policy

Quest failure should eventually use a Quest Result-style screen rather than a plain Defeat-only display.

Initial target:

```text
Quest Failed
Battles Cleared: n / total
Kakera Earned: total
EXP: total
Next: Return to Base
```

This is deferred until the route-based loop is in place.

## Data model direction

Keep internal `WaveData` names for now to reduce churn.

Interpret transitional data as:

- `WaveData`: fixed battle point battle data.
- `QuestData`: route-level quest data.
- `QuestProgressState`: current route / battle progression state.

Future data additions:

- `RoutePointData`.
- `QuestRouteData` or route point list on `QuestData`.
- Route point type enum: Start / Normal / Battle / Event / Boss.
- Kakera stock on quest runtime state.
- Battle result data separate from Quest result data.

Future cleanup:

- Stop using Distance fields.
- Remove Distance display.
- Move route progression into QuestProgressState.
- Consider renaming `WaveData` only after the route model is stable.

## Deferred systems

The following remain deferred:

- ScriptableObject QuestData.
- ScriptableObject route data.
- Full movement radar animation.
- Formation editing in preparation screen.
- Item use in preparation screen.
- Skill inspection in preparation screen.
- Enemy role display implementation.
- EXP processing.
- Lv Up processing.
- Quest reward processing.
- Grade evaluation.
- Item usage count display.
- Real Base scene.
- Quest Select scene.
