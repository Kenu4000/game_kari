$ErrorActionPreference = 'Stop'

# Battle readable refactor phase 2
# ------------------------------------------------------------
# This patch moves many BattleUIManager methods into partial files.
# The goal is readability. The script moves whole methods without
# changing their bodies.
#
# Important:
# - This should be run on refactor/battle-readable.
# - It does not change battle rules on purpose.
# - If Unity reports errors, switch back to main or revert this commit.

$branchName = 'refactor/battle-readable'
$mainPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$battleDir = 'Assets/Scripts/Battle'

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

function Get-CurrentBranch() {
    return (git rev-parse --abbrev-ref HEAD).Trim()
}

function Find-MatchingBrace([string]$text, [int]$openIndex) {
    $depth = 0
    $inString = $false
    $inChar = $false
    $inLineComment = $false
    $inBlockComment = $false
    $escape = $false

    for ($i = $openIndex; $i -lt $text.Length; $i++) {
        $c = $text[$i]
        $next = if ($i + 1 -lt $text.Length) { $text[$i + 1] } else { [char]0 }

        if ($inLineComment) {
            if ($c -eq "`n") { $inLineComment = $false }
            continue
        }

        if ($inBlockComment) {
            if ($c -eq '*' -and $next -eq '/') {
                $inBlockComment = $false
                $i++
            }
            continue
        }

        if ($inString) {
            if ($escape) { $escape = $false; continue }
            if ($c -eq '\\') { $escape = $true; continue }
            if ($c -eq '"') { $inString = $false }
            continue
        }

        if ($inChar) {
            if ($escape) { $escape = $false; continue }
            if ($c -eq '\\') { $escape = $true; continue }
            if ($c -eq "'") { $inChar = $false }
            continue
        }

        if ($c -eq '/' -and $next -eq '/') { $inLineComment = $true; $i++; continue }
        if ($c -eq '/' -and $next -eq '*') { $inBlockComment = $true; $i++; continue }
        if ($c -eq '"') { $inString = $true; continue }
        if ($c -eq "'") { $inChar = $true; continue }

        if ($c -eq '{') { $depth++ }
        if ($c -eq '}') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }

    return -1
}

function Extract-Method([ref]$textRef, [string]$methodName) {
    $text = $textRef.Value
    $pattern = "(?m)^        (private|public|protected|internal)\s+(static\s+)?(async\s+)?[\w<>\[\],\s\.]+\s+" + [regex]::Escape($methodName) + "\s*\("
    $match = [regex]::Match($text, $pattern)
    if (!$match.Success) { return $null }

    $start = $match.Index
    # Include immediately preceding single-line comments that are indented at method level.
    $prefixStart = $start
    while ($prefixStart -gt 0) {
        $lineStart = $text.LastIndexOf("`n", [Math]::Max(0, $prefixStart - 2))
        if ($lineStart -lt 0) { break }
        $candidateStart = $lineStart + 1
        $candidate = $text.Substring($candidateStart, $prefixStart - $candidateStart)
        if ($candidate -match '^        //') {
            $prefixStart = $candidateStart
        } elseif ($candidate -match '^\s*$') {
            $prefixStart = $candidateStart
        } else {
            break
        }
    }
    $start = $prefixStart

    $openIndex = $text.IndexOf('{', $match.Index)
    if ($openIndex -lt 0) { throw "Could not find opening brace for method: $methodName" }
    $closeIndex = Find-MatchingBrace $text $openIndex
    if ($closeIndex -lt 0) { throw "Could not find closing brace for method: $methodName" }

    $end = $closeIndex + 1
    while ($end -lt $text.Length -and ($text[$end] -eq "`r" -or $text[$end] -eq "`n")) { $end++ }

    $methodText = $text.Substring($start, $end - $start).TrimEnd()
    $newText = $text.Remove($start, $end - $start)
    $textRef.Value = $newText
    return $methodText
}

