# game_kari Project Context

## 1. Current project direction

Unityで制作中の戦闘特化型RPG。

現在の設計は、旧Wave/Distance制から **固定ルート制Quest** へ移行する。

旧方針:

```text
Wave Clear
→ DistanceGain
→ CurrentDistance加算
→ TargetDistance到達を目指す
```

新方針:

```text
Base
↓
Quest Select
↓
Quest Start
↓
Movement UI
↓
Fixed Battle Point
↓
Battle Preparation
↓
Battle
↓
Battle Result
↓
Movement UI
↓
Event Point / Battle Point / Boss
↓
Quest Result
↓
Return to Base
```

主軸は、4vs4の陣形型コマンドバトルと、Quest中のHP / MP / KO / Item持ち越し管理。

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
- `BattleUIManager.cs` の大きな全文置換は避ける。必要な場合はメソッド単位で差し替える。

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

## 4. Current implementation warning

リポジトリ内には、旧Wave/Distance制の移行中コードが残っている可能性がある。

例:

- `WaveData`。
- `WaveProgressState`。
- `BaseDistance`。
- `TargetDistance`。
- `DistanceGain`。
- `Quest Clear (n/n)` 表示。
- Distance / Progress を出すResult表示。

これらは移行対象。今後の新規実装ではDistance制を伸ばさない。

内部クラス名として `WaveData` は当面残してよい。プレイヤー表示上は `Wave` ではなく、`Battle` / `襲撃` / `戦闘地点` / `Boss` へ寄せる。

## 5. Route Quest policy

Questは固定一本道ルート。

RoutePoint種別は5種で固定する。

- `Start`。
- `Normal`。
- `Battle`。
- `Event`。
- `Boss`。

`Goal` は使わない。最終地点は `Boss`。

初期挙動:

- `Start`: 開始地点。
- `Normal`: 何も起きない。
- `Battle`: 固定戦闘。
- `Event`: テキストだけ表示して次へ。
- `Boss`: 最終固定戦闘。勝利後にQuest Resultへ。

確率戦闘は廃止。通常地点や線分でランダム襲撃は発生しない。

## 6. Removed systems

以下は現行ターゲットから廃止。

- Distance。
- `BaseDistance`。
- `DistanceGain`。
- `CurrentDistance`。
- `TargetDistance`。
- Distance補正。
- Battle Result上のDistance / Progress表示。
- 確率戦闘。
- ランダム襲撃。
- ステルス。
- カケラドライブ。
- 安全化レーン。
- カケラで通常襲撃を防ぐ仕様。
- GOボタン。
- 移動UI上のtextwindow。
- 移動UI上の常時ステータス表示。
- 移動UI上の常時編成 / Itemボタン。

## 7. Battle system overview

- 4vs4のコマンドバトル。
- 敵味方それぞれ2×2マス。
- 味方には控えキャラあり。
- 敵にも控えを持たせる。
- 味方ターン / 敵ターンの陣営別ターン制ではない。
- ターン開始時に盤面上の敵味方全員を素早さ順に並べる。
- 行動順は敵味方混在。
- 全員が行動終了した時点で1ターン終了。
- 次ターン開始時に行動順を再計算する。
- Skill / Swap / Item / Rotate を維持する。
- Swap / Rotateは行動権を消費しない。
- 技またはアイテムを確定するまで、交代や回転を行える。

## 8. Quest resource carryover

Quest中に維持するもの:

- 味方HP。
- 味方MP。
- KO状態。
- 前線配置。
- 控え。
- Inventory残数。
- カケラ所持数。

固定戦闘ごとに入れ替えるもの:

- 敵前線。
- 敵控え。
- 敵HP / KO / 状態。

Base帰還時:

- HP全回復。
- MP全回復。
- KO復帰。
- Quest中カケラ破棄。

## 9. MP policy

- Ally max MP is currently 4.
- Quest開始時、参加味方はMP 4/4。
- 固定戦闘開始時、生存中の味方前線 + 控えはMP +1。
- KO中の味方はMP回復しない。
- KO中の味方は現在MPを維持する。
- MP回復はMaxMPを超えない。
- 敵にはMP制を入れない。
- Swap / Rotate / Item / Pass はMP 0でも可能。
- Link skillはユーザーMPと指定パートナーMPを消費する現方針を維持。

## 10. Clear evaluation

表示上のClear評価は3区分に統一する。

- `1Turn Kill`。
- `2Turn Kill`。
- `3+ Turn`。

`4+Turn` は表示しない。内部に旧enumが残る場合は `3+ Turn` に丸める。

カケラ報酬:

- `1Turn Kill`: +3。
- `2Turn Kill`: +2。
- `3+ Turn`: +1。

