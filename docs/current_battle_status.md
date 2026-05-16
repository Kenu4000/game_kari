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
- ResolvingAction:
  - CommandPanel is hidden.
  - TopActionPanel is visible.
  - SkillName/UserName, ItemName/UserName, or enemy action/user text is shown.
  - BossNamePlate is hidden.
  - ResultPanel is hidden.
- BattleEnded:
  - CommandPanel is hidden.
  - TopActionPanel is hidden.
  - BossNamePlate is hidden.
  - ResultPanel is visible.
  - Victory/Defeat and Battle End are shown.
  - ReturnButton is visible.
- Player Skill and Item actions use actionResolveDelaySeconds before advancing to the next actor.
- Enemy actions also use ResolvingAction and actionResolveDelaySeconds, so consecutive enemy actions are shown one by one.
- Overlay visibility is currently controlled by scene-object name lookup for TopActionPanel and BossNamePlate.

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
