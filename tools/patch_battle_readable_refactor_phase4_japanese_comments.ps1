$ErrorActionPreference = 'Stop'

# Battle readable refactor phase 4
# ------------------------------------------------------------
# partial ファイルに追加した読みやすさ用コメントを日本語へ寄せる。
# 既存の処理本体は変更しない。

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

function Replace-Block([string]$path, [string]$methodName, [string[]]$jpLines) {
    if (!(Test-Path $path)) {
        Write-Host "Skip missing file: $path"
        return
    }

    $text = Get-Content -Path $path -Raw -Encoding UTF8

    $pattern = "(?ms)^        // READABLE-REFORM: " + [regex]::Escape($methodName) + "\r?\n(?:        // .*?\r?\n)+"
    $replacement = "        // 読みやすさメモ: $methodName`r`n"
    foreach ($line in $jpLines) {
        $replacement += "        // $line`r`n"
    }

    $newText = [regex]::Replace($text, $pattern, $replacement, 1)
    if ($newText -eq $text) {
        Write-Host "Skip comment block not found: $methodName"
        return
    }

    Write-Utf8NoBom $path $newText
    Write-Host "Translated comment: $methodName"
}

function Replace-Literal([string]$path, [string]$old, [string]$new) {
    if (!(Test-Path $path)) { return }
    $text = Get-Content -Path $path -Raw -Encoding UTF8
    if ($text.Contains($old)) {
        $text = $text.Replace($old, $new)
        Write-Utf8NoBom $path $text
        Write-Host "Translated literal in $path"
    }
}

# ------------------------------------------------------------
# 各partialファイル冒頭の英語エリア説明を日本語化
# ------------------------------------------------------------
Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' @'
        // ACTIONS AREA
        // ------------------------------------------------------------
        // Player and enemy action resolution methods live here.
        // This file should contain the readable flow of actions.
        // Damage calculation, KO, status panels, preview, and animation can be called from here.
'@ @'
        // 行動処理エリア
        // ------------------------------------------------------------
        // プレイヤーや敵の行動解決に関係するメソッドを置く。
        // ここでは「行動を選んでから結果が出るまで」の大きな流れを読みやすく保つ。
        // ダメージ計算、KO処理、ステータス表示、プレビュー、アニメーションの細部は別ファイルへ任せる。
'@

Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.Animation.cs' @'
        // ANIMATION AREA
        // ------------------------------------------------------------
        // Battle animation bridge methods live here.
        // These methods should connect battle logic to visual animation without changing battle rules.
'@ @'
        // アニメーション処理エリア
        // ------------------------------------------------------------
        // 戦闘ロジックと見た目のアニメーションをつなぐメソッドを置く。
        // ここでは戦闘ルールを変えず、見た目の再生だけを担当する。
'@

Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.KO.cs' @'
        // KNOCKOUT / REPLACEMENT AREA
        // ------------------------------------------------------------
        // Defeat, KO visuals, enemy compaction, and reserve replacement live here.
        // This area mutates the board after HP reaches zero.
'@ @'
        // KO・補充処理エリア
        // ------------------------------------------------------------
        // 撃破、KO表示、敵の前詰め、控えからの補充に関係する処理を置く。
        // HPが0になった後に、盤面をどう更新するかを担当する。
'@

Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' @'
        // PREVIEW AREA
        // ------------------------------------------------------------
        // Hover previews and target preview visuals live here.
        // These methods should not change battle state. They only change what the player sees.
'@ @'
        // プレビュー表示エリア
        // ------------------------------------------------------------
        // スキルhover時の見た目や、対象セルのプレビュー表示に関係する処理を置く。
        // ここでは戦闘状態を変えず、プレイヤーに見せる表示だけを変更する。
'@

Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs' @'
        // STATUS PANEL AREA
        // ------------------------------------------------------------
        // Ally/enemy status panel drawing and floating HP bars live here.
        // Keep this file focused on UI display, not battle rule decisions.
'@ @'
        // ステータス表示エリア
        // ------------------------------------------------------------
        // 味方・敵のステータス欄や、被弾時の浮動HPバー表示に関係する処理を置く。
        // ここでは戦闘ルールを決めず、UI表示に集中する。
'@

Replace-Literal 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' @'
        // TURN / PHASE AREA
        // ------------------------------------------------------------
        // Turn order, active unit selection, and battle result transitions live here.
        // Keep player command UI, resolving state, and battle end checks readable from this file.
'@ @'
        // ターン・フェーズ管理エリア
        // ------------------------------------------------------------
        // 行動順、現在の行動者、戦闘終了時の遷移に関係する処理を置く。
        // コマンド入力中、行動解決中、戦闘終了判定の流れをここで読み取れるようにする。
'@

