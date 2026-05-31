$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) { throw "Patch anchor not found: $label" }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$oldEnterResolving = @'
            _phase = BattlePhase.ResolvingAction;
            // HP bar deferral starts immediately before the actual HP-changing resolution.
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            SetActionOverlayVisible(true);
'@
$newEnterResolving = @'
            _phase = BattlePhase.ResolvingAction;
            // Keep skill-hover silhouettes visible during the action animation.
            if (_hoveredSkill != null)
            {
                RedrawTargetPreview();
                ApplySkillHoverSpritePreview();
            }
            else
            {
                ResetEnemyBoardHighlights();
                ResetBoardSpritePreviewColors();
            }

            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            SetActionOverlayVisible(true);
'@
$text = ReplaceRequired $text $oldEnterResolving $newEnterResolving 'keep hover preview during resolving action'

$oldEnterCommand = @'
            _phase = BattlePhase.CommandSelect;
            _active = activeUnit;
            EnsureEnemyActionStatesForPreview();
'@
$newEnterCommand = @'
            ClearTargetPreview();
            _phase = BattlePhase.CommandSelect;
            _active = activeUnit;
            EnsureEnemyActionStatesForPreview();
'@
$text = ReplaceRequired $text $oldEnterCommand $newEnterCommand 'clear old hover preview when entering command select'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill hover silhouettes to remain visible during action animation.'
