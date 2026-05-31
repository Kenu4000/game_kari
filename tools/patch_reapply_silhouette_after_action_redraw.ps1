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

$oldPlayerSkillRedraw = @'
            ApplySkillDamage(skill);
            ApplySkillEffect(skill);
            RedrawBoard();

            if (_battleEnded)
'@
$newPlayerSkillRedraw = @'
            ApplySkillDamage(skill);
            ApplySkillEffect(skill);
            RedrawBoard();
            ReapplySkillHoverPreviewDuringActionIfNeeded();

            if (_battleEnded)
'@
$text = ReplaceOptional $text $oldPlayerSkillRedraw $newPlayerSkillRedraw 'reapply silhouette after player skill redraw'

$oldBattleEndedRedraw = @'
            if (_battleEnded)
            {
                RedrawBoard();
                yield break;
            }
'@
$newBattleEndedRedraw = @'
            if (_battleEnded)
            {
                RedrawBoard();
                ReapplySkillHoverPreviewDuringActionIfNeeded();
                yield break;
            }
'@
$text = ReplaceOptional $text $oldBattleEndedRedraw $newBattleEndedRedraw 'reapply silhouette after battle-ended redraw'

$oldHealRedraw = @'
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");
            RedrawBoard();

            StartCoroutine(FinishPlayerActionAfterDelay());
'@
$newHealRedraw = @'
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");
            RedrawBoard();
            ReapplySkillHoverPreviewDuringActionIfNeeded();

            StartCoroutine(FinishPlayerActionAfterDelay());
'@
$text = ReplaceOptional $text $oldHealRedraw $newHealRedraw 'reapply silhouette after item redraw'

$helper = @'
        private void ReapplySkillHoverPreviewDuringActionIfNeeded()
        {
            if (_hoveredSkill == null || _battleEnded)
            {
                return;
            }

            RedrawTargetPreview();
            ApplySkillHoverSpritePreview();
        }

'@
if (!$text.Contains('private void ReapplySkillHoverPreviewDuringActionIfNeeded()')) {
    $anchor = '        private void ReapplySkillHoverPreviewIfNeeded()'
    $index = $text.IndexOf($anchor)
    if ($index -lt 0) {
        $anchor = '        private void ConfirmFormation()'
        $index = $text.IndexOf($anchor)
    }
    if ($index -lt 0) { throw 'Patch anchor not found: ReapplySkillHoverPreviewDuringActionIfNeeded helper' }
    $text = $text.Substring(0, $index) + $helper + $text.Substring($index)
    Write-Host 'Inserted: ReapplySkillHoverPreviewDuringActionIfNeeded helper'
} else {
    Write-Host 'Already exists: ReapplySkillHoverPreviewDuringActionIfNeeded helper'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched action redraw paths to reapply skill-hover silhouettes during action animation.'
