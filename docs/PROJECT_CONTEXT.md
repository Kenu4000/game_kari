# game_kari Project Context

## 1. Project overview

Unityで制作中の戦闘特化型RPG。マップ探索は作らず、以下のループを基本にする。

```text
拠点 → クエスト選択 → 会話イベント → 複数戦闘 → リザルト → 拠点
```

主軸は、戦闘UIと4vs4の陣形型コマンドバトル。

現時点では、ゲーム全体の完成よりも `BattleTest` Scene 上で戦闘UI・行動順・交代・KO・控え補充・勝敗判定の基礎を固める段階。

## 2. Repository / workflow

Repository:

```text
https://github.com/Kenu4000/game_kari
```

Unityで開く本体フォルダは、GitHubからcloneした `game_kari` フォルダに統一する。

通常作業の流れ:

```text
Fetch origin
必要なら Pull origin
Unity / VS Codeで作業
UnityでConsole赤エラーとPlay動作確認
GitHub Desktopで変更ファイル確認
commit
push
```

重要:

- 壊れた状態を確認目的でpushしない。
- Scene参照やInspector参照を含む作業は、Unity上で動作確認してからcommitする。
- GitHub上の安定版に戻したい場合は、GitHub Desktopで該当ファイルをDiscardし、必要ならFetch/Pullする。
- ChatGPTからGitHubへ直接書き込むのは、原則として `docs` などScene参照に影響しないファイルに限定する。
- `BattleUIManager.cs` の大きな全文置換は避ける。必要な場合はメソッド単位で手動差し替えする。

## 3. Unity / display policy

- Unity使用。
- マウス操作のみ。
- コントローラー操作は想定しない。
- 基準解像度は1920x1080。
- 最初は16:9固定。
- UIはuGUI + TextMeshPro想定。
- Canvas Scalerは `Scale With Screen Size`。
- Reference Resolution は `1920 x 1080`。
- 自動生成UIや仮UIでは英語表示を優先し、日本語TMPフォント問題は後で対応する。

## 4. Current implementation status

現在の主な実装対象は以下。

```text
Assets/Scripts/Battle/BattleUIManager.cs
Assets/Scripts/Battle/CommandPanelController.cs
Assets/Scenes/BattleTest.unity
```

現在の確認済み状態:

- Console赤エラーなし。
- Command UI切替OK。
- 固定Skill/Swap/ItemボタンOK。
- ダミー戦闘データ接続OK。
- Status表示OK。
- TurnNumberは素早さ順に基づく。
- TurnNumberはターン中固定。
- 行動済みユニットのTurnNumberは非表示。
- Skill / Item成功で行動済み化。
- Item失敗 / Swap / Rotateでは行動終了しない。
- 次activeへの進行OK。
- active強調表示OK。
- Skill対象マスPreview OK。
- 敵HPダメージ処理OK。
- 敵KO時の盤面除去OK。
- 敵Statusの生存敵リスト化・上詰めOK。
- EnemyStatusPanelは全滅時のみ非表示。高さ縮小は一旦行わない。
- 敵dummy行動は位置ベース攻撃。
- 敵dummyダメージは60を現行値とする。
- 味方KO時の控え自動補充OK。
- 敵KO時の控え自動補充OK。
- 控えなし味方KO時は味方盤面マスを空にする。
- 勝利判定OK。
- 敗北判定は実装済みだが、通常確認ではまだ確認しづらい。

直近の次候補:

```text
味方StatusのKO/空枠表示整理
```

## 5. Battle system overview

- 4vs4のコマンドバトル。
- 敵味方それぞれ2×2マス。
- 味方には控えキャラあり。
- 敵にも控えを持たせる方針。
- 味方ターン / 敵ターンのような陣営別ターン制ではない。
- ターン開始時に、盤面上の敵味方全員を素早さ順に並べて行動順を決定する。
- 行動順は敵味方混在。
- 全員が行動終了した時点で1ターン終了。
- そのターン中に素早さが変わっても順番は変えない。
- 次ターン開始時に再計算する。
- 交代や回転は行動権を消費しない。
- 技またはアイテムを確定するまで、交代や回転を行える。

