# Enemy Action Preview Policy

## Purpose

This document organizes the current design policy for enemy action preview, action order, enemy consecutive actions, and enemy AI depth.

Planning may happen in other chats, but final confirmation and repository markdown updates are handled in this implementation chat.

## 1. Already decided / already consistent with current direction

### No action order bar

Do not use a turn order bar.

Action order is shown by numbers near each character or in character status areas.

These numbers allow the player to see whether enemy consecutive actions are coming.

### Mixed speed order

The battle system uses a mixed speed order, not separate player/enemy phases.

Action order can be:

```text
Ally A -> Ally B -> Enemy 1 -> Enemy 2
```

or:

```text
Ally A -> Enemy 1 -> Ally B -> Enemy 2
```

### Enemy consecutive actions remain meaningful

Enemy consecutive actions are not treated as an error state.

They are a risk/reward element. Depending on the situation, the player may:

- avoid them
- accept them
- intentionally induce them

### No mercy correction for hidden enemy actions

Do not weaken enemy actions just because the player has not seen their details.

Hidden enemy actions still use normal enemy skill selection and normal damage.

### Speed is not simply better when higher

Speed should function as an action-order distribution stat.

Very fast allies can cluster ally actions together, which can also cluster enemy actions later.

Spreading ally speed can help insert ally turns between enemy turns.

## 2. Newly confirmed design policy from planning

### Enemy actions are not fully previewed at turn start

Do not show all enemy actions at the start of the turn.

Reasons:

- too much information
- too much calculation pressure
- too close to a perfect-information puzzle
- the existing system already has enough decision factors: 2x2 formation, Rotate, Skill, Swap, Item

### Preview only the next enemy action immediately before it

Enemy action details are shown only when the currently selecting ally is immediately before that enemy in action order.

Example:

```text
Ally A -> Enemy 1
```

During Ally A's command selection, show Enemy 1's action preview.

Example:

```text
Ally A -> Ally B -> Enemy 1
```

During Ally A's command selection, do not show Enemy 1's action preview.
During Ally B's command selection, show Enemy 1's action preview.

### Enemy consecutive actions only preview the first enemy

Example:

```text
Ally A -> Enemy 1 -> Enemy 2 -> Ally B
```

During Ally A's command selection, show Enemy 1's action preview only.
Do not show Enemy 2's action details.

The player still sees from action order numbers that Enemy 2 will act after Enemy 1.

### Preview display contents

Show:

- enemy action name
- target range
- target highlight on ally grid

Do not show detailed internal numbers at this stage:

- exact damage value
- attack multiplier
- hit rate
- internal AI condition
- detailed additional-effect numbers

### Previewed action is locked

Once an enemy action preview is shown, that enemy's action is fixed until its turn.

If the player changes formation with Rotate or Swap afterward, the enemy still executes the already-previewed action.

This makes previewed information reliable.

## 3. Enemy AI policy

Enemy AI should not search for optimal solutions.

The goal is not smart AI. The goal is to avoid obviously wasteful actions.

Basic structure:

- each enemy has action candidates
- each candidate has usability conditions
- unusable actions are excluded
- obviously wasteful actions are avoided
- remaining candidates are selected with weights

Examples:

```text
Frontline Slash:
usable only when at least one ally is in the frontline

Heal:
usable only when an enemy-side unit has reduced HP

Debuff:
lower weight if the same debuff is already active
```

## 4. Enemy AI features not planned for initial implementation

Do not implement these initially:

- multi-turn prediction
- player Rotate prediction
- optimal search
- win-rate calculation
- complex simulation

The battle should be made interesting through enemy skill shape, target range, action order, and interaction with formation/Rotate rather than deep AI.

## 5. Enemy skill type policy

### Position-fixed skills

Target fixed grid cells, rows, or columns.

They can miss if the player moves away with Rotate or Swap.

Examples:

- FrontTop thrust
- frontline slash
- bottom sweep

### Tracking skills

Target a character rather than a fixed cell.

They follow the target even if that character moves.

Examples:

- snipe
- weak-target hunt
- curse

Tracking skills should not dominate the enemy kit. If too many enemy actions track characters, positioning loses value.

The default direction is to center enemy design on position-fixed skills.

## 6. Implementation implications

The current dummy enemy action system already has enemy-specific action data.

Next implementation step should be enemy action preview:

1. Detect whether the next unacted unit after the active ally is an enemy.
2. If so, choose and lock that enemy's action for preview.
3. Display the action name and target range.
4. Highlight target cells on the ally grid.
5. When that enemy acts, use the locked action.
6. If no action was previewed, choose the action at execution time.
7. For enemy consecutive actions, preview only the first enemy after the active ally.

## 7. Current summary

- No action order bar.
- Action order is represented by numbers near characters/statuses.
- Do not show all enemy actions at turn start.
- Show only the next enemy action immediately before it.
- Show action name and target range, not exact damage values.
- Highlight target range on the ally grid.
- Once shown, the enemy action is locked until execution.
- Enemy consecutive actions after the first remain detailed-unknown.
- Do not weaken hidden enemy actions.
- Enemy consecutive actions are a risk/reward element.
- Speed distribution is a strategic element.
- Enemy AI should be condition filtering plus weighted selection, not optimal search.
