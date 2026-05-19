# Battle MVP v0.1 Checklist

## Purpose

This checklist defines the minimum confirmation set for closing the current battle-system foundation phase.

All required items were locally confirmed as OK, so the current battle prototype is treated as Battle MVP v0.1 complete.

## Scope

Battle MVP v0.1 includes:

- basic player skill flow
- enemy action flow
- turn order
- ally/enemy 2x2 formation
- Rotate
- Swap
- Item
- Buff/Debuff
- skill cooldown
- temporary Link skill
- turn-scoped LinkCooldown
- victory/defeat result flow
- Return restart flow

Battle MVP v0.1 does not include:

- manual Link partner selection UI
- position-based Link requirements
- character-specific Link compatibility
- partner stat contribution
- advanced Link animation
- damage popup polish
- ScriptableObject/Inspector data migration
- production character art integration

## Required checks

### Compile / Play mode

- [x] Unity Console has no red errors.
- [x] Play mode starts.
- [x] Dummy battle initializes.
- [x] CommandPanel is visible on player command selection.
- [x] EnemyActionPreviewPanel is visible on player command selection.

### Personal skills

- [x] Slash is usable.
- [x] Slash deals damage to the expected target cell.
- [x] Slash does not apply LinkCooldown.
- [x] Pierce is usable.
- [x] Pierce deals damage to the expected target cell.
- [x] Pierce does not apply LinkCooldown.

### Focus / Buff

- [x] Focus targets self.
- [x] Focus does not deal 0 damage to the enemy cell at the same grid position.
- [x] Focus applies AttackUp to the user.
- [x] AttackUp appears in the status text area.
- [x] AttackUp increases later damage.
- [x] Opposite buff/debuff cancellation still works.
- [x] Buff expiration still works.
- [x] Focus applies skill CT.
- [x] Focus shows WAIT while CT remains.
- [x] Focus button dims while CT remains.

### Skill cooldown

- [x] Skill CT is applied after successful skill use when CooldownTurns is greater than 0.
- [x] Skill CT ticks at the acting unit's command entry.
- [x] Skill CT blocks only that skill.
- [x] Skill CT does not block unrelated skills.
- [x] WAIT display disappears when CT reaches 0.

### Link skill / TwinHit

- [x] TwinHit is shown as `[LINK]` in hover description.
- [x] TwinHit hover shows `Partner: Name` when a temporary partner is available.
- [x] TwinHit uses the same partner for hover, action overlay, source flash, and LinkCooldown application.
- [x] TwinHit action overlay shows `User + Partner`.
- [x] TwinHit source flash highlights both user and temporary partner cells.
- [x] TwinHit deals damage to its expected target cells.
- [x] TwinHit applies skill CT.
- [x] TwinHit applies LinkCooldown to the user.
- [x] TwinHit applies LinkCooldown to the temporary partner.
- [x] LinkCooldown appears in the status text area.

### LinkCooldown

- [x] LinkCooldown is not ticked at individual command entry.
- [x] LinkCooldown is cleared at the next turn cycle start.
- [x] A LinkCooldown character cannot be the user of another Link skill in the same turn cycle when that case is reachable.
- [x] A LinkCooldown character cannot be selected as the temporary partner of another Link skill in the same turn cycle.
- [x] LinkCooldown does not prevent Personal skills.
- [x] LinkCooldown does not prevent Item.
- [x] LinkCooldown does not prevent Swap.
- [x] LinkCooldown does not prevent Rotate.
- [x] LinkCooldown does not remove the unit from normal turn order.

### Link partner unavailable states

- [x] If no living ally other than the user exists, Link skill button shows `NO PARTNER`.
- [x] If no living ally other than the user exists, Link skill hover shows `No available link partner.`
- [x] If living allies exist but all are LinkCooldown, Link skill button shows `NO READY PARTNER`.
- [x] If living allies exist but all are LinkCooldown, Link skill hover shows `No ready link partner.`
- [x] Partner-unavailable Link buttons are dimmed.
- [x] Clicking a partner-unavailable Link skill does not end the action.

### Item

- [x] Potion is usable while count is greater than 0.
- [x] Potion heals the expected ally target.
- [x] Potion count decreases.
- [x] Potion slot becomes `-` when count reaches 0.
- [x] Item flow still uses ResolvingAction delay.

### Swap / Rotate

- [x] Swap still works.
- [x] Swap panel clears skill description and target preview.
- [x] Rotate still works.
- [x] Rotate refreshes enemy action preview and highlights.
- [x] Rotate does not break LinkCooldown display.

### Enemy action preview / enemy action

- [x] EnemyActionPreviewPanel shows selected enemy actions.
- [x] `NEXT >` marks the next unacted enemy.
- [x] Only the `NEXT >` enemy target is highlighted.
- [x] Enemy preview hides during ResolvingAction.
- [x] Enemy action flash still appears during enemy actions.
- [x] Consecutive enemy actions are shown one by one.

### Victory / Defeat / Return

- [x] Victory result appears when all enemies are defeated.
- [x] Defeat result appears when all allies are defeated.
- [x] Return button restarts the dummy battle.
- [x] Return resets HP.
- [x] Return resets buffs.
- [x] Return resets Potion count.
- [x] Return resets skill CT.
- [x] Return resets LinkCooldown.
- [x] Return restores enemy state.
- [x] Skill, Swap, Item, and Rotate are usable after Return.

## Close condition

Battle MVP v0.1 is complete.

All required items above were locally confirmed as OK.

## Next recommended phase

The next recommended phase is data migration:

- move temporary skill setup away from hardcoded `AddDefaultSkills()`
- prepare ScriptableObject or Inspector-driven skill/character data
- keep current battle logic stable while data ownership changes