function Append-MethodsToPartial([string]$path, [string]$title, [string]$explain, [string[]]$methods) {
    $body = New-Object System.Text.StringBuilder
    [void]$body.AppendLine('namespace GameKari.Battle')
    [void]$body.AppendLine('{')
    [void]$body.AppendLine('    public partial class BattleUIManager')
    [void]$body.AppendLine('    {')
    [void]$body.AppendLine('        // ============================================================')
    [void]$body.AppendLine("        // $title")
    [void]$body.AppendLine('        // ------------------------------------------------------------')
    foreach ($line in $explain -split "`n") {
        [void]$body.AppendLine('        // ' + $line.TrimEnd())
    }
    [void]$body.AppendLine('        // ============================================================')

    foreach ($m in $methods) {
        if ([string]::IsNullOrWhiteSpace($m)) { continue }
        [void]$body.AppendLine()
        [void]$body.AppendLine($m.TrimEnd())
    }

    [void]$body.AppendLine('    }')
    [void]$body.AppendLine('}')
    Write-Utf8NoBom $path $body.ToString()
}

if (!(Test-Path $mainPath)) { throw "Required file not found: $mainPath" }
if ((Get-CurrentBranch) -ne $branchName) {
    throw "Run this on $branchName. Current branch: $(Get-CurrentBranch)"
}

$battleText = Get-Content -Path $mainPath -Raw -Encoding UTF8
if ($battleText -notmatch 'public partial class BattleUIManager') {
    throw 'BattleUIManager is not partial yet. Run phase1 patch first.'
}

