$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceMethod {
    param([string]$Source, [string]$Signature, [string]$Replacement, [string]$Label)

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }
        throw "Patch anchor not found: $Label"
    }

    $open = $Source.IndexOf('{', $start)
    if ($open -lt 0) { throw "Method body start not found: $Label" }

    $depth = 0
    $end = -1
    for ($i = $open; $i -lt $Source.Length; $i++) {
        if ($Source[$i] -eq '{') { $depth++ }
        elseif ($Source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) { throw "Method body end not found: $Label" }
    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

$text = ReplaceMethod `
    -Source $text `
    -Signature '        private void RedrawEnemyActionPreviewHighlights()' `
    -Replacement @'
        private void RedrawEnemyActionPreviewHighlights()
        {
            // Enemy grid/action preview is intentionally disabled.
            // Keep active/target highlights independent from enemy forecast visuals.
            ResetAllyBoardHighlights();
            RedrawActiveHighlights();
        }
'@ `
    -Label 'RedrawEnemyActionPreviewHighlights'

$text = ReplaceMethod `
    -Source $text `
    -Signature '        private void HighlightEnemyActionTargets(BattleUnit enemy, EnemyActionState action)' `
    -Replacement @'
        private void HighlightEnemyActionTargets(BattleUnit enemy, EnemyActionState action)
        {
            // Enemy grid/action preview is intentionally disabled.
        }
'@ `
    -Label 'HighlightEnemyActionTargets'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Disabled enemy action grid preview highlights.'
