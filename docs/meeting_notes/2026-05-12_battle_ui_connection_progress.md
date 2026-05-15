# Battle UI Connection Progress

## Date

2026-05-12

## Summary

Battle UI の自動生成・固定ボタン方式・ダミー戦闘データ接続を進め、`BattleTest` Scene 上で基本操作が通る状態まで確認した。

## Main outcomes

- GitHub Desktop に local の Unity project folder を認識させた。
- Git LFS を初期化した。
- Unity 用 `.gitignore` を追加した。
- `TextMeshPro` / `Unity UI` 関連依存を整理し、Console の `TMPro` / `UnityEngine.UI` / `EventSystems` 系エラーを解消した。
- TMP Essential Resources と Unity の `.meta` 類を Git 管理対象として整理した。
- `BattleTest` Scene を `Assets/Scenes/BattleTest.unity` に保存した。
- `CommandPanelController` を固定ボタン方式へ移行した。
- `BattleUIManager` を Scene に追加し、ダミー戦闘データと UI を接続した。

## Command UI status

`CommandPanelController` は、動的生成ではなく Scene 上の固定ボタンを Inspector 参照で扱う方式に変更した。

Confirmed behavior:

- Play 開始時から `SkillListPanel` を表示する。
- `Fight` で `SkillListPanel` を表示する。
- `Swap` で `SwapListPanel` を表示する。
- `Item` で `ItemListPanel` を表示する。
- 右クリックで `SkillListPanel` に戻る。
- `MainCommandButtons` は常時表示する。

Notes:

- 以前の `ShowMain()` は互換用に残し、内部では `ShowSkills()` を呼ぶ扱いにした。
- `simpleButtonPrefab` / `skillListRoot` / `sideListRoot` に依存する動的生成方式は整理した。
- `CommandViewMode` / `_mode` は未使用 warning の原因だったため削除した。

## Dummy battle behavior confirmed

`BattleUIManager` の dummy battle で以下を確認した。

- `Skill 1〜4` が `Slash / Pierce / TwinHit / Wave` に反映される。
- Skill button click で Console に action log が出る。
- `RotateButton` で味方盤面が回転する。
- `Item` は前方味方がいない場合に `No forward ally target` を出し、条件が合うと `Potion` 使用ログが出る。
- `Swap` で `Reserve` が表示される。
- `Reserve` click で active unit と reserve が交代する。
- Swap 中に Rotate しても、表示中の `SwapListPanel` が維持される。

## Status UI status

`BattleUIManager` に `EnemyStatusPanel` / `AllyStatusPanel` 参照を追加し、Status UI に dummy battle data を反映する処理を追加した。

Confirmed behavior:

- Enemy status names become:
  - `Goblin A`
  - `Archer`
  - `Goblin B`
  - `Shaman`
- Ally status names become:
  - `Knight`
  - `Mage`
  - `Cleric`
  - `Rogue`
- Rotate しても Status 欄の並び順は変化しない。
- Swap 後、出撃中 status は `Knight` から `Reserve` に更新される。
- Swap list 側には控えに戻った `Knight` が表示される。

## Turn order and active unit behavior

Turn number display was connected to the actual mixed ally/enemy turn order.

Confirmed behavior:

- Initial `TurnNumber` values are based on speed order, not fixed UI slot order.
- Turn order is calculated at turn start and remains fixed during the turn.
- Rotate does not recalculate turn numbers.
- Swap does not recalculate turn numbers.
- On swap, the reserve inherits the previous active unit's turn number.
- Skill use and successful item use mark the active unit as acted.
- Acted units have their `TurnNumber` hidden.
- Item failure does not mark the unit as acted.
- After an ally action, the system advances through the mixed turn order.
- Enemy turns are processed automatically as dummy enemy actions.
- When all units have acted, a new turn starts.

## Active unit highlight

Current active unit highlighting was added.

Confirmed behavior:

- The active ally board cell is highlighted.
- The active ally status slot is highlighted.
- The highlight moves after skill use when the next active ally is selected.
- The highlight follows the active unit after swap.
- The highlight follows the active unit after rotate.
- Item failure does not change the active highlight.

## Skill damage and enemy KO behavior

Dummy skill damage was connected to enemy HP.

Confirmed behavior:

- `Slash` damages Enemy FrontTop.
- `Pierce` damages Enemy FrontBottom.
- `TwinHit` damages Enemy FrontTop and Enemy FrontBottom.
- `Wave` damages all four enemy grid positions.
- Enemy HP bars update after damage.
- When enemy HP reaches 0, the enemy is marked dead.
- KO enemies are removed from the enemy grid using `SetUnit(false, pos, null)`.
- Attacking an empty enemy grid position logs a miss.
- KO enemies do not receive turn numbers.
- KO enemies do not perform dummy enemy actions.

## Enemy status list behavior

Enemy status UI was changed from fixed original slots to a live enemy list.

Confirmed behavior:

- Enemy status slots show only alive enemies.
- KO enemies disappear from the status list.
- Alive enemies are packed upward in the EnemyStatusPanel.
- Empty enemy status slots are hidden.
- EnemyStatusPanel resizes based on the visible enemy count.
- EnemyStatusPanel is hidden when there are no visible enemies.

