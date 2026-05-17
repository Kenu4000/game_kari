# Link Skill Policy

## Purpose

This document defines the current Link skill behavior for the battle prototype.

The current implementation is still a prototype-level Link system. It supports one temporary Link skill, automatic temporary partner selection, LinkCooldown, and UI feedback. It does not yet support manual partner selection, character-specific Link pairs, position requirements, or advanced Link animation.

## Current Link skill

- `TwinHit` is the temporary Link skill used for testing.
- `TwinHit` uses `SkillKind.Link`.
- `TwinHit` currently has `CooldownTurns = 2`.
- `TwinHit` currently has `LinkCooldownTurns = 1`.
- `Pierce` has been restored to a Personal damage skill.
- `Slash` and `Pierce` are Personal damage skills.
- `Focus` is a Personal self-buff skill.

## LinkCooldown definition

LinkCooldown is a turn-scoped "already participated in a Link skill" state.

When a Link skill is successfully used:

- The Link skill user receives `LinkCooldownRemaining`.
- The selected temporary Link partner receives `LinkCooldownRemaining`.

A character with LinkCooldown cannot participate in another Link skill during the same turn cycle.

This means a LinkCooldown character cannot be:

- the Link skill user
- the Link skill partner

However, a LinkCooldown character can still:

- use Personal skills
- use Items
- be swapped
- be moved by Rotate
- act normally when their turn comes

LinkCooldown is cleared at the start of the next turn cycle by `ClearAllLinkCooldowns()` from `StartNextTurn()`.

LinkCooldown is not ticked at individual command entry.

## Skill cooldown and LinkCooldown separation

Skill cooldown and LinkCooldown solve different problems.

Skill cooldown:

- belongs to the skill itself
- prevents repeated use of the same skill across future turns
- is represented by `CooldownTurns`
- is displayed as `WAIT:N`

LinkCooldown:

- belongs to the character
- prevents the same character from repeatedly participating in Link skills within the same turn cycle
- is represented by `LinkCooldownRemaining`
- is displayed in the status text area as `LinkCooldown N`

The same Link skill repeated across turns should be limited by skill CT, not by LinkCooldown.

## Temporary partner selection

The current system uses automatic temporary partner selection.

The temporary partner is selected by `LinkPartnerPolicy.FindFirstAvailablePartner()`.

The current rule is:

1. The partner must be a living ally.
2. The partner must not be the user.
3. The partner must not have `LinkCooldownRemaining > 0`.
4. The first ally matching those conditions is selected.

There is no manual selection UI yet.

There is no position requirement yet.

There is no character-specific Link compatibility yet.

## Partner availability states

The UI separates partner-unavailable reasons.

### NO PARTNER

Used when no living partner exists.

Condition:

- there is no living ally other than the user

Button text:

- `NO PARTNER`

Hover text:

- `No available link partner.`

### NO READY PARTNER

Used when living partners exist, but all possible partners are currently LinkCooldown.

Condition:

- there is at least one living ally other than the user
- every such ally has `LinkCooldownRemaining > 0`

Button text:

- `NO READY PARTNER`

Hover text:

- `No ready link partner.`

## Link skill UI

For Link skills, the hover description shows:

- `[LINK]`
- `Damage: N`
- `Partner: Name` when a temporary partner is available
- cooldown or unavailable-reason text when blocked

When a Link skill is used, the action overlay user text displays:

- `User + Partner`

The source flash highlights:

- the user cell
- the temporary partner cell

The selected temporary partner used for hover/action presentation is the same partner passed to cooldown application.

## Current implementation locations

Core policy:

- `Assets/Scripts/Battle/LinkPartnerPolicy.cs`

Battle logic:

- `Assets/Scripts/Battle/BattleUIManager.cs`

Command UI:

- `Assets/Scripts/Battle/CommandPanelController.cs`

Relevant helpers include:

- `LinkPartnerPolicy.HasLivingPartnerCandidate()`
- `LinkPartnerPolicy.FindFirstAvailablePartner()`
- `LinkPartnerPolicy.HasAvailablePartner()`
- `LinkPartnerPolicy.BuildUnavailableReason()`
- `BattleUIManager.GetLinkPartnerForSkill()`
- `BattleUIManager.ApplySkillCooldownAfterUse()`
- `CommandPanelController.GetTemporaryLinkPartner()`
- `CommandPanelController.BuildSkillLinkPartnerDescription()`
- `CommandPanelController.BuildLinkPartnerUnavailableText()`

## Not implemented yet

The following are intentionally not part of the current MVP:

- manual Link partner selection UI
- position-based Link requirements
- character-specific Link compatibility
- partner attack stat contribution
- partner being marked as acted
- partner-side skill CT
- dedicated Link cut-in or animation
- enemy-side Link skills

## MVP confirmation checklist

Before closing the current battle MVP phase, confirm:

- Console has no red errors.
- Play mode starts.
- Slash works as a Personal damage skill.
- Pierce works as a Personal damage skill.
- Focus applies AttackUp to self and uses CT.
- TwinHit displays `[LINK]` on hover.
- TwinHit displays `Partner: Name` when a temporary partner is available.
- TwinHit action overlay shows `User + Partner`.
- TwinHit flashes both the user and temporary partner cells.
- TwinHit gives LinkCooldown to both the user and temporary partner.
- A LinkCooldown character cannot be the user of another Link skill in the same turn cycle.
- A LinkCooldown character cannot be selected as the temporary partner of another Link skill in the same turn cycle.
- LinkCooldown is cleared at the next turn cycle start.
- Skill CT still remains independent from LinkCooldown.
- NO PARTNER appears when no living partner exists.
- NO READY PARTNER appears when living partners exist but all are LinkCooldown.
- Item, Swap, Rotate, Victory, Defeat, and Return still work.