# ------------------------------------------------------------
# phase3 で追加したメソッド説明コメントを日本語化
# ------------------------------------------------------------
Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'HandleSkillClicked' @(
    'プレイヤーがスキルボタンを押したときの入口。',
    'このメソッドでは、今コマンド入力を受け付けてよい状態かを確認し、行動解決の流れへ進める。',
    'ダメージ計算、KO処理、ステータス表示の細部はここに詰め込まず、別の担当メソッドへ渡す。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'ResolvePlayerSkillAfterIntroDelay' @(
    'プレイヤースキルの行動名表示後に実行されるコルーチン。',
    '基本順序は「行動名表示 → 少し待つ → アニメーション再生 → 効果適用 → 次の流れへ進む」。',
    'ダメージ発生タイミングや演出タイミングが分かりにくくなったら、まずここを見る。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'HandleRotateClicked' @(
    '陣形回転ボタンが押されたときの処理。',
    '回転すると行動者や見た目の重なりが変わるため、必要に応じてhoverプレビューも再適用する。',
    '盤面データと表示のずれが出た場合は、この周辺を確認する。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Animation.cs' 'PlaySkillAnimationIfAny' @(
    '戦闘ロジックからスキルアニメーション再生へつなぐ橋渡し。',
    'ここでは使用者と対象のUI位置を探し、SkillAnimationPlayerへ渡す。',
    '実際のコマ送り、弾の移動、Canvas上の動きはアニメーション側の担当。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Animation.cs' 'GetPrimarySkillAnimationTargetRect' @(
    'スキルアニメーションで向かう主対象のRectTransformを探す。',
    'これは見た目の対象であり、ダメージ対象そのものを決める場所ではない。',
    'アニメーションが違うセルへ飛ぶ場合は、ここかSkillAnimationDataのAnchor設定を確認する。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ClearTargetPreview' @(
    '対象プレビューの見た目だけを消す。',
    'HP、行動順、KO状態、控え状態などの戦闘データは変更しない。',
    'hover終了時や、プレビューを描き直す前に呼ぶ。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'RedrawTargetPreview' @(
    'スキルhover中や選択中に、黄色の対象プレビューを描き直す。',
    'ここでは「今どのセルを強調表示すべきか」だけを扱う。',
    '実際に誰へダメージを与えるかは別の処理が決める。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ApplySkillHoverSpritePreview' @(
    'スキルhover中の灰色シルエット表示を適用する。',
    '行動者と対象は通常表示のままにし、それ以外を薄いシルエットにする。',
    '回転後や敵補充後にhover表示がおかしい場合は、まずここを見る。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ApplySkillHoverSilhouetteOverlapAlpha' @(
    '上段の行動者と下段の味方が見た目上重なる場合の透明度調整。',
    'ここで変更するのは見た目のalphaだけ。',
    '実際のユニットデータや盤面データは変更しない。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs' 'RedrawStatusPanels' @(
    '現在の戦闘データをもとにステータス欄を再描画する。',
    'ステータス欄は表示専用で、HPやKO状態を決定する場所ではない。',
    'データ上のHPは正しいのにバー表示だけがおかしい場合は、この周辺を見る。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'ResolveDefeatedEnemies' @(
    'ダメージ適用後、倒れた敵を処理する。',
    'KOフェード、敵の前詰め、控え敵の登場、ステータス更新の順番が重要。',
    '敵撃破後の見た目や盤面更新が怪しい場合は、まずここを見る。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'CompactEnemyFrontlineIfEmpty' @(
    '敵の前衛が空になったとき、後衛を前へ詰める処理。',
    'これは敵の陣形維持であり、ダメージ計算ではない。',
    'この処理を変えた場合は、敵KO後の表示とhoverシルエットを必ず確認する。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'FillEmptyEnemyCellsFromReserves' @(
    '空いた敵セルへ控え敵を補充する処理。',
    '控えから盤面へ敵を置くため、戦闘データを変更する。',
    '見える敵が変わるので、後続でステータス表示やhoverプレビュー更新が必要になる。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'EnterCommandSelect' @(
    'プレイヤーがコマンドを選べるフェーズに入る処理。',
    'コマンドUIを準備し、前の行動状態を整理する。',
    'コマンドが出るタイミングがおかしい場合は、まずここを見る。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'EnterResolvingAction' @(
    '行動解決中フェーズに入る処理。',
    'このフェーズ中は、基本的にコマンド入力を受け付けない。',
    '演出の都合でプレビューを残す場合もあるが、入力状態とは分けて考える。'
)

Replace-Block 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'RedrawTurnOrderBar' @(
    '行動順バーの表示を更新する。',
    '実際の行動順を決める場所ではなく、現在の行動順データを描画する場所。',
    '実際の行動は正しいのに表示だけ変な場合は、この周辺を見る。'
)

Write-Host '日本語コメント化パッチが完了しました。'
