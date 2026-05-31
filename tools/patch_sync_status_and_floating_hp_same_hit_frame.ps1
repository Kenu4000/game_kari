$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

# Revert the previous timing change: status HP must not start before source lunge if floating HP starts after lunge.
$oldEarly = @'
            ApplyDeferredHpBarFillUpdates();
            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ShowPendingFloatingHpBars();

            ClearPendingActionFlashTargets();
'@
$newSameFrame = @'
            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ApplyDeferredHpBarFillUpdates();
            ShowPendingFloatingHpBars();

            ClearPendingActionFlashTargets();
'@
$text = ReplaceOptional $text $oldEarly $newSameFrame 'start status and floating HP on same hit frame'

# Keep player-side redraws because they are needed to queue deferred status HP rates.
# If a previous patch inserted duplicate RedrawBoard calls, do not add more here.

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status HP and floating HP to start on the same hit frame.'
