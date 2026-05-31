$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

# Ensure helper exists even if the rotate-follow patch has not been applied locally yet.
$helpers = @'
        private void ReapplySkillHoverPreviewIfNeeded()
        {
            if (_hoveredSkill == null || _battleEnded || _phase != BattlePhase.CommandSelect)
            {
                return;
            }

            RedrawTargetPreview();
            ApplySkillHoverSpritePreview();
        }

        private void SyncBoardUnitGridPositions()
        {
            SyncBoardUnitGridPosition(true, GridPos.FrontTop);
            SyncBoardUnitGridPosition(true, GridPos.BackTop);
            SyncBoardUnitGridPosition(true, GridPos.FrontBottom);
            SyncBoardUnitGridPosition(true, GridPos.BackBottom);
            SyncBoardUnitGridPosition(false, GridPos.FrontTop);
            SyncBoardUnitGridPosition(false, GridPos.BackTop);
            SyncBoardUnitGridPosition(false, GridPos.FrontBottom);
            SyncBoardUnitGridPosition(false, GridPos.BackBottom);
        }

        private void SyncBoardUnitGridPosition(bool isAllyBoard, GridPos position)
        {
            if (_grid == null)
            {
                return;
            }

            BattleUnit unit = _grid.GetUnit(isAllyBoard, position);
            if (unit != null)
            {
                unit.GridPos = position;
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private void ReapplySkillHoverPreviewIfNeeded()' '        private void ConfirmFormation()' $helpers 'hover preview reapply and GridPos sync helpers'

$old = @'
                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                _statusSlotUnits.Clear();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();

                if (replacementOccurred)
'@
$new = @'
                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                SyncBoardUnitGridPositions();
                _statusSlotUnits.Clear();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();
                ReapplySkillHoverPreviewIfNeeded();

                if (replacementOccurred)
'@
$text = ReplaceOptional $text $old $new 'refresh hover preview after enemy compact/replacement'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy compact/replacement to refresh skill hover silhouettes after grid movement.'