## 11. Kakera policy

カケラはQuest中のみ有効な戦闘準備ボーナス用リソース。

- 最大9。
- 持ち込み不可。
- 持ち帰り不可。
- 戦闘評価で獲得。
- 次以降の戦闘準備画面で使用。

初期用途:

- カケラ1消費で次の戦闘の敵情報を確認する。

表示候補:

- 敵数。
- 敵配置。
- 敵の大まかな役割。

役割表示は後実装でよい。初期は固定文字列や仮表示でよい。

初期では入れない効果:

- HP回復。
- KO復帰。
- MP回復。
- 敵への事前ダメージ。
- 固定戦闘スキップ。
- 味方全体バフ。

## 12. 1Turn Kill HP bonus

1Turn Kill時、生存中の味方にHP +5。

対象:

- 生存中の前線味方。
- 生存中の控え味方。

対象外:

- KO中の味方。

表示方針:

- Battle Resultの大きな表には載せない。
- 戦闘終了直後に `1TurnKill BONUS!` のようなテロップと回復演出で見せる。

## 13. Battle preparation screen

固定戦闘地点に到達したら即戦闘ではなく、戦闘準備画面を挟む。

流れ:

```text
Movement UI
↓
固定戦闘地点に到達
↓
襲撃テロップ
↓
Battle Preparation
↓
Battle Start
```

初期実装は既存BattleUI内の仮パネルでよい。

初期表示:

- HP / MP / KO概要。
- カケラ所持数。
- 敵情報: 未確認 / 確認済み。

初期操作:

- カケラ1消費で敵情報確認。
- 戦闘開始。

後回し:

- 配置変更。
- Item使用。
- 技4つ確認。
- 連携条件確認。

戦闘準備画面から移動画面へ戻ることは基本不可。必要なら将来、撤退のみ設計する。

## 14. Battle Result policy

旧Wave Result / Distance Resultは廃止。

Battle Result表示:

```text
Battle Clear
Clear: 1Turn Kill
Kakera: +3
EXP: +10
Lv Up: None

[Formation] [Next]
```

EXP / Lv Upは初期では仮値表示のみ。実際の経験値・Lv Up処理は後回し。

下部ボタン:

- `Formation`。
- `Next`。

`Formation` は戦闘準備画面と同じ仮パネルを開く。

`Next` は移動UIへ戻り、固定ルート上を次の地点へ進める。

## 15. Movement UI policy

移動UIは管理画面ではなく、レーダー演出つきのルート確認画面。

構成:

- 上段: レーダー演出。
- 下段: 正確なルートバー。

上段レーダー:

- 黒い半円レーダー。
- 外周の目盛り。
- トラック / バスのピクトグラム。
- 直近1〜2個の戦闘 / イベントアイコン。
- 背景ロケーション差分に依存しない抽象表現。
- 黒白モノトーンのピクトグラム調。
- 下段ルートバーとある程度位置合わせする。

下段ルートバー:

- 正確なルール情報。
- 現在位置。
- 次の固定戦闘。
- 次のイベント。
- Boss。
- あとnマス / あとn区間。

例:

```text
S ─ ○ ─ 🚚 ─ ⚔ ─ ⚠ ─ ○ ─ ⚔ ─ Boss
```

操作:

- 移動画面中、画面クリックで一時停止。
- 停止マークを表示。
- もう一度クリックで再生。
- 通常は1秒前後の短い簡易アニメ。
- クリックで即スキップ可能な挙動も検討。

## 16. Quest Result policy

Distanceは出さない。

`Party: Alive x/y, KO z` は固定表示しない。

初期Quest Result:

```text
Quest Clear
Battles Cleared: 3 / 3
Kakera Earned: total
EXP: total
Next: Return to Base
```

Party詳細は内部集計として残してよいが、常時表示はしない。必要なら詳細画面やFormation画面で見る。

後回し:

- 総合Grade。
- Item使用数。
- 報酬一覧。
- Lv Up処理。
- Skill強化。

## 17. Battle UI / status policy

現行のBattle UI方針は維持する。

- uGUI + TextMeshPro。
- MainCommandButtons は常時表示。
- SkillListPanelを基準にする。
- 右クリックでSkill表示へ戻る。
- Battle logは廃止。
- 敵HPはバー中心。
- 味方StatusにはHP / MP / KO状態を表示。
- 敵にはMPを表示しない。

## 18. Grid coordinates

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

## 19. Mouse control policy

- 左クリック: 選択 / 決定。
- ホバー: 説明表示 / 技の対象マスプレビュー。
- 右クリック: Skill表示へ戻る。
- 空いている盤面をクリックしても何も起きない。
- 味方や敵にホバーしても何も起きない。
- 回転ボタンは味方盤面の近くに常設。

