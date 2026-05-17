# Current Battle Status

## Latest confirmed implementation notes

- SkillData has Damage, EffectType, EffectTarget, BuffType, and BuffTurns.
- SkillData.MpCost has been removed.
- BattleUnit.CurrentMP has been removed.
- CharacterData.MaxMP has been removed.
- MP behavior, MP data fields, and MP bar UI objects have been removed from the active battle prototype.
- Do not add new MP-based mechanics.
- SkillData also has SkillKind, CooldownTurns, and LinkCooldownTurns for skill cooldown and link-skill restrictions.
- SkillKind exists with Personal and Link.
- SkillEffectTargetType exists with Self, Target, AllAllies, and AllEnemies.
- Skill damage is routed through BattleUIManager.CalculateDamage().
- CalculateDamage applies AttackUp, AttackDown, DefenseUp, and DefenseDown modifiers.
- BattleUIManager has buff helper methods for apply, refresh, cancel, find, and turn ticking.
- Buff ticking is called from StartNextTurn() before rebuilding turn order.
- Editor-only debug hotkeys exist for checking active ally buffs: 1 AttackUp, 2 AttackDown, 3 DefenseUp, 4 DefenseDown.
- SkillEffectType.ApplyBuff is processed after skill damage.
- SkillTargetPattern.Self exists.
- Skill4 is currently Focus instead of Wave.
- Focus does 0 damage, targets Self, has EffectTarget Self, applies AttackUp for 2 turns, and has CooldownTurns 2.
- Focus behavior has been tested and confirmed: buff application, damage increase, opposite-buff cancellation, expiry, CT assignment, CT ticking, CT blocking, command UI CT display, and CT button dimming behave as expected.
- TwinHit is currently a temporary Link skill for testing: SkillKind.Link, CooldownTurns 2, LinkCooldownTurns 1.
- Focus does not deal 0 damage to the enemy cell at the same grid position.
- Buff UI is implemented as text in ally/enemy status slots.
- Skill descriptions show effect text for ApplyBuff skills.
- LinkCooldownRemaining is displayed in the same status text area as buffs/debuffs.
- ItemData stores HealAmount and Count.
- Dummy Potion starts at count 3 and disappears from the item slot as '-' when count reaches 0.
- Play start clears default skill description text.
- Swap and Item panels clear skill description and target preview.

## Skill resource / cooldown status

- MP has been removed from active battle logic, data fields, and visible status UI.
- Skill use does not check MP.
- Skill use does not consume MP.
- Skill use logs do not show MP values.
- CommandPanelController does not show MP cost in skill button labels.
- CommandPanelController does not show MP Cost or Not enough MP in skill hover descriptions.
- Swap button labels do not show MP.
- Ally status slots no longer update MP bars, and MPBar scene objects have been removed.
- Code search has no remaining CurrentMP / MaxMP / MpCost / MPBar hits after the MP bar removal check.
- CooldownTurns is the per-skill cooldown model.
- LinkCooldownTurns defines the temporary turn-scoped link participation lock duration, currently intended as 1 for testing and production.
- SkillKind.Personal is for ordinary individual skills.
- SkillKind.Link is reserved for link/combination skills.
- CooldownTurns is consumed after successful skill use.
- LinkCooldownTurns is applied after successful Link skill use.
- BattleUnit has SkillCooldowns for per-skill current CT state.
- BattleUnit has LinkCooldownRemaining for turn-scoped link participation state.
- LinkCooldown means the character has already participated in a Link skill during the current turn cycle.
- LinkCooldown characters cannot be Link skill users or Link partners during that same turn cycle.
- LinkCooldown characters can still use Personal skills, use Items, be swapped, be moved by Rotate, and take their normal action order.
- LinkCooldown is no longer ticked at individual command entry.
- StartNextTurn() clears LinkCooldown from allies, reserves, enemies, and enemy reserves through ClearAllLinkCooldowns().
- Skill CT still ticks at the acting unit's command entry through TickSkillCooldownsAtTurnStart().
- Successful Link skill use currently applies LinkCooldown to both the user and an automatically selected temporary partner.
- The temporary partner is the first living ally other than the user whose LinkCooldownRemaining is 0.
- BattleUIManager has helper methods for reading and writing skill cooldown, LinkCooldown, and Link partner availability state.
- CommandPanelController reads BattleUnit.SkillCooldowns, LinkCooldownRemaining, and the ally list for skill button and description display.
- BattleUIManager passes _allies into commandPanel.Setup(_active, _reserves, _allies).
- Current BattleUIManager helpers include GetSkillCooldownRemaining(), SetSkillCooldownRemaining(), FindSkillCooldownState(), GetSkillCooldownKey(), GetLinkCooldownRemaining(), SetLinkCooldownRemaining(), IsLinkSkillBlocked(), ClearAllLinkCooldowns(), TickSkillCooldownsAtTurnStart(), TickSkillCooldowns(), CanUseSkillWithCooldowns(), FindAvailableLinkPartner(), HasAvailableLinkPartner(), and ApplySkillCooldownAfterUse().
- TickLinkCooldown() may still exist as dead code and should be removed later if no remaining references exist.
- Current CommandPanelController helpers include BuildSkillButtonLabel(), BuildSkillCooldownDescription(), GetSkillCooldownRemaining(), FindSkillCooldownState(), GetSkillCooldownKey(), GetLinkCooldownRemaining(), IsLinkSkillBlocked(), HasAvailableLinkPartner(), ApplySkillButtonVisualState(), ResetButtonVisualState(), and SetButtonAlpha().
- Skills with remaining CT are blocked in HandleSkillClicked().
- Link skills are blocked in HandleSkillClicked() while the user has LinkCooldownRemaining.
- Link skills are also blocked when there is no living, non-LinkCooldown ally available as a temporary link partner.
- Skill buttons show WAIT / LINK / NO PARTNER state text when CT, LinkCooldown, or missing partner state is active.
- Skill hover descriptions show cooldown / LinkCooldown / missing partner status text.
- CT, LinkCooldown, or NO PARTNER unavailable skill buttons are dimmed by applying alpha 0.45 to the button Graphic and TMP label.
- Skill buttons remain interactable during CT/LinkCooldown/NO PARTNER so clicks are still routed to BattleUIManager and blocked by logic.
- LinkCooldownRemaining appears in the status text area as `LinkCooldown N`, making it visible which character is under LinkCooldown.
- CT blocking currently logs the reason to Console and command UI shows state text/dimming, but there is no WAIT icon or dedicated unavailable-reason UI yet.
- Next implementation step should remove dead TickLinkCooldown() if unused, then continue toward real Link partner selection or Link action presentation.

