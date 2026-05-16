# Enemy Action Preview Confirmed Notes

## Purpose

This document records confirmed answers and implementation-relevant notes for the enemy action preview design.

It supplements `docs/enemy_action_preview_policy.md`.

## Confirmed answers

### Turn starts with enemy

If the first acting unit in a turn is an enemy, that enemy action is treated as unpreviewed.

Reason:

- There is no ally command opportunity immediately before it.
- Even if the action were displayed at turn start, the player could not respond to it.

### Preview locks the action, not the exact target cells

When an enemy action is previewed, the selected action is locked.

The exact affected cells are still determined by that action's targeting rule at execution time.

Example:

```text
Preview: Frontline Slash
Player rotates or swaps
Enemy still uses Frontline Slash
The action resolves against the current frontline at execution time
```

This keeps the preview useful while preserving player response through Rotate / Swap.

### Enemy AI condition timing

Enemy action conditions are evaluated when the preview is selected.

If an action is selected and previewed, it will not be replaced at execution time even if the player moves away and the action misses.

This makes previewed information reliable.

### Previewed enemy is KO'd

If a previewed enemy is KO'd before acting, its preview disappears.

Do not immediately reveal the next enemy's detailed action after that KO.

Reason:

- The ally action has already been committed.
- The player no longer has a command opportunity to respond to the newly revealed action.

### Enemy consecutive actions

For a sequence like:

```text
Ally A -> Enemy 1 -> Enemy 2 -> Ally B
```

Only Enemy 1's detailed action is previewed during Ally A's command selection.

Enemy 2 remains detailed-unknown, although the action order numbers show that another enemy action is coming.

### Tracking skills are questionable

Tracking skills may not fit the current core design.

Concerns:

- They require UI that communicates that a character, not a cell, is targeted.
- If the target character moves by Rotate, the preview display must still make sense.
- Too many tracking skills reduce the value of grid positioning.

Current direction:

- Do not prioritize tracking skills.
- Focus initial enemy action preview implementation on position-fixed skills.
- Reconsider tracking skills later only if there is a clear gameplay need.

## Preview UI direction

### Enemy status area

The enemy action preview should be shown near the corresponding enemy's status entry.

Initial ideal UI:

- Increase spacing between enemy status slots.
- Show a speech-bubble-like UI above or near only the enemy whose action is previewed.
- Display the action name in that bubble.

Example concept:

```text
[ speech bubble: Frontline Slash ]
[ Enemy Status Slot ]
```

Only the previewed enemy gets this speech bubble.

### Target highlight

The target range should be highlighted on the ally grid.

Color policy:

- active ally: blue
- player skill preview: yellow
- enemy action preview: red

Enemy action preview highlight should preferably pulse slowly rather than stay as a static red color.

### Temporary formation after Rotate

During Rotate temporary formation state:

- Keep the enemy preview visible.
- After formation confirmation, update the highlighted target range according to the confirmed formation and the locked enemy action.

This is less problematic while previewed enemy skills are position-fixed rather than tracking.

## Implementation implications

Minimal implementation should focus on:

1. Detect whether the next unacted unit after the active ally is an enemy.
2. Choose and lock that enemy's action when the active ally's command selection begins.
3. Show the locked action name near that enemy's status slot.
4. Highlight the affected ally grid cells in red.
5. Pulse the enemy preview highlight if practical; static red is acceptable for the first pass.
6. If the previewed enemy acts, execute the locked action.
7. If the previewed enemy is KO'd, clear the preview.
8. If the next unit is enemy at the start of a turn with no ally immediately before it, no preview is shown.

Do not include in the first pass:

- tracking skills
- full enemy AI weighting
- damage number preview
- multi-enemy detailed preview
- advanced speech bubble art
