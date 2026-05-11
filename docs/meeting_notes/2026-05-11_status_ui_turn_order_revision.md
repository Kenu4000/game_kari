# Status UI / Turn Order Revision Notes

## Summary

The battle UI direction has changed from a bottom turn-order bar to symmetric enemy/ally status panels.

## Adopted changes

- The bottom `TurnOrderBar` concept is deprecated.
- Enemy status should be displayed on the left side of the screen.
- Ally status should remain on the right side of the screen.
- Enemy status UI should mirror the ally status UI layout.
- Turn order is displayed as a small number beside each unit's status icon, similar to the earliest UI sketch.
- The status list order does not change when units move or when the grid rotates. This avoids excessive visual movement.

## Turn-order number rules

- The number is based on the order counted from the top of the turn.
- The number does not change during the turn, even after commands advance.
- KO units display `×`.
- Units that have already acted lose their number display.
- Empty / unassigned status slots display blank.

## Current acting unit highlight

- The current acting unit's standing character illustration glows blue on the battlefield grid.
- The corresponding status frame is highlighted with a blue border.
- The turn-order number `1` is also emphasized.

## Enemy status content

Enemy status slots should include:

- Enemy type icon
- Small HP bar
- Turn-order number
- Current acting highlight
- Buff/debuff icon area
- Enemy name only if needed, displayed small

## Ally status content

Ally status remains compact and should include:

- Face icon
- HP/MP bars and numbers
- Turn-order number
- Buff/debuff icon area
- Current acting highlight

## Implementation impact

The next `BattleUICreator.cs` revision should remove `TurnOrderBar` generation and add/generate `EnemyStatusPanel` on the left side. `AllyStatusPanel` should be mirrored on the right side. Turn order should be represented in status slots, not as a separate bottom timeline.
