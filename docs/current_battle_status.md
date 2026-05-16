# Current Battle Status

## Latest confirmed implementation notes

- SkillData has MpCost, Damage, EffectType, BuffType, and BuffTurns.
- Skill damage is routed through BattleUIManager.CalculateDamage().
- CalculateDamage applies AttackUp, AttackDown, DefenseUp, and DefenseDown modifiers.
- BattleUIManager has buff helper methods for apply, refresh, cancel, find, and turn ticking.
- Buff ticking is called from StartNextTurn() before rebuilding turn order.
- Editor-only debug hotkeys exist for checking active ally buffs: 1 AttackUp, 2 AttackDown, 3 DefenseUp, 4 DefenseDown.
- SkillEffectType.ApplyBuff is processed after skill damage.
- SkillTargetPattern.Self exists.
- Skill4 is currently Focus instead of Wave.
- Focus costs 6 MP, does 0 damage, targets Self, and applies AttackUp for 2 turns.
- Focus behavior has been tested and confirmed: buff application, damage increase, opposite-buff cancellation, and expiry behave as expected.
- Focus does not deal 0 damage to the enemy cell at the same grid position.
- Buff UI is implemented as text in ally/enemy status slots.
- Skill descriptions show effect text for ApplyBuff skills.
- ItemData stores HealAmount and Count.
- Dummy Potion starts at count 3 and disappears from the item slot as '-' when count reaches 0.
- Play start clears default skill description text.
- Swap and Item panels clear skill description and target preview.

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
- GetSkillAnimationTargetPositions() is used for action flash cells.
- Self-target skills are effect/animation targets, not enemy damage targets.
- Current ApplyBuff skills still resolve to the active user as the effect target unless more detailed ally/enemy effect targeting is added later.

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
  - HP, MP, buffs, Potion count, and enemy state return to their initial dummy battle state.
  - Skill, Swap, Item, and Rotate are usable after restart.

## Stable reference

See the existing design notes in:

docs/implementation_policy.md
docs/enemy_action_preview_policy.md
docs/enemy_action_preview_confirmed_notes.md
docs/enemy_frontline_replacement_policy.md
docs/enemy_frontline_replacement_confirmed_notes.md
