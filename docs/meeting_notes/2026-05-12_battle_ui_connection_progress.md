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

Unconfirmed / pending:

- HP bar の減少確認は未実施。現状はダミー戦闘で被ダメージ処理がまだないため。

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

## Next recommended task

Next task should be small and isolated.

Candidate:

- Implement real turn order number reflection on Status UI.

Expected direction:

- Use `TurnOrderManager` result to set `TurnNumber` in `EnemyStatus_*` / `AllyStatus_*`.
- Turn order numbers should represent mixed enemy/ally speed order from the start of the turn.
- Rotating grid should not reorder status slots.
- Acting state handling can be deferred until later.
