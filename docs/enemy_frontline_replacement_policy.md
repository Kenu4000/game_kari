# Enemy Frontline and Reserve Replacement Policy

## Purpose

This document records the confirmed policy for enemy frontline compaction and enemy reserve replacement.

This note does not cover enemy action preview or action order display.

## Confirmed policy

### Frontline zero is generally not allowed

Enemy side should not remain in a state where both frontline cells are empty while backline enemies exist.

However, if at least one frontline enemy remains, the backline does not automatically advance.

```text
FrontTop     BackTop
FrontBottom  BackBottom
```

### Defeating only one frontline enemy

If only one frontline enemy is defeated and the other frontline enemy remains alive, no backline movement occurs.

Example:

```text
Before:
F1  B1
F2  B2

F1 defeated:
-   B1
F2  B2
```

Since F2 remains in the frontline, B1 and B2 do not move forward.

If an enemy reserve exists, it is spawned into the defeated enemy's empty position.

```text
R1  B1
F2  B2
```

### Simultaneous frontline defeat

Backline enemies move forward only when both frontline enemies are defeated by the same single action / skill resolution and the enemy frontline becomes empty.

Example:

```text
Before:
F1  B1
F2  B2

F1 and F2 defeated by the same skill:
-   B1
-   B2

Backline advances:
B1  -
B2  -
```

This creates tactical value for attacks that can defeat both frontline enemies at once.

### Individual defeat is not simultaneous defeat

If FrontTop is defeated by one ally action and FrontBottom is defeated later by another ally action in the same turn, it is treated as individual defeat, not simultaneous defeat.

The simultaneous frontline defeat rule applies only within one action / skill resolution.

## Reserve replacement policy

### Simple replacement first

Complex spawn types are not planned at this stage.

Do not implement these yet:

- FrontSpawn
- BackSpawn
- SameSlotSpawn as separate data
- enemy-specific spawn position types
- branching by frontline/backline enemy role

### Normal single defeat

If no frontline-zero compaction occurs, enemy reserve replacement uses the defeated enemy's empty position.

### Frontline-zero compaction case

If simultaneous frontline defeat causes enemy frontline zero, compaction happens before reserve replacement.

Processing order:

1. Apply damage.
2. Collect defeated enemies from that skill/action.
3. Remove defeated enemies from the grid.
4. Check whether the enemy frontline is empty.
5. If frontline is empty and backline enemies exist, move backline enemies forward.
6. Spawn reserves into empty cells.

### Reserve spawn order after compaction

After compaction, reserves are spawned into empty cells in this order:

```text
FrontTop -> FrontBottom -> BackTop -> BackBottom
```

This order is chosen for implementation simplicity.

This may later be adjusted per enemy or per encounter if needed.

### Reserve count

If there are more reserves than empty grid cells, only as many reserves as there are empty cells are spawned.

Remaining reserves stay in reserve.

### Spawned reserve action state

Spawned enemy reserves do not act during the turn in which they are spawned.

They are treated as already acted and join speed order from the next turn.

## Ally-side note

The same simultaneous-frontline-defeat compaction concept may also be valid for ally-side KO processing.

Enemy Rotate / enemy Swap are not planned. Adding enemy-side Rotate/Swap would significantly change the game and should not be implemented as part of this policy.

## Design intent

This rule gives distinct value to different attack types.

Single-target high damage:

- reliably defeats one enemy
- does not necessarily collapse the enemy formation

Frontline / multi-target / combo attacks:

- can defeat both frontline enemies at once
- can pull backline enemies forward
- can create reserve replacement pressure

This supports the desired distinction between direct kill value and formation-breaking value.

## Current implementation implication

The current implementation processes enemy defeat immediately per target. To support this policy correctly, multi-target skill resolution should be changed so that defeated enemies are collected for the whole skill before reserve replacement happens.

Required direction:

- apply all damage for a skill
- collect all defeated enemies
- remove all defeated enemies
- perform frontline-zero compaction if needed
- then spawn enemy reserves

This matters for `TwinHit`, `Wave`, and future combo/area attacks.
