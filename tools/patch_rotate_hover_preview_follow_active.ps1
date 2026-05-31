$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) { throw "Patch anchor not found: $label" }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

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

$oldRotate = @'
            _formation.RotateAlliesClockwise();

            _formationSettling = true;
            _lastRotateTime = Time.time;
'@
$newRotate = @'
            _formation.RotateAlliesClockwise();
            SyncBoardUnitGridPositions();

            _formationSettling = true;
            _lastRotateTime = Time.time;
'@
$text = ReplaceRequired $text $oldRotate $newRotate 'sync GridPos immediately after rotate'

$oldRotateRedraw = @'
            RedrawBoard();
        }
'@
$newRotateRedraw = @'
            RedrawBoard();
            ReapplySkillHoverPreviewIfNeeded();
        }
'@
$text = ReplaceRequired $text $oldRotateRedraw $newRotateRedraw 'reapply hover preview after rotate redraw'

$oldConfirm = @'
            CompactFrontlineIfEmpty(true);

            RedrawBoard();

            if (!_battleEnded && _phase == BattlePhase.CommandSelect)
'@
$newConfirm = @'
            CompactFrontlineIfEmpty(true);
            SyncBoardUnitGridPositions();

            RedrawBoard();
            ReapplySkillHoverPreviewIfNeeded();

            if (!_battleEnded && _phase == BattlePhase.CommandSelect)
'@
$text = ReplaceRequired $text $oldConfirm $newConfirm 'reapply hover preview after formation confirm'

$helper = @'
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
$text = InsertBeforeIfMissing $text 'private void ReapplySkillHoverPreviewIfNeeded()' '        private void ConfirmFormation()' $helper 'hover preview reapply and GridPos sync helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched rotate hover preview so silhouettes follow the active BattleUnit after rotation.'
