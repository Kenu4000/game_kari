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

# Pending flag. Replacement entrance should be a separate segment after the action effect finishes.
$fieldInsert = @'
        private bool _pendingEnemyAutoReplacementEnterAnimation;

'@
$text = InsertBeforeIfMissing $text 'private bool _pendingEnemyAutoReplacementEnterAnimation' '        private readonly List<ActionValuePopup> _pendingActionValuePopups = new();' $fieldInsert 'pending enemy replacement animation flag'

# Reset pending flag at battle setup/list clear timing.
$text = ReplaceOptional $text '            _actedUnits.Clear();' @'
            _actedUnits.Clear();
            _pendingEnemyAutoReplacementEnterAnimation = false;
'@ 'clear pending enemy replacement flag'

# In KO fadeout, do not play replacement entrance immediately. Only mark it pending.
$oldInlinePlay = @'
            if (replacementOccurred)
            {
                yield return PlayAutoReplacementEnterAnimation(false);
            }
'@
$newPending = @'
            if (replacementOccurred)
            {
                _pendingEnemyAutoReplacementEnterAnimation = true;
            }
'@
$text = ReplaceOptional $text $oldInlinePlay $newPending 'defer replacement entrance until after action effect'

# After action effect has fully ended and value popups are cleared, play replacement entrance as its own segment.
$oldEndEffect = @'
            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
        }
'@
$newEndEffect = @'
            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
            yield return PlayPendingAutoReplacementAnimations();
        }
'@
$text = ReplaceOptional $text $oldEndEffect $newEndEffect 'play pending replacement after action value cleanup'

# The no-flash branch also ends with Hide/Clear/yield break. Add replacement segment before yield break.
$oldNoFlashEnd = @'
                HideActiveActionValuePopups();
                ClearPendingActionValuePopups();
                yield break;
'@
$newNoFlashEnd = @'
                HideActiveActionValuePopups();
                ClearPendingActionValuePopups();
                yield return PlayPendingAutoReplacementAnimations();
                yield break;
'@
$text = ReplaceOptional $text $oldNoFlashEnd $newNoFlashEnd 'play pending replacement after no-flash action cleanup'

$helper = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private IEnumerator PlayPendingAutoReplacementAnimations()' '        private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)' $helper 'PlayPendingAutoReplacementAnimations helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched auto replacement entrance to play after the action effect has finished.'