# Method buckets. Names that do not exist in the current local file are skipped.
# This lets the patch survive small differences between local revisions.
$buckets = [ordered]@{
    'BattleUIManager.Preview.cs' = @{
        Title = 'PREVIEW AREA'
        Explain = @'
Visual-only preview methods live here.
They may highlight cells, recolor sprites, or show silhouettes.
They should not change HP, KO state, reserves, or turn order.
'@
        Names = @(
            'ClearTargetPreview',
            'RedrawTargetPreview',
            'ApplySkillHoverSpritePreview',
            'ClearSkillHoverSpritePreview',
            'ApplySkillHoverSilhouette',
            'EnsureSkillHoverSilhouetteMaterial',
            'SetBoardSpriteSilhouette',
            'SetBoardSpriteSilhouetteOutlineVisible',
            'EnsureSilhouetteOutlineImages',
            'ApplySkillHoverSilhouetteOverlapAlpha',
            'ApplyEnemyActionSilhouettePreview',
            'ClearEnemyActionSilhouettePreview',
            'ShowEnemyActionPreview',
            'HideEnemyActionPreview',
            'EnsureEnemyActionPreviewPanel',
            'SetEnemyActionPreviewText'
        )
    }
    'BattleUIManager.Animation.cs' = @{
        Title = 'ANIMATION AREA'
        Explain = @'
BattleUIManager-side animation bridge methods live here.
The actual frame-by-frame skill animation is handled by SkillAnimationPlayer.
These methods should prepare RectTransforms and call animation helpers.
'@
        Names = @(
            'PlaySkillAnimationIfAny',
            'GetPrimarySkillAnimationTargetRect',
            'GetSkillAnimationTargetPositions',
            'PlayActionSpriteLunge',
            'PlayActionSpriteLungeAndHitShake',
            'PlayTargetHitShake',
            'PlayActionFlashSequence',
            'FlashCells',
            'SetCellFlashColor',
            'RestoreCellColorAfterFlash',
            'PlayPendingAutoReplacementAnimations',
            'PlayAutoReplacementEnterAnimation'
        )
    }
    'BattleUIManager.StatusPanels.cs' = @{
        Title = 'STATUS PANEL AREA'
        Explain = @'
Status panel drawing and HP bar display methods live here.
These methods should display battle data, not decide battle rules.
If displayed HP looks wrong but unit HP is correct, start here.
'@
        Names = @(
            'RedrawStatusPanels',
            'RedrawEnemyStatusPanel',
            'RedrawAllyStatusPanel',
            'ResetEnemyStatusCanvasGroupAlphas',
            'ResetStatusSlot',
            'SetStatusSlotText',
            'SetStatusSlotHpBar',
            'AnimateHpBarFill',
            'ShowFloatingHpBar',
            'HideFloatingHpBar',
            'CreateFloatingHpBarIfNeeded',
            'GetOrCreateCanvasGroup',
            'FadeCanvasGroup'
        )
    }
    'BattleUIManager.KO.cs' = @{
        Title = 'KO / REPLACEMENT AREA'
        Explain = @'
Defeat, KO, reserve replacement, and enemy compacting methods live here.
This area is fragile because visual timing and battle data timing interact.
Move slowly and keep comments near any delayed status-panel update.
'@
        Names = @(
            'HandleAllyDefeated',
            'HandleEnemyDefeated',
            'ResolveDefeatedEnemies',
            'CompactEnemyFrontlineIfEmpty',
            'FillEmptyEnemyCellsFromReserves',
            'TryReplaceDefeatedAlly',
            'RemoveDefeatedUnitsFromTurnOrder',
            'IsUnitDefeated',
            'CollectDefeatedEnemies',
            'ApplyEnemyDefeatVisuals',
            'FadeDefeatedUnit'
        )
    }
    'BattleUIManager.Turns.cs' = @{
        Title = 'TURNS AREA'
        Explain = @'
Turn order and battle phase methods live here.
This file should answer whose turn it is and which phase the battle is in.
It should call view/action helpers rather than drawing every UI detail inline.
'@
        Names = @(
            'EnterCommandSelect',
            'EnterResolvingAction',
            'AdvanceTurn',
            'FinishTurn',
            'RefreshTurnOrder',
            'RedrawTurnOrderBar',
            'UpdateTurnOrderSlotViews',
            'SetActiveUnit',
            'FindNextActiveUnit',
            'IsBattleEnded',
            'HandleBattleEnded',
            'ShowBattleResult'
        )
    }
    'BattleUIManager.Actions.cs' = @{
        Title = 'ACTIONS AREA'
        Explain = @'
Player and enemy action resolution methods live here.
This file should contain the readable flow of actions.
Damage calculation, KO, status panels, preview, and animation can be called from here.
'@
        Names = @(
            'HandleSkillClicked',
            'HandleItemClicked',
            'HandlePassClicked',
            'HandleRotateClicked',
            'HandleMouseWheelRotateInput',
            'ResolvePlayerSkillAfterIntroDelay',
            'ResolveEnemyActionAfterIntroDelay',
            'ResolveEnemyAction',
            'ResolvePlayerSkill',
            'ApplySkillToTarget',
            'ApplySkillDamage',
            'ApplySkillHeal',
            'ApplySkillBuff',
            'QueueActionValuePopup',
            'ShowPendingActionValuePopups',
            'CreateActionValuePopupLabel'
        )
    }
}

$movedTotal = 0
$skipped = New-Object System.Collections.Generic.List[string]
foreach ($fileName in $buckets.Keys) {
    $bucket = $buckets[$fileName]
    $methods = New-Object System.Collections.Generic.List[string]
    foreach ($name in $bucket.Names) {
        $method = Extract-Method ([ref]$battleText) $name
        if ($null -ne $method) {
            $methods.Add($method) | Out-Null
            $movedTotal++
        } else {
            $skipped.Add($name) | Out-Null
        }
    }

    $path = Join-Path $battleDir $fileName
    Append-MethodsToPartial $path $bucket.Title $bucket.Explain $methods.ToArray()
}

Write-Utf8NoBom $mainPath $battleText

Write-Host "Moved methods: $movedTotal"
if ($skipped.Count -gt 0) {
    Write-Host 'Skipped methods not found in this local revision:'
    foreach ($s in $skipped) { Write-Host " - $s" }
}
Write-Host 'Open Unity and compile. If errors occur, paste the first errors.'
