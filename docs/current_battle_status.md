# Current Battle Status

This file records the latest confirmed BattleTest status when `docs/PROJECT_CONTEXT.md` is temporarily behind.

## Confirmed implementation status

- Console has no red errors in the latest checked BattleTest flow.
- Victory condition works.
- Defeat condition works.
- After `Victory / Battle End` or `Defeat / Battle End`, Command UI no longer accepts input.
- Skill MP cost is implemented.
- Skill buttons show MP cost.
- MP shortage is shown with `×` on skill buttons.
- MP-shortage skills remain hoverable so their descriptions can be read.
- Skill hover description shows description, MP cost, and `Not enough MP` when applicable.
- Hovered skill description refreshes after `CommandPanelController.Setup()` without requiring the cursor to leave and re-enter the button.
- Skill use consumes MP.
- MP-shortage skill clicks fail without ending the actor's turn.
- Enemy dummy actions use enemy-specific action data.
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
- Enemy KO is resolved per skill/action instead of immediately per individual target.
- Enemy defeats are collected during a skill, then resolved together.
- Enemy frontline compaction runs before reserve replacement when enemy frontline becomes empty.
- Existing enemy backline units move forward during enemy frontline compaction and preserve action state / TurnNumber.
- Enemy reserves fill empty cells in `FrontTop -> FrontBottom -> BackTop -> BackBottom` order.
- Newly spawned enemy reserves are treated as already acted for that turn.
- EnemyStatus order does not change just because enemies move on the board.
- Enemy board display is mirrored: visual left side is enemy backline, visual right side is enemy frontline.
- Enemy skill target preview highlight is mirrored to match enemy board display.
- Rotate has a temporary formation state.
- During temporary formation after Rotate, Command UI is disabled, but Rotate remains available.
- After `rotationSettleSeconds` currently `0.5`, formation is confirmed.
- If the ally frontline is completely empty and backline allies exist, backline allies slide forward.
- The placement side effect where Rotate + frontline compact can produce placements not directly possible via ordinary Swap is currently allowed.

## Confirmed design notes

- Enemy action preview remains a future candidate, not the immediate implementation priority.
- Enemy preview, if implemented, should initially be limited and should not turn the game into full-information puzzle solving.
- Tracking enemy skills are not prioritized because they complicate UI and may weaken grid-position gameplay.
- Enemy frontline replacement policy is enemy-only for the first implementation. Ally replacement remains KO-position based for now.
- Enemy Rotate / enemy Swap are not planned.

## Workflow rule

- Planning/design may happen in other chats.
- Final confirmation, implementation guidance, repository confirmation, and markdown updates happen in this implementation chat.
- Other chats must not write repository markdown files.
- In this implementation chat, markdown updates may be made without asking each time when they preserve important project state.
