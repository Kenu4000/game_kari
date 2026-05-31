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

$field = @'
        private bool _enemyActionSilhouettePreviewActive;
        private readonly List<GridPos> _enemyActionSilhouetteFocusPositions = new();

'@
$text = InsertBeforeIfMissing $text '_enemyActionSilhouettePreviewActive' '        private readonly List<GridPos> _pendingActionFlashTargets = new();' $field 'enemy action silhouette preview state'

$oldEnterCommand = @'
            ClearTargetPreview();
            _phase = BattlePhase.CommandSelect;
'@
$newEnterCommand = @'
            ClearEnemyActionSilhouettePreview();
            ClearTargetPreview();
            _phase = BattlePhase.CommandSelect;
'@
$text = ReplaceOptional $text $oldEnterCommand $newEnterCommand 'clear enemy silhouette when entering command select'

$oldExecute = @'
            PrepareEnemyActionFlashTargets(enemy, action);
            SetPendingActionSourceFlashTargets(false, new List<GridPos> { enemy.GridPos });

            List<GridPos> targets = GetEnemyActionTargetPositions(enemy, action);
'@
$newExecute = @'
            PrepareEnemyActionFlashTargets(enemy, action);
            SetPendingActionSourceFlashTargets(false, new List<GridPos> { enemy.GridPos });

            List<GridPos> targets = GetEnemyActionTargetPositions(enemy, action);
            ApplyEnemyActionSilhouettePreview(targets);
'@
$text = ReplaceOptional $text $oldExecute $newExecute 'apply enemy silhouette preview from enemy action targets'

$oldEnemyRedraw = @'
            ExecuteEnemyAction(enemy, action);
            ClearPreviewEnemyActionState(enemy);
            RedrawBoard();

            yield return PlayPendingActionFlashOrDelay();
'@
$newEnemyRedraw = @'
            ExecuteEnemyAction(enemy, action);
            ClearPreviewEnemyActionState(enemy);
            RedrawBoard();
            ReapplyEnemyActionSilhouettePreviewIfNeeded();

            yield return PlayPendingActionFlashOrDelay();
'@
$text = ReplaceOptional $text $oldEnemyRedraw $newEnemyRedraw 'reapply enemy silhouette after enemy redraw'

$oldAdvance = @'
            AdvanceToNextActor();
        }
'@
$newAdvance = @'
            ClearEnemyActionSilhouettePreview();
            AdvanceToNextActor();
        }
'@
$text = ReplaceOptional $text $oldAdvance $newAdvance 'clear enemy silhouette before advancing after enemy action'

$helper = @'
        private void ApplyEnemyActionSilhouettePreview(List<GridPos> targetPositions)
        {
            _enemyActionSilhouettePreviewActive = true;
            _enemyActionSilhouetteFocusPositions.Clear();

            if (targetPositions != null)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    GridPos position = targetPositions[i];
                    if (!_enemyActionSilhouetteFocusPositions.Contains(position))
                    {
                        _enemyActionSilhouetteFocusPositions.Add(position);
                    }
                }
            }

            ReapplyEnemyActionSilhouettePreviewIfNeeded();
        }

        private void ClearEnemyActionSilhouettePreview()
        {
            _enemyActionSilhouettePreviewActive = false;
            _enemyActionSilhouetteFocusPositions.Clear();
        }

        private void ReapplyEnemyActionSilhouettePreviewIfNeeded()
        {
            if (!_enemyActionSilhouettePreviewActive || _battleEnded)
            {
                return;
            }

            for (int i = 0; i < _enemyActionSilhouetteFocusPositions.Count; i++)
            {
                ApplyAllyTopBottomOverlapAlphaFromFocusPosition(_enemyActionSilhouetteFocusPositions[i]);
            }
        }

        private void ApplyAllyTopBottomOverlapAlphaFromFocusPosition(GridPos focusPosition)
        {
            GridPos bottomPosition;
            switch (focusPosition)
            {
                case GridPos.FrontTop:
                    bottomPosition = GridPos.FrontBottom;
                    break;

                case GridPos.BackTop:
                    bottomPosition = GridPos.BackBottom;
                    break;

                default:
                    return;
            }

            if (_grid == null)
            {
                return;
            }

            BattleUnit focusUnit = _grid.GetUnit(true, focusPosition);
            BattleUnit bottomUnit = _grid.GetUnit(true, bottomPosition);
            if (focusUnit == null || focusUnit.IsDead || bottomUnit == null || bottomUnit.IsDead)
            {
                return;
            }

            RectTransform focusRect = GetBoardSpriteRect(true, focusPosition);
            RectTransform bottomRect = GetBoardSpriteRect(true, bottomPosition);
            if (focusRect == null || bottomRect == null || !RectTransformsOverlap(focusRect, bottomRect))
            {
                return;
            }

            Image bottomImage = GetBoardSpriteImage(true, bottomPosition);
            if (bottomImage == null)
            {
                return;
            }

            ApplySkillHoverSilhouette(bottomImage, skillHoverSilhouetteOverlapAlpha);
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplyEnemyActionSilhouettePreview(List<GridPos> targetPositions)' '        private void ExecuteEnemyAction(BattleUnit enemy, EnemyActionState action)' $helper 'enemy action silhouette preview helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy action to apply top-bottom ally overlap silhouette transparency.'
