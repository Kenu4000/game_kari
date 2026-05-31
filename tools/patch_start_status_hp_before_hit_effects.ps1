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

# Ensure HP-changing player skill resolution queues status HP updates before the hit animation coroutine starts.
$oldPlayer = @'
            ApplySkillDamage(skill);
            ApplySkillEffect(skill);

            if (_battleEnded)
'@
$newPlayer = @'
            ApplySkillDamage(skill);
            ApplySkillEffect(skill);
            RedrawBoard();

            if (_battleEnded)
'@
$text = ReplaceOptional $text $oldPlayer $newPlayer 'redraw after player skill HP changes'

# Ensure item heal queues status HP updates before the hit animation coroutine starts.
$oldItem = @'
            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");

            StartCoroutine(FinishPlayerActionAfterDelay());
'@
$newItem = @'
            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");
            RedrawBoard();

            StartCoroutine(FinishPlayerActionAfterDelay());
'@
$text = ReplaceOptional $text $oldItem $newItem 'redraw after item heal HP changes'

# Start status HP animation before source lunge / floating HP / damage popup.
$oldOrder = @'
            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ApplyDeferredHpBarFillUpdates();
            ShowPendingFloatingHpBars();

            ClearPendingActionFlashTargets();
'@
$newOrder = @'
            ApplyDeferredHpBarFillUpdates();
            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ShowPendingFloatingHpBars();

            ClearPendingActionFlashTargets();
'@
$text = ReplaceOptional $text $oldOrder $newOrder 'start status HP before source lunge and hit effects'

# When there are no flash targets, still apply queued status HP immediately.
$oldNoFlash = @'
                ApplyDeferredHpBarFillUpdates();
                ShowPendingFloatingHpBars();
                yield return new WaitForSeconds(actionResolveDelaySeconds);
'@
$newNoFlash = @'
                ApplyDeferredHpBarFillUpdates();
                ShowPendingFloatingHpBars();
                yield return new WaitForSeconds(actionResolveDelaySeconds);
'@
$text = ReplaceOptional $text $oldNoFlash $newNoFlash 'no-flash status HP timing already immediate'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status HP to start before hit effects, and queued player-side HP redraws.'