Design note:

- This prepares for a future enemy reserve system. Later, when a 5th or later enemy exists, KO can free a grid cell and a reserve enemy can enter that cell.

## Skill target preview

Skill target preview was changed to board-cell highlighting.

Confirmed behavior:

- Hovering `Slash` highlights Enemy FrontTop.
- Hovering `Pierce` highlights Enemy FrontBottom.
- Hovering `TwinHit` highlights Enemy FrontTop and Enemy FrontBottom.
- Hovering `Wave` highlights all enemy cells.
- Empty target cells still highlight, so the player can see where the attack will land even if it will miss.
- Target preview is cleared when the cursor leaves the skill button.
- Target preview is re-applied after actor change if the cursor remains on the same skill button.

## Enemy dummy action behavior

Enemy dummy actions now deal damage to allies.

Confirmed behavior:

- Enemy dummy action occurs at the correct speed-order position.
- The enemy action damages the current active ally by 10.
- Ally HP bars update after dummy enemy damage.
- If the current active ally is dead, the enemy targets the first alive ally.
- Ally HP reaching 0 marks that ally as dead.
- Dead allies are skipped by next-active selection.

## Ally KO and reserve replacement behavior

Ally KO handling now supports automatic reserve replacement.

Confirmed behavior:

- When enemy dummy damage reduces an ally to 0 HP, `HandleAllyDefeated()` is called.
- KO ally is marked dead.
- If an alive reserve exists, it is placed into the defeated ally's same `GridPos`.
- `_allies` is updated from the defeated ally to the replacement reserve.
- The replacement reserve is removed from `_reserves`.
- The defeated ally's turn number and acted state are cleared by `RemoveTurnState()`.
- The replacement is added to `_actedUnits`, so it cannot act during the turn it enters.
- The replacement has no `TurnNumber` during the entry turn.
- On the next turn, the replacement joins the speed-based turn order normally.
- EnemyStatusPanel anchor/pivot is no longer overwritten from code during resize; Scene-side RectTransform settings are preserved.
- Damage test value was restored to `const int damage = 10`.

Design decision:

- KO replacement does not inherit action rights. This preserves the value of defeating an unacted unit before it can act.
- This rule should later be applied symmetrically to enemy reserve replacement as well.

Known limitation:

- Ally KO without reserve keeps the KO unit in the ally side state for now.
- Ally status UI does not yet have a dedicated KO visual style beyond current HP / turn-number behavior.
- Enemy action targeting is still dummy behavior, not proper AI targeting.

## BattleUICreator status

`BattleUICreator.cs` の Canvas 検索は Unity 6000.4.6f1 の warning 対応として以下に変更した。

```csharp
Canvas canvas = Object.FindAnyObjectByType<Canvas>();
```

`TurnOrderBar` は現行仕様では復活させない。

## Git / Codex operation notes

Codex は今回の作業で以下の問題を繰り返した。

- 古い branch 差分を引きずる。
- `BattleUICreator.cs` を勝手に混ぜる。
- `TurnOrderBar` を復活させる。
- 同一変数の二重宣言など、コンパイル不能なコードを出す。
- 既存 API を削除する。

当面の方針:

- Unity Scene / Inspector 参照 / Battle UI 接続作業では Codex を使わない。
- 変更対象ファイルを1つに絞る。
- ChatGPT 側で修正版コードまたは差分を作る。
- 手元で手動反映する。
- Unity で Console / Play 確認する。
- GitHub Desktop で変更ファイルを確認してから push する。
- ChatGPT から GitHub へ直接書き込むのは、docs など Scene 参照に影響しないファイルに限定する。

## Current confirmed state

- Console red errors: none.
- Command UI switching: OK.
- Fixed command buttons: OK.
- Dummy skill actions: OK.
- Dummy swap action: OK.
- Dummy item action: OK.
- Rotate action: OK.
- Swap panel persistence during rotate: OK.
- Status names: OK.
- Status update after swap: OK.
- Turn numbers based on speed order: OK.
- Turn numbers remain fixed during a turn: OK.
- Active unit progression through turn order: OK.
- Enemy dummy actions in turn order: OK.
- Skill damage to enemies: OK.
- Enemy KO removal from grid: OK.
- Enemy status list packing and resizing: OK.
- Skill target cell preview: OK.
- Enemy dummy damage to allies: OK.
- Ally KO automatic reserve replacement: OK.
- Replacement reserve cannot act on the turn it enters: OK.
- Replacement reserve joins speed order from the next turn: OK.

## Next recommended task

Next task should be small and isolated.

Candidate:

- Implement enemy reserve replacement using the same no-action-inheritance rule.

Expected direction:

- Add enemy reserves separately from active enemies.
- When an enemy reaches 0 HP, remove it from the grid.
- If an enemy reserve exists, place it into the defeated enemy's same grid position.
- Do not inherit the defeated enemy's action right.
- The replacement enemy should join speed-based turn order from the next turn.
- Keep this separate from full enemy AI targeting.