## Battle phase and UI status

- BattlePhase has CommandSelect, ResolvingAction, and BattleEnded.
- CommandSelect:
  - CommandPanel is visible and usable.
  - TopActionPanel is hidden.
  - BossNamePlate is hidden.
  - ResultPanel is hidden.
  - EnemyActionPreviewPanel is visible.
- ResolvingAction:
  - CommandPanel is hidden.
  - TopActionPanel is visible.
  - SkillName/UserName, ItemName/UserName, or enemy action/user text is shown.
  - BossNamePlate is hidden.
  - ResultPanel is hidden.
  - EnemyActionPreviewPanel is hidden.
- BattleEnded:
  - CommandPanel is hidden.
  - TopActionPanel is hidden.
  - BossNamePlate is hidden.
  - ResultPanel is visible.
  - Victory/Defeat and Battle End are shown.
  - ReturnButton is visible.
  - EnemyActionPreviewPanel is hidden.
- Player Skill and Item actions use actionResolveDelaySeconds before advancing to the next actor.
- Enemy actions also use ResolvingAction and actionResolveDelaySeconds, so consecutive enemy actions are shown one by one.
- Overlay visibility is currently controlled by scene-object name lookup for TopActionPanel and BossNamePlate.

## Skill target separation status

- Skill target handling is now separated into damage targets, effect targets, and animation targets.
- GetSkillDamageTargetPositions() is used for enemy damage cells only.
- GetSkillEffectTargets() is used for buff/effect recipient units.
- GetSkillEffectTargets() now reads SkillData.EffectTarget.
- EffectTarget Self is implemented and returns the active user.
- EffectTarget Target, AllAllies, and AllEnemies are reserved for future effect targeting.
- GetSkillAnimationTargetPositions() is used for action flash cells.
- Self-target skills are effect/animation targets, not enemy damage targets.
- Current non-Self effect targeting is reserved and intentionally not active yet.

## Action animation status

- ResolvingAction now has a minimal source-and-target flash animation.
- The flash uses pending action source/target positions stored before the action resolve delay.
- Skill actions flash the acting ally cell in cyan and affected enemy board cells in white.
- Self-target skills flash the acting ally cell.
- Item actions flash the acting ally cell in cyan and healed ally cell in white.
- Enemy actions flash the acting enemy cell in cyan and actually targeted ally board cells in white.
- The flash uses actionResolveDelaySeconds as the total animation duration.
- actionFlashCount controls the number of blink cycles; the current default is 3.
- Enemy action preview highlights and action flash highlights are separate systems.
- EnemyActionPreviewPanel and red preview highlights hide during ResolvingAction; only the action flash is shown during action resolution.
- This is a temporary animation layer; no character movement, damage popup, Animator Controller, or external tween library is involved yet.

## Enemy action preview status

- Enemy actions are selected and cached separately from execution.
- CommandSelect prepares selected enemy actions for preview.
- EnemyActionPreviewPanel is created automatically under Canvas if it does not already exist.
- EnemyActionPreviewPanel shows unacted enemies as enemy name, action name, and target summary.
- The next unacted enemy line is marked with `NEXT >`.
- The board highlight shows only the `NEXT >` enemy's target, not every enemy target at once.
- Enemy action preview highlights are red-tinted ally board cells.
- Enemy action preview highlights are redrawn at the end of RedrawBoard(), so they appear without requiring Rotate.
- Skill hover still uses the enemy board target preview separately.
- Enemy action preview panel and highlights hide during ResolvingAction and BattleEnded.
- Rotate refreshes enemy action preview text and highlights.

## Turn order display status

- Turn order numbers are shown in status slots.
- Acted units hide their own displayed turn number.
- Acted units still keep their original position for numbering later units.
- Dead units are excluded from displayed turn-number counting.
- This prevents a display number larger than the current visible battle participant count after KO.
- Reserve replacements that cannot act this turn do not show a turn number.

## Result panel status

- ResultPanel is created automatically under Canvas if it does not already exist.
- ResultPanel contains ResultTitle, ResultSubText, and ReturnButton.
- ReturnButton currently restarts the dummy battle.
- Confirmed Return flow:
  - ResultPanel hides.
  - CommandPanel returns.
  - The dummy battle restarts from the first ally turn.
  - HP, buffs, Potion count, cooldown state, and enemy state return to their initial dummy battle state.
  - Skill, Swap, Item, and Rotate are usable after restart.

## Stable reference

See the existing design notes in:

docs/implementation_policy.md
docs/enemy_action_preview_policy.md
docs/enemy_action_preview_confirmed_notes.md
docs/enemy_frontline_replacement_policy.md
docs/enemy_frontline_replacement_confirmed_notes.md
