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

# This flag means: KO fade already happened, but enemy status layout/replacement must wait until the action effect ends.
$field = @'
        private bool _pendingEnemyKoReplacementPhase;

'@
$text = InsertBeforeIfMissing $text '_pendingEnemyKoReplacementPhase' '        private bool _pendingEnemyAutoReplacementEnterAnimation;' $field 'pending enemy KO replacement phase flag'

# Clear on battle reset/setup.
$text = ReplaceOptional $text '            _pendingEnemyAutoReplacementEnterAnimation = false;' @'
            _pendingEnemyAutoReplacementEnterAnimation = false;
            _pendingEnemyKoReplacementPhase = false;
'@ 'clear pending enemy KO replacement phase flag'

# In KO fade coroutine: after fade, do not compact/fill/redraw/layout yet. Just remove dead grid cells and mark the replacement phase pending.
$oldImmediateReplacement = @'
            CompactEnemyFrontlineIfEmpty();
            bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
            RedrawBoard();
            ResetEnemyStatusCanvasGroupAlphas();

            if (replacementOccurred)
            {
                _pendingEnemyAutoReplacementEnterAnimation = true;
            }
'@
$newDeferredReplacement = @'
            _pendingEnemyKoReplacementPhase = true;
'@
$text = ReplaceOptional $text $oldImmediateReplacement $newDeferredReplacement 'defer compact/fill/redraw until replacement phase'

# Older variant without reset call.
$oldImmediateReplacement2 = @'
            CompactEnemyFrontlineIfEmpty();
            bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
            RedrawBoard();

            if (replacementOccurred)
            {
                _pendingEnemyAutoReplacementEnterAnimation = true;
            }
'@
$text = ReplaceOptional $text $oldImmediateReplacement2 $newDeferredReplacement 'defer compact/fill/redraw until replacement phase variant'

# Replacement phase: after action effects and popup cleanup, then align/compact/fill/redraw, then entrance animation if needed.
$oldPendingHelper = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }
'@
$newPendingHelper = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyKoReplacementPhase)
            {
                _pendingEnemyKoReplacementPhase = false;

                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();

                if (replacementOccurred)
                {
                    _pendingEnemyAutoReplacementEnterAnimation = true;
                }
            }

            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }
'@
$text = ReplaceOptional $text $oldPendingHelper $newPendingHelper 'move enemy replacement layout into replacement phase'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy KO status layout/replacement to run after action effects.'
