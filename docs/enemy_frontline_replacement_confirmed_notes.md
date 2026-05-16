# Enemy Frontline Replacement Confirmed Notes

## Purpose

This document records the follow-up confirmations for enemy frontline compaction and reserve replacement.

It supplements `docs/enemy_frontline_replacement_policy.md`.

## Confirmed details

### Enemy-only first implementation

The next implementation should target the enemy side only.

Ally-side simultaneous-frontline-defeat compaction may be valid later, but it is not part of the first implementation.

Ally reserve replacement remains KO-position based for now.

### Enemy Rotate / Swap

Enemy-side Rotate and Swap are not planned.

Adding enemy Rotate or enemy Swap would significantly change the game structure and should not be included in this feature.

### Simultaneous defeat timing

Simultaneous defeat is judged when HP reaches 0 during one action / one skill resolution.

If a multi-target skill causes both frontline enemies to become HP0 during the same skill resolution, this can trigger frontline compaction.

### Single action causing frontline zero

If the enemy frontline becomes empty as the result of one action, frontline compaction should occur even if only one remaining frontline enemy was defeated by that action.

This matters in cases where one frontline cell was already empty before the action.

This case is expected to be uncommon because enemy reserve replacement prioritizes frontline cells, but allowing it is acceptable and may create useful tactical value.

### Individual defeat across separate actions

If FrontTop is defeated by one ally action and FrontBottom is defeated later by another ally action, that is individual defeat across separate actions.

The key implementation unit is one action / skill resolution, not the whole turn.

### Reserve spawn order

Enemy reserve spawn order after compaction is:

```text
FrontTop -> FrontBottom -> BackTop -> BackBottom
```

This is for implementation simplicity.

Future enemy-specific priority adjustments may be considered later, but they are not part of the initial implementation.

### Reserve count

If more reserves exist than empty grid cells, spawn only as many as can fit.

Remaining reserves stay in reserve.

### Existing backline enemy action state

If an existing backline enemy moves forward during frontline compaction, its action state is preserved.

- If it was unacted, it remains unacted and can still act later that turn.
- Its TurnNumber is preserved.
- Only its GridPos changes.

### Spawned reserve action state

A newly spawned reserve enemy is treated as already acted for that turn.

It gets no TurnNumber during the current turn and joins the speed order from the next turn.

### Enemy status order

EnemyStatus display order does not change just because existing backline enemies move forward.

Only the board position changes.

## Implementation implication

The enemy KO processing must move from immediate per-target reserve replacement to per-action defeat collection.

Required flow for ally skills against enemies:

```text
1. Resolve all damage in the skill/action.
2. Collect all enemies that reached HP0 during that skill/action.
3. Remove defeated enemies from the grid.
4. Remove defeated enemies from turn state.
5. Check enemy frontline-zero condition.
6. If enemy frontline is empty and backline enemies exist, move BackTop -> FrontTop and BackBottom -> FrontBottom.
7. Spawn enemy reserves into empty cells in FrontTop -> FrontBottom -> BackTop -> BackBottom order.
8. Mark spawned reserves as acted for the current turn.
9. Preserve action state and TurnNumber of existing enemies that moved forward.
10. Check battle end.
```

This feature should be implemented before adding more complex enemy AI or enemy action preview UI.