## 6. Grid coordinates

2×2盤面は以下で管理する。

```text
[ FrontTop    ][ BackTop    ]
[ FrontBottom ][ BackBottom ]
```

- Front = 前列。
- Back = 後列。
- Top = 上段。
- Bottom = 下段。
- 左が前、右が後ろ。

## 7. Mouse control policy

- 左クリック：選択 / 決定。
- ホバー：説明表示 / 技の対象マスプレビュー。
- 右クリック：Skill表示へ戻る。
- 空いている盤面をクリックしても何も起きない。
- 味方や敵にホバーしても何も起きない。
- 回転ボタンは味方盤面の近くに常設。
- 将来的にマウスホイールで盤面回転する案も検討する。

## 8. Command UI

現在のCommand UIは、Scene上の固定ボタンをInspector参照で扱う方式。

表示方針:

```text
MainCommandButtons は常時表示
Play開始時は SkillListPanel 表示
Fight → SkillListPanel
Swap → SwapListPanel
Item → ItemListPanel
右クリック → SkillListPanel
```

旧方針の「通常メニューへ戻る」は現在の実装には合わない。現時点では、右クリックや初期表示は `SkillListPanel` が基準。

Swap:

- 控えキャラをクリックすると即交代。
- 交代は行動権を消費しない。
- 交代後も技またはアイテムを使うまで行動終了しない。
- Swap中にRotateしても、表示中のSwapListPanelは維持する。

Rotate:

- 味方盤面全体を90度時計回りに回転する。
- Rotateは行動権を消費しない。
- Rotateしても現在のコマンド画面は維持する。
- active強調と対象Previewは更新する。

## 9. Skill policy

- 各キャラは技を4つ持つ。
- 現在のダミー技は `Slash / Pierce / TwinHit / Wave`。
- 技にhoverすると対象マスを黄色系でPreview表示する。
- 空マスも対象ならPreview表示する。
- 技クリックで使用確定し、そのキャラの行動終了。
- Skill使用後、次の未行動ユニットへ進む。
- 敵の番ならdummy enemy actionとして自動処理する。

現在のダミー対象:

```text
Slash   → Enemy FrontTop
Pierce  → Enemy FrontBottom
TwinHit → Enemy FrontTop / Enemy FrontBottom
Wave    → Enemy 4マス全部
```

現在のダミーダメージ:

```text
Slash   → 20
Pierce  → 20
TwinHit → 15 each
Wave    → 10 each
```

将来方針:

- 技は拠点で複数候補から4つを選んで装備する。
- MP不足や条件不足の技は暗くしてクリック不可にする。
- 技選択中にMP消費量をPreviewする。
- 合体技は通常技と同じ4枠の中に入る候補として扱う。

## 10. MP / resource policy

- MP回復は非常にシビアにする、または無しにする。
- MPの枯渇により、強いキャラだけで戦い続けることを難しくする。
- 全員で戦う必要があるバランスを目指す。
- MP0でも、回転・交代・アイテム使用・合体技条件としての配置役は可能。
- MP0でも使える基本技は必須にしない。MP0でパスしかできない状態もバランス調整対象として許容。

## 11. Combo skill policy

- 合体技を導入予定。
- 初期実装では通常技を優先し、合体技は後から追加する。
- 合体技は通常技と同じく、キャラの技枠4つのうち1つとして選択できる想定。
- 条件には、特定キャラの存在、前衛配置、特定マス、特定バフなどを将来設定できるようにしたい。
- 合体技はキャラ同士の関係性や会話イベントと結びつける。
- 弱いキャラにも、合体技の相方として出番を作れる。

## 12. Formation / rotation policy

- 前後列を直接入れ替える専用コマンドは実装しない。
- 前衛・後衛の調整は、位置固定のキャラクター交代と盤面全体の90度回転を組み合わせて行う。
- 列入れ替えを別コマンド化せず、回転と交代を使ったパズル要素として扱う。
- 回転後、現在操作中キャラの位置と技対象Previewを更新する。

## 13. Battle UI layout policy

画面構成:

```text
左：EnemyStatusPanel
中央左：EnemyGridPanel
中央右：AllyGridPanel
右：AllyStatusPanel
上中央：CommandPanel
上部/敵側付近：BossNamePlate
味方盤面付近：RotateButton
```

現行方針:

- `TurnOrderBar` は復活させない。
- 行動順数字はStatus欄へ統合する。
- バトルログは廃止。

## 14. Status UI policy

### EnemyStatusPanel

- 画面左側に配置。
- HPのみ表示。敵MPは表示しない。
- 敵HPはバーのみ。数値は表示しない。
- 行動順数字を表示。
- KO済み敵はStatusから消える。
- 生存している出撃中敵だけを上から詰めて表示する。
- 空Statusスロットは非表示。
- EnemyStatusPanel本体の高さ変更は一旦行わない。全滅時のみPanel全体を非表示にする。
- EnemyStatusPanelのAnchor / Pivotはコードで上書きしない。Scene側RectTransform設定を尊重する。
- 子スロットはPanel高さをもとに上端寄せで配置する。

### AllyStatusPanel

- 画面右側に配置。
- HP/MPバーと数値を表示。
- 行動順数字を表示。
- active allyのStatus枠を青系で強調する。
- 味方Statusはパーティ表示順ベースで扱う。
- KO時に控えが出る場合、Status表示は控えに差し替わる。
- 控えがいないKO時の専用表示は未整理。

### Status order

- グリッドを移動・回転させても味方Statusの並び順は原則変化しない。
- 敵Statusは現在生存している出撃中敵の一覧として扱い、KOで詰める。
- 敵控えが出た場合、倒れた敵の位置に入った控えをStatusに表示する。

## 15. Turn order display policy

- 行動順は各Status欄の数字で表示する。
- 数字はターントップから数えた順番。
- 行動順は敵味方全員を素早さ順に混ぜた順番。
- ターン中は行動順数字を再計算しない。
- 全員が行動し終わったら1ターン終了。
- 行動前：数字あり。
- 行動済み：数字なし。暗くしない。
- KO：数字なし。
- ターン途中に補充された控え：数字なし、そのターン中は行動不可。
- 次ターン開始時に補充控えもSpeed順へ参加する。

現在行動中の強調:

- active allyの盤面セルを青系で強調する。
- 対応するAllyStatus枠も青系で強調する。

## 16. KO / reserve replacement policy

共通方針:

- KOされたユニットの行動権は消滅する。
- 控えが同じGridPosに自動投入されても、KOされたユニットの行動権は引き継がない。
- 補充ユニットは `_actedUnits` に追加され、そのターン中は行動不可。
- 補充ユニットは次ターン開始時の `RebuildTurnOrder()` でSpeed順に参加する。
- このルールは敵味方共通。

味方KO:

- 敵dummy行動で味方HPが0になると `HandleAllyDefeated()` を呼ぶ。
- 控えがいれば、KO味方と同じGridPosへ出す。
- `_allies` をKO味方から控えに差し替える。
- `_reserves` から出撃控えを削除する。
- KO味方のTurnNumber / acted stateは `RemoveTurnState()` で消す。
- 控えがいない場合、KO味方は盤面から消え、そのGridPosは空マスになる。
- 控えがいないKO時のAllyStatus専用表示は今後整理。

敵KO:

- 敵HPが0になると `HandleEnemyDefeated()` を呼ぶ。
- KO敵を盤面から除去する。
- 敵控えがいれば、KO敵と同じGridPosへ出す。
- `_enemies` をKO敵から控え敵に差し替える。
- `_enemyReserves` から出撃控え敵を削除する。
- KO敵のTurnNumber / acted stateは `RemoveTurnState()` で消す。
- 補充敵はそのターン中dummy enemy actionしない。

## 17. Enemy dummy action / targeting policy

現在の敵dummy行動は位置ベース攻撃。

```text
Enemy FrontTop    → Ally FrontTop
Enemy BackTop     → Ally BackTop
Enemy FrontBottom → Ally FrontBottom
Enemy BackBottom  → Ally BackBottom
```

- 対象マスに味方がいない / KO済みの場合、`missed unavailable ally cell` を出してMiss。
- 敵dummyダメージは現行値 `60`。
- まだ本格AIではない。