## 20. Skill / Link policy

- 各キャラは技を4つ持つ。
- 技にhoverすると対象マスをPreview表示する。
- 空マスも対象ならPreview表示する。
- 技クリックで使用確定し、そのキャラの行動終了。
- Skill使用後、次の未行動ユニットへ進む。
- 敵の番なら敵AI行動として自動処理する。
- 技は拠点で複数候補から4つを選んで装備する将来方針。
- 合体技は通常技と同じ4枠の中に入る候補として扱う。
- Link skillは指定パートナー条件を使う現方針を維持。

## 21. Formation / rotation policy

- 前後列を直接入れ替える専用コマンドは実装しない。
- 前衛・後衛の調整は、位置固定のキャラクター交代と盤面全体の90度回転を組み合わせる。
- Rotateは行動権を消費しない。
- Rotate後、active位置と対象Previewを更新する。

## 22. Immediate implementation order

次の実装順:

1. Distance / Progress表示をBattle Resultから削除。
2. Clear評価を `1Turn Kill` / `2Turn Kill` / `3+ Turn` に変更。
3. カケラ所持数・獲得量を追加。
4. Battle Resultを `Clear / Kakera / EXP / Lv Up / Formation / Next` に変更。
5. 旧Quest Resultから固定 `Party Alive/KO` 表示を外す。
6. RoutePointData / route modelを追加。
7. 移動UIの下段ルートバーを仮実装。
8. 戦闘準備画面を既存BattleUI内の仮パネルとして追加。
9. 上段レーダー演出を追加。

## 23. Deferred systems

- ScriptableObject QuestData。
- ScriptableObject RouteData。
- Full movement radar animation。
- Formation editing in preparation screen。
- Item use in preparation screen。
- Skill inspection in preparation screen。
- Detailed enemy role display。
- EXP processing。
- Lv Up processing。
- Quest reward processing。
- Grade evaluation。
- Item usage count display。
- Real Base scene。
- Quest Select scene。

## Route Flow Implementation Status

- Quest is currently driven by fixed RoutePoint data: Start / Normal / Battle / Event / Boss.
- Battle Result uses Kakera / EXP / Lv Up / Next display instead of Distance / Progress.
- Temporary Route Movement, Event, Battle Preparation, Scout, Quest Clear, and Quest Failed panels are implemented in BattleUIManager.
- Distance fields and progress calculation have been removed from BattleSetupData, QuestData, WaveData, QuestResultData, and WaveProgressState.
- WaveProgressState is now responsible only for battle turn tracking used by Clear evaluation.
- WaveData and QuestData.Waves remain as compatibility data for enemy battle definitions.
- Battle Result internals use BattleClear* naming rather than WaveClear* naming.


## Result Panel Cleanup Status

- Quest Clear and Quest Failed now share the same summary text builder.
- Result overlay setup is centralized through helper methods in BattleUIManager.
- Route Movement and Event temporary panels use the same button setup policy.
- The temporary route bar uses readable RoutePoint labels while keeping the UI provisional.
- WaveData and QuestData.Waves remain as compatibility data for battle definitions.


## Route Preparation Text Polish

- Route Event and Battle Preparation temporary panels now use the shared result overlay setup helper.
- Battle Preparation text is grouped into Party, Kakera, Enemy Info, Scout action hint, and Start Battle.
- Scout state now distinguishes hidden details, completed scout, and insufficient Kakera.
- Enemy formation scout output is displayed line-by-line for readability.
- This remains a temporary text-based UI until dedicated Movement / Preparation panels are created.


## Route Movement Text Cleanup

- Movement temporary UI now builds its body through `BuildRouteMovementText()`.
- Movement display is grouped into Quest title, Current point, Route bar, Next point, and Next action.
- Route bar uses `->` separators and readable route point labels while remaining provisional.
- Current point display includes route index information when available.
- Next important point display now reports segments without using Distance terminology.


## Battle Result To Movement Flow

- Battle Result `Next` must return to the temporary Movement panel.
- Movement `Next` is the only route action that advances to Event / Battle Preparation / Quest Clear.
- Battle Result must not directly start the next battle.
- The remaining `HasNextWave` field is temporarily fed by `HasNextRoutePoint` until result data naming is cleaned further.


## Battle Result Explicit State

- Battle Result now has an explicit `_showingBattleResult` state flag.
- Battle Result `Next` returns to Movement and does not directly start Battle Preparation or Battle.
- Movement `Next` remains the only route advancement action.
- This avoids stale Preparation / Quest result flags affecting the shared ResultPanel button behavior.

