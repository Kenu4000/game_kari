$ErrorActionPreference = 'Stop'

# Battle readable refactor phase 3
# ------------------------------------------------------------
# This patch does two things at the same time:
# 1. Remove simple compiler warnings that do not affect behavior.
# 2. Add explanatory comments before important methods in the partial files.
#
# The comments are intentionally verbose. The goal is to make the battle flow
# easier to understand for future debugging.

$mainPath = 'Assets/Scripts/Battle/BattleUIManager.cs'

function Write-Utf8NoBom([string]$path, [string]$content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

function Replace-Text([ref]$textRef, [string]$old, [string]$new, [string]$label) {
    if ($textRef.Value.Contains($old)) {
        $textRef.Value = $textRef.Value.Replace($old, $new)
        Write-Host "Replaced: $label"
    } else {
        Write-Host "Skip, not found: $label"
    }
}

function Insert-Comment-Before-Method([string]$path, [string]$methodName, [string]$commentText) {
    if (!(Test-Path $path)) {
        Write-Host "Skip missing file: $path"
        return
    }

    $text = Get-Content -Path $path -Raw -Encoding UTF8
    $marker = "READABLE-REFORM: $methodName"
    if ($text.Contains($marker)) {
        Write-Host "Comment already exists: $methodName"
        return
    }

    $pattern = "(?m)^        (private|public|protected|internal)\s+(static\s+)?[\w<>\[\],\s\.]+\s+" + [regex]::Escape($methodName) + "\s*\("
    $match = [regex]::Match($text, $pattern)
    if (!$match.Success) {
        Write-Host "Skip method not found: $methodName"
        return
    }

    $lines = $commentText -split "`n"
    $comment = New-Object System.Text.StringBuilder
    [void]$comment.AppendLine("        // READABLE-REFORM: $methodName")
    foreach ($line in $lines) {
        [void]$comment.AppendLine('        // ' + $line.TrimEnd())
    }

    $text = $text.Insert($match.Index, $comment.ToString())
    Write-Utf8NoBom $path $text
    Write-Host "Added comment: $methodName"
}

if (!(Test-Path $mainPath)) {
    throw "Required file not found: $mainPath"
}

# ------------------------------------------------------------
# Warning cleanup in BattleUIManager.cs
# ------------------------------------------------------------
$main = Get-Content -Path $mainPath -Raw -Encoding UTF8

# Old silhouette shader size field is no longer used after the outline was
# changed to duplicate Image objects. Removing it clears CS0414.
$main = [regex]::Replace(
    $main,
    '\r?\n\s*\[SerializeField\]\s+private\s+float\s+skillHoverSilhouetteOutlineSize\s*=\s*1f;\s*',
    "`r`n")

# Newer Unity versions warn about the overload that takes FindObjectsSortMode.
# The two-argument overload is no longer necessary here because we only need
# any BattleUIReferences object, not a stable sorted order.
$main = $main.Replace(
    'FindObjectsByType<BattleUIReferences>(FindObjectsInactive.Include, FindObjectsSortMode.None)',
    'FindObjectsByType<BattleUIReferences>(FindObjectsInactive.Include)')

Write-Utf8NoBom $mainPath $main
Write-Host 'Cleaned warning-prone BattleUIManager fields/API usage.'

# ------------------------------------------------------------
# Heavy comments for moved partial files.
# ------------------------------------------------------------
Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'HandleSkillClicked' @'
Called when the player chooses a skill button.
This method is the entrance to player action resolution.
It should validate that commands are currently accepted, then start the action flow.
Do not put low-level damage/KO/status-panel details directly here.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'ResolvePlayerSkillAfterIntroDelay' @'
Coroutine for a player skill after the action title is shown.
The intended order is:
1. show action name
2. wait for the intro delay
3. play skill animation if the skill has one
4. apply the skill result
5. continue the battle flow
If damage timing becomes confusing, read this method first.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Actions.cs' 'HandleRotateClicked' @'
Called when the player rotates the formation.
Rotation affects which character is active and which cells overlap visually.
After rotation, hover preview may need to be reapplied so the gray silhouette follows the current active unit.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Animation.cs' 'PlaySkillAnimationIfAny' @'
Bridge from battle logic to SkillAnimationPlayer.
This method should only find the caster/target UI objects and pass them to the animation player.
The actual frame animation, projectile movement, and Canvas animation layer are handled elsewhere.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Animation.cs' 'GetPrimarySkillAnimationTargetRect' @'
Finds the main target RectTransform used by a skill animation.
This is visual targeting only. It should not decide damage targets.
If animation flies toward the wrong cell, inspect this method and the SkillAnimationData anchor settings.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ClearTargetPreview' @'
Clears visual target preview only.
This should not change HP, turn state, active unit, KO state, or reserves.
Use this when leaving hover/selection states or before redrawing preview highlights.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'RedrawTargetPreview' @'
Redraws the yellow target preview shown while hovering/selecting a skill.
This is a view operation. It should answer only:
"Which cells should look highlighted right now?"
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ApplySkillHoverSpritePreview' @'
Applies the gray silhouette preview while hovering a skill.
The active unit and intended target remain normal; unrelated sprites become gray silhouettes.
If hover visuals look wrong after rotate or enemy replacement, start reading here.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Preview.cs' 'ApplySkillHoverSilhouetteOverlapAlpha' @'
Handles the special overlap case where a top-row active unit overlaps the bottom-row ally.
Only visual alpha should be changed here.
Do not change the real unit data from this method.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.StatusPanels.cs' 'RedrawStatusPanels' @'
Redraws status panels from current battle data.
Status panels are display objects only. They should show HP and KO state, not decide them.
If a bar looks wrong but the BattleUnit HP is correct, debug this area.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'ResolveDefeatedEnemies' @'
Handles enemy defeat results after damage has already been applied.
This area is fragile because visual timing matters:
KO fade, enemy compacting, reserve entry, and status panel refresh must not happen in a confusing order.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'CompactEnemyFrontlineIfEmpty' @'
Moves enemy backline units forward when the frontline becomes empty.
This is enemy formation maintenance, not damage calculation.
After changing this method, check enemy KO and hover silhouette refresh carefully.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.KO.cs' 'FillEmptyEnemyCellsFromReserves' @'
Fills empty enemy cells from reserve enemies.
This method changes battle data by placing reserve units on the grid.
Because it changes visible units, status panels and hover previews may need to be refreshed afterwards.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'EnterCommandSelect' @'
Enters the phase where the player can choose a command.
This is a phase transition method. It should prepare command UI and clear old action state.
If commands appear at the wrong time, start here.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'EnterResolvingAction' @'
Enters the phase where an action is being resolved.
During this phase, command input should be blocked.
Some visual previews may intentionally remain until the action animation needs them cleared.
'@

Insert-Comment-Before-Method 'Assets/Scripts/Battle/BattleUIManager.Turns.cs' 'RedrawTurnOrderBar' @'
Updates the turn order display.
This should not decide actual turn order by itself; it should draw the current turn-order state.
If the display is wrong but actions occur correctly, debug here.
'@

Write-Host 'Phase 3 warning cleanup and comments completed.'