次以降の候補:

```text
敵ごとのスキル選択
正面対象以外の攻撃パターン
前列優先ターゲット
```

## 18. Battle end policy

実装済み。

最小仕様:

```text
出撃中の生存敵が0
かつ
生存敵控えが0
→ Victory

出撃中の生存味方が0
かつ
生存味方控えが0
→ Defeat
```

戦闘終了後:

- `_battleEnded` flagを立てる。
- `commandPanel.SetInteractable(false)` で入力停止する。
- 以後ターン進行しない。
- 上部表示は `Victory / Battle End` または `Defeat / Battle End`。

## 19. Boss UI policy

- ボスもEnemyStatusPanelに1枠で表示する。
- ボス名はBossNamePlateにも表示する。
- ボスが大型の場合、盤面では複数マスにまたがって見せてもよい。
- 初期実装では複数マスボスを内部仕様としては実装せず、代表マスを持つ単体として扱う。

## 20. Quest interval / reward policy

- クエストは複数戦闘で構成される。
- 各戦闘の間に、休憩・死体漁りの2つの選択肢を出す想定。
- 会話イベントは独立した選択肢ではなく、休憩または死体漁りに付随するイベントとして扱う。
- 条件を満たした場合、休憩・死体漁りのアイコン右上などに会話イベント発生マークを表示する。
- 会話マーク付きの選択肢を選ぶと、通常効果に加えて会話イベントが発生する。
- 会話イベントは、キャラ関係性の進行や合体技解放につながる可能性がある。
- 装備などの物理的な報酬は少なめにする想定。
- 技の解放・技候補の増加・会話イベントを主要報酬にしたい。

## 21. Character performance policy

- 全キャラを同じ強さにはしない。
- あえて性能差をつける。
- 弱いキャラでも、合体技条件・盾役・配置役・関係性イベントによって出番を作る。
- 控え交代とMP制限により、単一強キャラだけで戦い続ける状態を避ける。

## 22. Item policy

- 回復アイテムのみ。
- 対象は操作キャラの前のマスにいる味方1人。
- BackTopのキャラはFrontTopの味方に使用可能。
- BackBottomのキャラはFrontBottomの味方に使用可能。
- FrontTop / FrontBottomのキャラは前方マスがないため、そのままではアイテム使用不可。
- アイテムを使用したら、そのキャラの行動終了。
- アイテム使用失敗では行動終了しない。

## 23. Acting / animation / asset policy

行動確定後または敵行動中は、最終的にコマンドUIを操作不能にする予定。

画面上部中央に表示:

```text
1行目：使用技名
2行目：使用者名
```

演出方針:

- 攻撃元マスと対象マスを強調表示。
- 無関係キャラは少し目立たなくする。
- 攻撃時は使用者が少し前に出る。
- 被弾時は対象キャラが小さく揺れる。
- 操作中キャラは立ち絵を光らせる。

素材方針:

- 絵が描けない前提のため、VRoidを「3Dモデルとして直接表示する」のではなく、2D立ち絵素材生成用として使う方針。
- VRoidでキャラを作り、ポーズ・表情を付け、透過PNGとして書き出す。
- Unityでは普通の2D立ち絵として扱う。
- まずは `normal / attack / damage / ko` 程度の差分で十分。
- 背景も3D/素材を固定カメラで撮影して2D背景化する方針が現実的。

## 24. Buff / debuff policy

初期実装:

- 攻撃アップ。
- 防御アップ。
- 攻撃ダウン。
- 防御ダウン。

実装しない:

- 素早さアップ。
- 素早さダウン。
- 状態異常。

ルール:

- 基本持続は2ターン。
- 同じ効果は上書き。
- 重複加算しない。
- 同種の逆効果がかかった場合は打ち消して0に戻す。

## 25. Codex usage policy

現時点では、Unity Scene / Inspector参照 / Battle UI接続作業ではCodexを使わない方針。

理由:

- 古いbranch差分を引きずることがあった。
- `BattleUICreator.cs` を勝手に混ぜることがあった。
- `TurnOrderBar` を復活させることがあった。
- 同一変数の二重宣言など、コンパイル不能なコードを出すことがあった。
- 既存APIを削除することがあった。

