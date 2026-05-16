# Current Battle Status

This file records the latest confirmed BattleTest status when `docs/PROJECT_CONTEXT.md` is temporarily behind.

## Confirmed

- Console has no red errors in the latest checked BattleTest flow.
- Victory condition works.
- Defeat condition works.
- After `Victory / Battle End` or `Defeat / Battle End`, Command UI no longer accepts input.
- Enemy dummy actions now use enemy-specific action data.
- Current enemy action settings:

```text
Goblin A       -> Claw   -> SameGridPosAlly -> 60
Archer         -> Arrow  -> AllyFrontTop    -> 45
Goblin B       -> Bite   -> AllyFrontBottom -> 60
Shaman         -> Hex    -> AllAllies       -> 25
Enemy Reserve  -> Strike -> SameGridPosAlly -> 60
```

- Enemy dummy damage value `60` is intentional for current testing/balance direction.
- Ally KO with reserve available replaces the defeated ally in the same GridPos.
- Ally KO with no reserve clears the ally grid cell.
- AllyStatus keeps `Name KO` and HP0 display for defeated allies with no reserve.
- Enemy KO removes the enemy from the grid and replaces it with enemy reserve if available.
- Rotate has a temporary formation state.
- During temporary formation after Rotate, Command UI is disabled, but Rotate remains available.
- After `rotationSettleSeconds` currently `0.5`, formation is confirmed.
- If the ally frontline is completely empty and backline allies exist, backline allies slide forward.
- The placement side effect where Rotate + frontline compact can produce placements not directly possible via ordinary Swap is currently allowed.

## Workflow rule

- Planning/design may happen in other chats.
- Final confirmation, implementation guidance, repository confirmation, and markdown updates happen in the implementation chat.
- Other chats must not write repository markdown files.
