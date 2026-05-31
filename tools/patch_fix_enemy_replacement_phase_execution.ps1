$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        throw "Patch anchor not found: $label"
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$old = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
            _pendingEnemyKoReplacementPhase = false;
            _statusSlotUnits.Clear();
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }
'@

$new = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyKoReplacementPhase)
            {
                _pendingEnemyKoReplacementPhase = false;

                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                _statusSlotUnits.Clear();
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

$text = ReplaceRequired $text $old $new 'fix PlayPendingAutoReplacementAnimations body'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Fixed enemy replacement phase execution.'