当面の方針:

- 変更対象ファイルを1つに絞る。
- ChatGPT側で修正版コードまたは差分を作る。
- 手元で手動反映する。
- UnityでConsole / Play確認する。
- GitHub Desktopで変更ファイルを確認してからpushする。

Codexを使う場合は、必ず以下を指定する。

```text
目的：
対象ファイル：
変更禁止ファイル：
実装内容：
テスト：
出力：
```

## 26. PROJECT_CONTEXT.md update policy

このチャットでは、重要な仕様決定・作業方針変更・トラブル対応方針が固まった場合、確認なしで `docs/PROJECT_CONTEXT.md` へ追記・修正してよい。

ただし、追記・修正の成功/失敗は応答で明記する。

追記対象の例:

- 戦闘仕様の確定・変更。
- UI仕様の確定・変更。
- GitHub / Unity / Codexの運用ルール変更。
- 作業ログとして残す価値がある失敗と対処。
- 新規チャット移行に必要な前提情報。

## 27. Immediate next tasks

1. 味方StatusのKO/空枠表示整理。
2. 敗北判定の実動作確認。
3. バトルデータをdummy生成から外部データ/ScriptableObject寄りに分離する。
4. 敵ごとのスキル/ターゲットパターンを導入する。
5. 戦闘終了時の結果UIを仮実装する。

## 28. Work log

### 2026-05-10

- Canvas設定完了。
- BattleUICreatorによるUI自動生成を試行。
- 日本語TMPフォント問題を確認。
- Codexのpush不可問題を確認。
- `docs/PROJECT_CONTEXT.md` を作成。
- 技4枠、合体技、MP制限、戦闘間コマンド、会話イベント付随方式などの初期方針を整理。

### 2026-05-11

- GitHub App / ChatGPT Connector が `Kenu4000/game_kari` にアクセスできることを確認。
- `game_kari` private化後もConnectorから読み書き可能な状態を確認。
- 不要な古いCodexブランチを削除。
- `TurnOrderBar` 廃止、Status欄へのTurnNumber統合方針を確定。
- 敵StatusはHPのみ、味方StatusはHP/MP表示に確定。
- 行動順は敵味方混在のSpeed順、ターン中固定に確定。

### 2026-05-12

- `CommandPanelController` を固定ボタン方式へ移行。
- Play開始時からSkillListPanel表示に変更。
- Fight / Swap / Item / 右クリックの表示切替を確認。
- `BattleUIManager` を作成し、dummy battle dataをScene UIへ接続。
- Status欄に敵味方名・HP/MP・TurnNumberを反映。
- TurnNumberをSpeed順の実データへ接続。
- TurnNumberはターン中固定、Swap/Rotateで再計算しない仕様を確認。
- Skill / Item成功で行動済み化し、TurnNumberを非表示にする処理を追加。
- 行動後に次activeへ進む処理を追加。
- 敵dummy行動をSpeed順の途中に挟む処理を追加。
- active allyの盤面セル・Status枠の強調表示を追加。
- Skill使用時の敵HP減少とHPバー反映を追加。
- 敵KO時に盤面から除去し、空マス攻撃をMissにする処理を追加。
- EnemyStatusを生存敵リスト化し、KOで上詰め・空スロット非表示にする処理を追加。
- EnemyStatusPanel高さ縮小は表示ずれが出たため一旦停止し、全滅時のみ非表示に変更。
- Skill hover時の対象マスPreviewを追加。
- Skill hover中にactorが変わってもPreviewを再適用する処理を追加。
- 敵dummy行動で味方HPを減らす処理を追加。
- 味方KO時の控え自動補充を追加。
- 敵KO時の控え自動補充を追加。
- KO補充は敵味方ともに行動権を引き継がない仕様に確定。
- 勝利判定を実装し、Victory / Battle End表示とCommand UI停止を確認。
- 敵dummy行動を位置ベース攻撃に変更。
- 敵dummyダメージは60を現行値に変更。
- 控えなし味方KO時、味方盤面マスを空にする処理を追加。
