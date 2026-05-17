# Battle MVP v0.1 Checklist

## Purpose

This checklist defines the minimum confirmation set for closing the current battle-system foundation phase.

If every required item passes, the current battle prototype can be treated as Battle MVP v0.1 complete.

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

- [ ] Unity Console has no red errors.
- [ ] Play mode starts.
- [ ] Dummy battle initializes.
- [ ] CommandPanel is visible on player command selection.
- [ ] EnemyActionPreviewPanel is visible on player command selection.

### Personal skills

- [ ] Slash is usable.
- [ ] Slash deals damage to the expected target cell.
- [ ] Slash does not apply LinkCooldown.
- [ ] Pierce is usable.
- [ ] Pierce deals damage to the expected target cell.
- [ ] Pierce does not apply LinkCooldown.

### Focus / Buff

- [ ] Focus targets self.
- [ ] Focus does not deal 0 damage to the enemy cell at the same grid position.
- [ ] Focus applies AttackUp to the user.
- [ ] AttackUp appears in the status text area.
- [ ] AttackUp increases later damage.
- [ ] Opposite buff/debuff cancellation still works.
- [ ] Buff expiration still works.
- [ ] Focus applies skill CT.
- [ ] Focus shows WAIT while CT remains.
- [ ] Focus button dims while CT remains.

### Skill cooldown

- [ ] Skill CT is applied after successful skill use when CooldownTurns is greater than 0.
- [ ] Skill CT ticks at the acting unit's command entry.
- [ ] Skill CT blocks only that skill.
- [ ] Skill CT does not block unrelated skills.
- [ ] WAIT display disappears when CT reaches 0.

### Link skill / TwinHit

- [ ] TwinHit is shown as `[LINK]` in hover description.
- [ ] TwinHit hover shows `Partner: Name` when a temporary partner is available.
- [ ] TwinHit uses the same partner for hover, action overlay, source flash, and LinkCooldown application.
- [ ] TwinHit action overlay shows `User + Partner`.
- [ ] TwinHit source flash highlights both user and temporary partner cells.
- [ ] TwinHit deals damage to its expected target cells.
- [ ] TwinHit applies skill CT.
- [ ] TwinHit applies LinkCooldown to the user.
- [ ] TwinHit applies LinkCooldown to the temporary partner.
- [ ] LinkCooldown appears in the status text area.

### LinkCooldown

- [ ] LinkCooldown is not ticked at individual command entry.
- [ ] LinkCooldown is cleared at the next turn cycle start.
- [ ] A LinkCooldown character cannot be the user of another Link skill in the same turn cycle when that case is reachable.
- [ ] A LinkCooldown character cannot be selected as the temporary partner of another Link skill in the same turn cycle.
- [ ] LinkCooldown does not prevent Personal skills.
- [ ] LinkCooldown does not prevent Item.
- [ ] LinkCooldown does not prevent Swap.
- [ ] LinkCooldown does not prevent Rotate.
- [ ] LinkCooldown does not remove the unit from normal turn order.

### Link partner unavailable states

- [ ] If no living ally other than the user exists, Link skill button shows `NO PARTNER`.
- [ ] If no living ally other than the user exists, Link skill hover shows `No available link partner.`
- [ ] If living allies exist but all are LinkCooldown, Link skill button shows `NO READY PARTNER`.
- [ ] If living allies exist but all are LinkCooldown, Link skill hover shows `No ready link partner.`
- [ ] Partner-unavailable Link buttons are dimmed.
- [ ] Clicking a partner-unavailable Link skill does not end the action.

### Item

- [ ] Potion is usable while count is greater than 0.
- [ ] Potion heals the expected ally target.
- [ ] Potion count decreases.
- [ ] Potion slot becomes `-` when count reaches 0.
- [ ] Item flow still uses ResolvingAction delay.

### Swap / Rotate

- [ ] Swap still works.
- [ ] Swap panel clears skill description and target preview.
- [ ] Rotate still works.
- [ ] Rotate refreshes enemy action preview and highlights.
- [ ] Rotate does not break LinkCooldown display.

### Enemy action preview / enemy action

- [ ] EnemyActionPreviewPanel shows selected enemy actions.
- [ ] `NEXT >` marks the next unacted enemy.
- [ ] Only the `NEXT >` enemy target is highlighted.
- [ ] Enemy preview hides during ResolvingAction.
- [ ] Enemy action flash still appears during enemy actions.
- [ ] Consecutive enemy actions are shown one by one.

### Victory / Defeat / Return

- [ ] Victory result appears when all enemies are defeated.
- [ ] Defeat result appears when all allies are defeated.
- [ ] Return button restarts the dummy battle.
- [ ] Return resets HP.
- [ ] Return resets buffs.
- [ ] Return resets Potion count.
- [ ] Return resets skill CT.
- [ ] Return resets LinkCooldown.
- [ ] Return restores enemy state.
- [ ] Skill, Swap, Item, and Rotate are usable after Return.

## Close condition

Battle MVP v0.1 can be considered complete when:

- every required item above is confirmed, or
- any unconfirmed item is explicitly moved to a later phase with a reason.

After closing this checklist, the next recommended phase is data migration:

- move temporary skill setup away from hardcoded `AddDefaultSkills()`
- prepare ScriptableObject or Inspector-driven skill/character data
- keep current battle logic stable while data ownership changes
